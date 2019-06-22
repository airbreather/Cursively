using System;
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

        internal CsvTextReaderInput(byte delimiter, TextReader textReader, int readBufferCharCount, int encodeBatchCharCount, bool ignoreByteOrderMark)
            : base(delimiter, true)
        {
            _textReader = textReader;
            _readBufferCharCount = readBufferCharCount;
            _encodeBatchCharCount = encodeBatchCharCount;
            _ignoreByteOrderMark = ignoreByteOrderMark;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public CsvTextReaderInput WithDelimiter(byte delimiter) =>
            new CsvTextReaderInput(delimiter, _textReader, _readBufferCharCount, _encodeBatchCharCount, _ignoreByteOrderMark);

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

            return new CsvTextReaderInput(Delimiter, _textReader, readBufferCharCount, _encodeBatchCharCount, _ignoreByteOrderMark);
        }

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

            return new CsvTextReaderInput(Delimiter, _textReader, _readBufferCharCount, encodeBatchCharCount, _ignoreByteOrderMark);
        }

        /// <inheritdoc />
        protected override void Process(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor)
        {
            var textReader = _textReader;
            int encodeBatchCharCount = _encodeBatchCharCount;

            char[] readBuffer = new char[_readBufferCharCount];

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
            else
            {
                encodeBuffer = new byte[encodeBufferLength];
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

            tokenizer.ProcessEndOfStream(visitor);
        }

        /// <inheritdoc />s
        protected override async ValueTask ProcessAsync(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor, IProgress<int> progress, CancellationToken cancellationToken)
        {
            // TextReader doesn't support cancellation, so it's really important that we do this
            // ourselves.  it does involve a volatile read, so don't go overboard.
            cancellationToken.ThrowIfCancellationRequested();

            var textReader = _textReader;
            int encodeBatchCharCount = _encodeBatchCharCount;

            char[] readBuffer = new char[_readBufferCharCount];

            int encodeBufferLength = Encoding.UTF8.GetMaxByteCount(encodeBatchCharCount);
            byte[] encodeBuffer = new byte[encodeBufferLength];

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

                CsvCharsInput.ProcessSegment(tokenizer, visitor, new ReadOnlySpan<char>(readBuffer, 0, cnt), encodeBuffer, encodeBatchCharCount);
                progress?.Report(cnt);
            }

            tokenizer.ProcessEndOfStream(visitor);
            progress?.Report(0);
        }

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

        private async ValueTask<bool> EatBOMAsync(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor, char[] readBuffer, byte[] encodeBuffer, int encodeBatchCharCount, IProgress<int> progress, CancellationToken cancellationToken)
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
                CsvCharsInput.ProcessSegment(tokenizer, visitor, new ReadOnlySpan<char>(readBuffer, off, cnt), encodeBuffer, encodeBatchCharCount);
                progress?.Report(rptCnt);
            }

            return false;
        }
    }
}
