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

        private readonly int _minReadBufferCharCount;

        private readonly int _encodeBatchCharCount;

        private readonly bool _ignoreByteOrderMark;

        private readonly ArrayPool<char> _readBufferPool;

        private readonly MemoryPool<byte> _encodeBufferPool;

        internal CsvTextReaderInput(byte delimiter, TextReader textReader, int minReadBufferCharCount, ArrayPool<char> readBufferPool, int encodeBatchCharCount, MemoryPool<byte> encodeBufferPool, bool ignoreByteOrderMark)
            : base(delimiter, requiresExplicitReset: true)
        {
            _textReader = textReader;
            _minReadBufferCharCount = minReadBufferCharCount;
            _readBufferPool = readBufferPool;
            _encodeBatchCharCount = encodeBatchCharCount;
            _encodeBufferPool = encodeBufferPool;
            _ignoreByteOrderMark = ignoreByteOrderMark;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public CsvTextReaderInput WithDelimiter(byte delimiter) =>
            new CsvTextReaderInput(delimiter, _textReader, _minReadBufferCharCount, _readBufferPool, _encodeBatchCharCount, _encodeBufferPool, _ignoreByteOrderMark);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="minReadBufferCharCount"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public CsvTextReaderInput WithMinReadBufferCharCount(int minReadBufferCharCount)
        {
            if (minReadBufferCharCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(minReadBufferCharCount), minReadBufferCharCount, "Must be greater than zero.");
            }

            return new CsvTextReaderInput(Delimiter, _textReader, minReadBufferCharCount, _readBufferPool, _encodeBatchCharCount, _encodeBufferPool, _ignoreByteOrderMark);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="readBufferPool"></param>
        /// <returns></returns>
        public CsvTextReaderInput WithReadBufferPool(ArrayPool<char> readBufferPool) =>
            new CsvTextReaderInput(Delimiter, _textReader, _minReadBufferCharCount, readBufferPool, _encodeBatchCharCount, _encodeBufferPool, _ignoreByteOrderMark);

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

            return new CsvTextReaderInput(Delimiter, _textReader, _minReadBufferCharCount, _readBufferPool, encodeBatchCharCount, _encodeBufferPool, _ignoreByteOrderMark);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="encodeBufferPool"></param>
        /// <returns></returns>
        public CsvTextReaderInput WithEncodeBufferPool(MemoryPool<byte> encodeBufferPool) =>
            new CsvTextReaderInput(Delimiter, _textReader, _minReadBufferCharCount, _readBufferPool, _encodeBatchCharCount, encodeBufferPool, _ignoreByteOrderMark);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ignoreByteOrderMark"></param>
        /// <returns></returns>
        public CsvTextReaderInput WithIgnoreByteOrderMark(bool ignoreByteOrderMark) =>
            new CsvTextReaderInput(Delimiter, _textReader, _minReadBufferCharCount, _readBufferPool, _encodeBatchCharCount, _encodeBufferPool, ignoreByteOrderMark);

        /// <inheritdoc />
        protected override void Process(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor)
        {
            var textReader = _textReader;
            int minReadBufferCharCount = _minReadBufferCharCount;
            var readBufferPool = _readBufferPool;
            int encodeBatchCharCount = _encodeBatchCharCount;
            var encodeBufferPool = _encodeBufferPool;

            IMemoryOwner<byte> encodeBufferOwner = null;

            char[] readBuffer;
            if (readBufferPool is null)
            {
                readBuffer = new char[minReadBufferCharCount];
            }
            else
            {
                readBuffer = readBufferPool.Rent(minReadBufferCharCount);
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
            int minReadBufferCharCount = _minReadBufferCharCount;
            var readBufferPool = _readBufferPool;
            int encodeBatchCharCount = _encodeBatchCharCount;
            var encodeBufferPool = _encodeBufferPool;

            IMemoryOwner<byte> encodeBufferOwner = null;

            char[] readBuffer;
            if (readBufferPool is null)
            {
                readBuffer = new char[minReadBufferCharCount];
            }
            else
            {
                readBuffer = readBufferPool.Rent(minReadBufferCharCount);
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

                int readLength;
                while ((readLength = await textReader.ReadAsync(readBuffer, 0, readBuffer.Length).ConfigureAwait(false)) != 0)
                {
                    // TextReader doesn't support cancellation, so it's really important that we do this
                    // ourselves.  it does involve a volatile read, so don't go overboard.
                    cancellationToken.ThrowIfCancellationRequested();

                    CsvCharsInput.ProcessSegment(tokenizer, visitor, new ReadOnlySpan<char>(readBuffer, 0, readLength), encodeBuffer.Span, encodeBatchCharCount);
                    progress?.Report(readLength);
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
            int charCount = _textReader.Read(readBuffer, 0, readBuffer.Length);
            if (charCount == 0)
            {
                tokenizer.ProcessEndOfStream(visitor);
                return true;
            }

            int charOffset = 0;
            if (charCount > 0 && readBuffer[0] == '\uFEFF')
            {
                charOffset = 1;
            }

            if (charOffset < charCount)
            {
                CsvCharsInput.ProcessSegment(tokenizer, visitor, new ReadOnlySpan<char>(readBuffer, charOffset, charCount - charOffset), encodeBuffer, encodeBatchCharCount);
            }

            return false;
        }

        private async ValueTask<bool> EatBOMAsync(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor, char[] readBuffer, Memory<byte> encodeBuffer, int encodeBatchCharCount, IProgress<int> progress, CancellationToken cancellationToken)
        {
            int charCount = await _textReader.ReadAsync(readBuffer, 0, readBuffer.Length).ConfigureAwait(false);
            if (charCount == 0)
            {
                tokenizer.ProcessEndOfStream(visitor);
                progress?.Report(0);
                return true;
            }

            // TextReader doesn't support cancellation, so it's really important that we do this
            // ourselves.  it does involve a volatile read, so don't go overboard.
            cancellationToken.ThrowIfCancellationRequested();

            int charOffset = 0;
            if (charCount > 0 && readBuffer[0] == '\uFEFF')
            {
                charOffset = 1;
            }

            if (charOffset < charCount)
            {
                CsvCharsInput.ProcessSegment(tokenizer, visitor, new ReadOnlySpan<char>(readBuffer, charOffset, charCount - charOffset), encodeBuffer.Span, encodeBatchCharCount);
                progress?.Report(charCount);
            }

            return false;
        }
    }
}
