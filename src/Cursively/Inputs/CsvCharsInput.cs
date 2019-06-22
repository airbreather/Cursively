using System;
using System.Text;

namespace Cursively.Inputs
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CsvCharsInput : CsvInput
    {
        private readonly ReadOnlyMemory<char> _chars;

        private readonly int _encodeBatchCharCount;

        private readonly bool _ignoreByteOrderMark;

        internal CsvCharsInput(byte delimiter, ReadOnlyMemory<char> chars, int encodeBatchCharCount, bool ignoreByteOrderMark)
            : base(delimiter, false)
        {
            _chars = chars;
            _encodeBatchCharCount = encodeBatchCharCount;
            _ignoreByteOrderMark = ignoreByteOrderMark;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public CsvCharsInput WithDelimiter(byte delimiter) =>
            new CsvCharsInput(delimiter, _chars, _encodeBatchCharCount, _ignoreByteOrderMark);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="encodeBatchCharCount"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public CsvCharsInput WithEncodeBatchCharCount(int encodeBatchCharCount)
        {
            if (encodeBatchCharCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(encodeBatchCharCount), encodeBatchCharCount, "Must be greater than zero.");
            }

            return new CsvCharsInput(Delimiter, _chars, encodeBatchCharCount, _ignoreByteOrderMark);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ignoreByteOrderMark"></param>
        /// <returns></returns>
        public CsvCharsInput WithIgnoreByteOrderMark(bool ignoreByteOrderMark) =>
            new CsvCharsInput(Delimiter, _chars, _encodeBatchCharCount, ignoreByteOrderMark);

        /// <inheritdoc />
        protected override unsafe void Process(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor)
        {
            ProcessFullSegment(_chars.Span, _encodeBatchCharCount, _ignoreByteOrderMark, tokenizer, visitor);
        }

        internal static unsafe void ProcessFullSegment(ReadOnlySpan<char> chars, int encodeBatchCharCount, bool ignoreByteOrderMark, CsvTokenizer tokenizer, CsvReaderVisitorBase visitor)
        {
            if (ignoreByteOrderMark && !chars.IsEmpty && chars[0] == '\uFEFF')
            {
                chars = chars.Slice(1);
            }

            if (chars.IsEmpty)
            {
                return;
            }

            if (encodeBatchCharCount > chars.Length)
            {
                encodeBatchCharCount = chars.Length;
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

            ProcessSegment(tokenizer, visitor, chars, encodeBuffer, encodeBatchCharCount);
            tokenizer.ProcessEndOfStream(visitor);
        }

        internal static unsafe void ProcessSegment(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor, ReadOnlySpan<char> chars, Span<byte> encodeBuffer, int encodeBatchCharCount)
        {
            var encoding = Encoding.UTF8;

            int remainingCharCount = chars.Length;
            fixed (byte* encodePtr = &encodeBuffer[0])
            fixed (char* decodePtrFixed = &chars[0])
            {
                char* decodePtr = decodePtrFixed;

                while (remainingCharCount > encodeBatchCharCount)
                {
                    int encodeByteCount = encoding.GetByteCount(decodePtr, encodeBatchCharCount);
                    encoding.GetBytes(decodePtr, encodeBatchCharCount, encodePtr, encodeByteCount);
                    tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(encodePtr, encodeByteCount), visitor);

                    remainingCharCount -= encodeBatchCharCount;
                    decodePtr += encodeBatchCharCount;
                }

                if (remainingCharCount > 0)
                {
                    int encodeByteCount = encoding.GetByteCount(decodePtr, remainingCharCount);
                    encoding.GetBytes(decodePtr, remainingCharCount, encodePtr, encodeByteCount);
                    tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(encodePtr, encodeByteCount), visitor);
                }
            }
        }
    }
}
