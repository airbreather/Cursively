using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Cursively.Inputs
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CsvStreamInput : CsvAsyncInput
    {
        private readonly Stream _csvStream;

        private readonly int _minReadBufferByteCount;

        private readonly ArrayPool<byte> _readBufferPool;

        private readonly bool _ignoreUTF8ByteOrderMark;

        private readonly long _originalStreamPosition;

        internal CsvStreamInput(byte delimiter, Stream csvStream, int minReadBufferByteCount, ArrayPool<byte> readBufferPool, bool ignoreUTF8ByteOrderMark)
            : base(delimiter, true)
        {
            _csvStream = csvStream;
            _minReadBufferByteCount = minReadBufferByteCount;
            _readBufferPool = readBufferPool;
            _ignoreUTF8ByteOrderMark = ignoreUTF8ByteOrderMark;

            _originalStreamPosition = csvStream.CanSeek ? csvStream.Position : -1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public CsvStreamInput WithDelimiter(byte delimiter) =>
            new CsvStreamInput(delimiter, _csvStream, _minReadBufferByteCount, _readBufferPool, _ignoreUTF8ByteOrderMark);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="minReadBufferByteCount"></param>
        /// <returns></returns>
        public CsvStreamInput WithMinReadBufferByteCount(int minReadBufferByteCount)
        {
            if (minReadBufferByteCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(minReadBufferByteCount), minReadBufferByteCount, "Must be greater than zero.");
            }

            return new CsvStreamInput(Delimiter, _csvStream, minReadBufferByteCount, _readBufferPool, _ignoreUTF8ByteOrderMark);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="readBufferPool"></param>
        /// <returns></returns>
        public CsvStreamInput WithReadBufferPool(ArrayPool<byte> readBufferPool) =>
            new CsvStreamInput(Delimiter, _csvStream, _minReadBufferByteCount, readBufferPool, _ignoreUTF8ByteOrderMark);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ignoreUTF8ByteOrderMark"></param>
        /// <returns></returns>
        public CsvStreamInput WithIgnoreUTF8ByteOrderMark(bool ignoreUTF8ByteOrderMark) =>
            new CsvStreamInput(Delimiter, _csvStream, _minReadBufferByteCount, _readBufferPool, ignoreUTF8ByteOrderMark);

        /// <inheritdoc />
        protected override void Process(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor)
        {
            var csvStream = _csvStream;
            int minReadBufferByteCount = _minReadBufferByteCount;
            var readBufferPool = _readBufferPool;

            byte[] readBuffer;
            if (readBufferPool is null)
            {
                readBuffer = new byte[minReadBufferByteCount];
            }
            else
            {
                readBuffer = readBufferPool.Rent(minReadBufferByteCount);
            }

            try
            {
                if (_ignoreUTF8ByteOrderMark && EatUTF8BOM(tokenizer, visitor, csvStream, readBuffer))
                {
                    return;
                }

                int cnt;
                while ((cnt = csvStream.Read(readBuffer, 0, readBuffer.Length)) != 0)
                {
                    tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(readBuffer, 0, cnt), visitor);
                }
            }
            finally
            {
                readBufferPool?.Return(readBuffer, clearArray: true);
            }

            tokenizer.ProcessEndOfStream(visitor);
        }

        /// <inheritdoc />
        protected override async ValueTask ProcessAsync(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor, IProgress<int> progress, CancellationToken cancellationToken)
        {
            // not all streams support cancellation, so we might as well do this ourselves.  it
            // does involve a volatile read, so don't go overboard.
            cancellationToken.ThrowIfCancellationRequested();

            var csvStream = _csvStream;
            int minReadBufferByteCount = _minReadBufferByteCount;
            var readBufferPool = _readBufferPool;

            byte[] readBuffer;
            if (readBufferPool is null)
            {
                readBuffer = new byte[minReadBufferByteCount];
            }
            else
            {
                readBuffer = readBufferPool.Rent(minReadBufferByteCount);
            }

            try
            {
                if (_ignoreUTF8ByteOrderMark && await EatUTF8BOMAsync(tokenizer, visitor, csvStream, readBuffer, progress, cancellationToken).ConfigureAwait(false))
                {
                    return;
                }

                int cnt;
                while ((cnt = await csvStream.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
                {
                    // not all streams support cancellation, so we might as well do this ourselves.  it
                    // does involve a volatile read, so don't go overboard.
                    cancellationToken.ThrowIfCancellationRequested();

                    tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(readBuffer, 0, cnt), visitor);
                    progress?.Report(cnt);
                }
            }
            finally
            {
                readBufferPool?.Return(readBuffer, clearArray: true);
            }

            tokenizer.ProcessEndOfStream(visitor);
            progress?.Report(0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override bool TryResetCore()
        {
            if (_originalStreamPosition < 0)
            {
                return false;
            }

            _csvStream.Position = _originalStreamPosition;
            return true;
        }

        private static bool EatUTF8BOM(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor, Stream csvStream, byte[] readBuffer)
        {
            if (readBuffer.Length < 3)
            {
                // don't bother pooling; nobody should really ever care.
                readBuffer = new byte[3];
            }

            int byteCount = 0;
            while (byteCount < 3)
            {
                int readLength = csvStream.Read(readBuffer, byteCount, readBuffer.Length - byteCount);
                if (readLength == 0)
                {
                    if (byteCount != 0)
                    {
                        tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(readBuffer, 0, byteCount), visitor);
                    }

                    tokenizer.ProcessEndOfStream(visitor);
                    return true;
                }

                byteCount += readLength;
            }

            int byteOffset = 0;
            if (readBuffer[0] == 0xEF &&
                readBuffer[1] == 0xBB &&
                readBuffer[2] == 0xBF)
            {
                byteOffset = 3;
            }

            if (byteOffset < byteCount)
            {
                tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(readBuffer, byteOffset, byteCount - byteOffset), visitor);
            }

            return false;
        }

        private static async ValueTask<bool> EatUTF8BOMAsync(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor, Stream csvStream, byte[] readBuffer, IProgress<int> progress, CancellationToken cancellationToken)
        {
            if (readBuffer.Length < 3)
            {
                // don't bother pooling; nobody should really ever care.
                readBuffer = new byte[3];
            }

            int byteCount = 0;
            while (byteCount < 3)
            {
                int readLength = await csvStream.ReadAsync(readBuffer, byteCount, readBuffer.Length - byteCount, cancellationToken).ConfigureAwait(false);

                // not all streams support cancellation, so we might as well do this ourselves.  it
                // does involve a volatile read, so don't go overboard.
                cancellationToken.ThrowIfCancellationRequested();

                if (readLength == 0)
                {
                    if (byteCount != 0)
                    {
                        tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(readBuffer, 0, byteCount), visitor);
                        progress?.Report(byteCount);
                    }

                    tokenizer.ProcessEndOfStream(visitor);
                    progress?.Report(0);
                    return true;
                }

                byteCount += readLength;
            }

            int byteOffset = 0;
            if (readBuffer[0] == 0xEF &&
                readBuffer[1] == 0xBB &&
                readBuffer[2] == 0xBF)
            {
                byteOffset = 3;
            }

            if (byteOffset < byteCount)
            {
                tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(readBuffer, byteOffset, byteCount - byteOffset), visitor);
            }

            progress?.Report(byteCount);
            return false;
        }
    }
}
