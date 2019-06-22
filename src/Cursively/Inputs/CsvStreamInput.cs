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
        private readonly Stream _stream;

        private readonly int _minReadBufferByteCount;

        private readonly ArrayPool<byte> _readBufferPool;

        private readonly long _pos;

        private readonly bool _ignoreUTF8ByteOrderMark;

        internal CsvStreamInput(byte delimiter, Stream stream, int minReadBufferByteCount, ArrayPool<byte> readBufferPool, bool ignoreUTF8ByteOrderMark)
            : base(delimiter, true)
        {
            _stream = stream;
            _minReadBufferByteCount = minReadBufferByteCount;
            _readBufferPool = readBufferPool;
            _ignoreUTF8ByteOrderMark = ignoreUTF8ByteOrderMark;

            _pos = stream.CanSeek ? stream.Position : -1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public CsvStreamInput WithDelimiter(byte delimiter) =>
            new CsvStreamInput(delimiter, _stream, _minReadBufferByteCount, _readBufferPool, _ignoreUTF8ByteOrderMark);

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

            return new CsvStreamInput(Delimiter, _stream, minReadBufferByteCount, _readBufferPool, _ignoreUTF8ByteOrderMark);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="readBufferPool"></param>
        /// <returns></returns>
        public CsvStreamInput WithReadBufferPool(ArrayPool<byte> readBufferPool) =>
            new CsvStreamInput(Delimiter, _stream, _minReadBufferByteCount, readBufferPool, _ignoreUTF8ByteOrderMark);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ignoreUTF8ByteOrderMark"></param>
        /// <returns></returns>
        public CsvStreamInput WithIgnoreUTF8ByteOrderMark(bool ignoreUTF8ByteOrderMark) =>
            new CsvStreamInput(Delimiter, _stream, _minReadBufferByteCount, _readBufferPool, ignoreUTF8ByteOrderMark);

        /// <inheritdoc />
        protected override void Process(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor)
        {
            var stream = _stream;
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
                if (_ignoreUTF8ByteOrderMark && EatUTF8BOM(tokenizer, visitor, stream, readBuffer))
                {
                    return;
                }

                int cnt;
                while ((cnt = stream.Read(readBuffer, 0, readBuffer.Length)) != 0)
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

            var stream = _stream;
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
                if (_ignoreUTF8ByteOrderMark && await EatUTF8BOMAsync(tokenizer, visitor, stream, readBuffer, progress, cancellationToken).ConfigureAwait(false))
                {
                    return;
                }

                int cnt;
                while ((cnt = await stream.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
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
            if (_pos < 0)
            {
                return false;
            }

            _stream.Seek(_pos, SeekOrigin.Begin);
            return true;
        }

        private static bool EatUTF8BOM(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor, Stream stream, byte[] buffer)
        {
            if (buffer.Length < 3)
            {
                buffer = new byte[3];
            }

            int off = 0;
            while (off < 3)
            {
                int cnt = stream.Read(buffer, off, buffer.Length - off);
                if (cnt == 0)
                {
                    if (off != 0)
                    {
                        tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(buffer, 0, off), visitor);
                    }

                    tokenizer.ProcessEndOfStream(visitor);
                    return true;
                }

                off += cnt;
            }

            int len = off;
            off = 0;
            if (buffer[0] == 0xEF &&
                buffer[1] == 0xBB &&
                buffer[2] == 0xBF)
            {
                off = 3;
                len -= 3;
            }

            if (len != 0)
            {
                tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(buffer, off, len), visitor);
            }

            return false;
        }

        private static async ValueTask<bool> EatUTF8BOMAsync(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor, Stream stream, byte[] buffer, IProgress<int> progress, CancellationToken cancellationToken)
        {
            if (buffer.Length < 3)
            {
                buffer = new byte[3];
            }

            int off = 0;
            while (off < 3)
            {
                int cnt = await stream.ReadAsync(buffer, off, buffer.Length - off, cancellationToken).ConfigureAwait(false);

                // not all streams support cancellation, so we might as well do this ourselves.  it
                // does involve a volatile read, so don't go overboard.
                cancellationToken.ThrowIfCancellationRequested();

                if (cnt == 0)
                {
                    if (off != 0)
                    {
                        tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(buffer, 0, off), visitor);
                        progress?.Report(off);
                    }

                    tokenizer.ProcessEndOfStream(visitor);
                    progress?.Report(0);
                    return true;
                }

                off += cnt;
            }

            int len = off;
            int rptLen = len;
            off = 0;
            if (buffer[0] == 0xEF &&
                buffer[1] == 0xBB &&
                buffer[2] == 0xBF)
            {
                off = 3;
                len -= 3;
            }

            if (len != 0)
            {
                tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(buffer, off, len), visitor);
            }

            progress?.Report(rptLen);
            return false;
        }
    }
}
