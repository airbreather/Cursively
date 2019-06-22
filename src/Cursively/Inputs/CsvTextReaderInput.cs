using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cursively.Inputs
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CsvTextReaderInput : CsvAsyncInput
    {
        private readonly TextReader _textReader;

        private readonly int _readBufferCharCount;

        private readonly int _encodeBatchCharCount;

        private readonly bool _ignoreByteOrderMark;

        private readonly ArrayPool<char> _readBufferPool;

        private readonly MemoryPool<byte> _encodeBufferPool;

        internal CsvTextReaderInput(byte delimiter, TextReader textReader, int readBufferCharCount, ArrayPool<char> readBufferPool, int encodeBatchCharCount, MemoryPool<byte> encodeBufferPool, bool ignoreByteOrderMark)
            : base(delimiter, true)
        {
            _textReader = textReader;
            _readBufferCharCount = readBufferCharCount;
            _encodeBatchCharCount = encodeBatchCharCount;
            _ignoreByteOrderMark = ignoreByteOrderMark;
            _readBufferPool = readBufferPool;
            _encodeBufferPool = encodeBufferPool;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public CsvTextReaderInput WithDelimiter(byte delimiter) =>
            new CsvTextReaderInput(delimiter, _textReader, _readBufferCharCount, _readBufferPool, _encodeBatchCharCount, _encodeBufferPool, _ignoreByteOrderMark);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="readBufferCharCount"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public CsvTextReaderInput WithReadBufferCharCount(int readBufferCharCount)
        {
            if (readBufferCharCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(readBufferCharCount), readBufferCharCount, "Must be greater than zero.");
            }

            return new CsvTextReaderInput(Delimiter, _textReader, readBufferCharCount, _readBufferPool, _encodeBatchCharCount, _encodeBufferPool, _ignoreByteOrderMark);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="readBufferPool"></param>
        /// <returns></returns>
        public CsvTextReaderInput WithReadBufferPool(ArrayPool<char> readBufferPool) =>
            new CsvTextReaderInput(Delimiter, _textReader, _readBufferCharCount, readBufferPool, _encodeBatchCharCount, _encodeBufferPool, _ignoreByteOrderMark);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="encodeBatchCharCount"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public CsvTextReaderInput WithEncodeBatchCharCount(int encodeBatchCharCount)
        {
            if (encodeBatchCharCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(encodeBatchCharCount), encodeBatchCharCount, "Must be greater than zero.");
            }

            return new CsvTextReaderInput(Delimiter, _textReader, _readBufferCharCount, _readBufferPool, encodeBatchCharCount, _encodeBufferPool, _ignoreByteOrderMark);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="encodeBufferPool"></param>
        /// <returns></returns>
        public CsvTextReaderInput WithEncodeBufferPool(MemoryPool<byte> encodeBufferPool) =>
            new CsvTextReaderInput(Delimiter, _textReader, _readBufferCharCount, _readBufferPool, _encodeBatchCharCount, encodeBufferPool, _ignoreByteOrderMark);

        /// <inheritdoc />
        protected override void Process(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor)
        {
            var textReader = _textReader;
            int readBufferCharCount = _readBufferCharCount;
            int encodeBatchCharCount = _encodeBatchCharCount;
            var readBufferPool = _readBufferPool;
            var encodeBufferPool = _encodeBufferPool;

            IMemoryOwner<byte> encodeBufferOwner = null;

            char[] readBuffer;
            if (readBufferPool is null)
            {
                readBuffer = new char[readBufferCharCount];
            }
            else
            {
                readBuffer = readBufferPool.Rent(readBufferCharCount);
            }

            try
            {
                if (encodeBatchCharCount > readBuffer.Length)
                {
                    encodeBatchCharCount = readBuffer.Length;
                }

                int encodeBufferLength = Encoding.UTF8.GetMaxByteCount(encodeBatchCharCount);
                Span<byte> encodeBuffer = stackalloc byte[0];
                if (encodeBufferLength < 1024)
                {
                    encodeBuffer = stackalloc byte[encodeBufferLength];
                }
                else if (encodeBufferPool is null)
                {
                    encodeBuffer = new byte[encodeBufferLength];
                }
                else
                {
                    encodeBufferOwner = encodeBufferPool.Rent(encodeBufferLength);
                    encodeBuffer = encodeBufferOwner.Memory.Span;
                }

                if (_ignoreByteOrderMark && EatBOM(tokenizer, visitor, readBuffer, encodeBuffer, encodeBatchCharCount))
                {
                    return;
                }

                int cnt;
                while ((cnt = textReader.Read(readBuffer, 0, readBuffer.Length)) != 0)
                {
                    CsvCharsInput.ProcessSegment(tokenizer, visitor, new ReadOnlySpan<char>(readBuffer, 0, cnt), encodeBuffer, encodeBatchCharCount);
                }
            }
            finally
            {
                encodeBufferOwner?.Dispose();
                readBufferPool?.Return(readBuffer, clearArray: true);
            }

            tokenizer.ProcessEndOfStream(visitor);
        }

        /// <inheritdoc />
        protected override async ValueTask ProcessAsync(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor, IProgress<int> progress, CancellationToken cancellationToken)
        {
            // TextReader doesn't support cancellation, so it's really important that we do this
            // ourselves.  it does involve a volatile read, so don't go overboard.
            cancellationToken.ThrowIfCancellationRequested();

            var textReader = _textReader;
            int readBufferCharCount = _readBufferCharCount;
            int encodeBatchCharCount = _encodeBatchCharCount;
            var readBufferPool = _readBufferPool;
            var encodeBufferPool = _encodeBufferPool;

            IMemoryOwner<byte> encodeBufferOwner = null;

            char[] readBuffer;
            if (readBufferPool is null)
            {
                readBuffer = new char[readBufferCharCount];
            }
            else
            {
                readBuffer = readBufferPool.Rent(readBufferCharCount);
            }

            try
            {
                if (encodeBatchCharCount > readBuffer.Length)
                {
                    encodeBatchCharCount = readBuffer.Length;
                }

                int encodeBufferLength = Encoding.UTF8.GetMaxByteCount(encodeBatchCharCount);

                Memory<byte> encodeBuffer;
                if (encodeBufferPool is null)
                {
                    encodeBuffer = new byte[encodeBufferLength];
                }
                else
                {
                    encodeBufferOwner = encodeBufferPool.Rent(encodeBufferLength);
                    encodeBuffer = encodeBufferOwner.Memory;
                }

                if (_ignoreByteOrderMark && await EatBOMAsync(tokenizer, visitor, readBuffer, encodeBuffer, encodeBatchCharCount, progress, cancellationToken).ConfigureAwait(false))
                {
                    return;
                }

                int cnt;
                while ((cnt = await textReader.ReadAsync(readBuffer, 0, readBuffer.Length).ConfigureAwait(false)) != 0)
                {
                    // TextReader doesn't support cancellation, so it's really important that we do this
                    // ourselves.  it does involve a volatile read, so don't go overboard.
                    cancellationToken.ThrowIfCancellationRequested();

                    CsvCharsInput.ProcessSegment(tokenizer, visitor, new ReadOnlySpan<char>(readBuffer, 0, cnt), encodeBuffer.Span, encodeBatchCharCount);
                    progress?.Report(cnt);
                }
            }
            finally
            {
                encodeBufferOwner?.Dispose();
                readBufferPool?.Return(readBuffer, clearArray: true);
            }

            tokenizer.ProcessEndOfStream(visitor);
            progress?.Report(0);
        }

        /// <inheritdoc />
        protected override bool TryResetCore() => false;

        private bool EatBOM(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor, char[] readBuffer, Span<byte> encodeBuffer, int encodeBatchCharCount)
        {
            int cnt = _textReader.Read(readBuffer, 0, readBuffer.Length);
            if (cnt == 0)
            {
                tokenizer.ProcessEndOfStream(visitor);
                return true;
            }

            int off = 0;
            if (cnt > 0 && readBuffer[0] == '\uFEFF')
            {
                ++off;
                --cnt;
            }

            if (cnt != 0)
            {
                CsvCharsInput.ProcessSegment(tokenizer, visitor, new ReadOnlySpan<char>(readBuffer, off, cnt), encodeBuffer, encodeBatchCharCount);
            }

            return false;
        }

        private async ValueTask<bool> EatBOMAsync(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor, char[] readBuffer, Memory<byte> encodeBuffer, int encodeBatchCharCount, IProgress<int> progress, CancellationToken cancellationToken)
        {
            int cnt = await _textReader.ReadAsync(readBuffer, 0, readBuffer.Length).ConfigureAwait(false);
            if (cnt == 0)
            {
                tokenizer.ProcessEndOfStream(visitor);
                progress?.Report(0);
                return true;
            }

            // TextReader doesn't support cancellation, so it's really important that we do this
            // ourselves.  it does involve a volatile read, so don't go overboard.
            cancellationToken.ThrowIfCancellationRequested();

            int off = 0;
            int rptCnt = cnt;
            if (cnt > 0 && readBuffer[0] == '\uFEFF')
            {
                ++off;
                --cnt;
            }

            if (cnt != 0)
            {
                CsvCharsInput.ProcessSegment(tokenizer, visitor, new ReadOnlySpan<char>(readBuffer, off, cnt), encodeBuffer.Span, encodeBatchCharCount);
                progress?.Report(rptCnt);
            }

            return false;
        }
    }
}
