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

        private readonly int _chunkCharCount;

        private readonly bool _ignoreByteOrderMark;

        internal CsvCharsInput(byte delimiter, ReadOnlyMemory<char> chars, int chunkCharCount, bool ignoreByteOrderMark)
            : base(delimiter, false)
        {
            _chars = chars;
            _chunkCharCount = chunkCharCount;
            _ignoreByteOrderMark = ignoreByteOrderMark;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public CsvCharsInput WithDelimiter(byte delimiter) =>
            new CsvCharsInput(delimiter, _chars, _chunkCharCount, _ignoreByteOrderMark);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chunkCharCount"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public CsvCharsInput WithChunkCharCount(int chunkCharCount)
        {
            if (chunkCharCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(chunkCharCount), chunkCharCount, "Must be greater than zero.");
            }

            return new CsvCharsInput(Delimiter, _chars, chunkCharCount, _ignoreByteOrderMark);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ignoreByteOrderMark"></param>
        /// <returns></returns>
        public CsvCharsInput WithIgnoreByteOrderMark(bool ignoreByteOrderMark) =>
            new CsvCharsInput(Delimiter, _chars, _chunkCharCount, ignoreByteOrderMark);

        /// <inheritdoc />
        protected override unsafe void Process(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor)
        {
            ProcessFullSegment(_chars.Span, _chunkCharCount, _ignoreByteOrderMark, tokenizer, visitor);
        }

        internal static unsafe void ProcessFullSegment(ReadOnlySpan<char> chars, int chunkCharCount, bool ignoreByteOrderMark, CsvTokenizer tokenizer, CsvReaderVisitorBase visitor)
        {
            if (ignoreByteOrderMark && !chars.IsEmpty && chars[0] == '\uFEFF')
            {
                chars = chars.Slice(1);
            }

            if (chars.IsEmpty)
            {
                return;
            }

            if (chunkCharCount > chars.Length)
            {
                chunkCharCount = chars.Length;
            }

            int maxByteCount = Encoding.UTF8.GetMaxByteCount(chunkCharCount);
            Span<byte> bytes = stackalloc byte[0];
            if (maxByteCount < 1024)
            {
                bytes = stackalloc byte[maxByteCount];
            }
            else
            {
                bytes = new byte[maxByteCount];
            }

            ProcessSegment(tokenizer, visitor, chars, bytes, chunkCharCount);

            tokenizer.ProcessEndOfStream(visitor);
        }

        internal static unsafe void ProcessSegment(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor, ReadOnlySpan<char> chars, Span<byte> bytes, int chunkCharCount)
        {
            var encoding = Encoding.UTF8;

            int rem = chars.Length;
            fixed (byte* b = &bytes[0])
            fixed (char* cHead = &chars[0])
            {
                char* c = cHead;

                while (rem > chunkCharCount)
                {
                    int byteCount = encoding.GetByteCount(c, chunkCharCount);
                    encoding.GetBytes(c, chunkCharCount, b, byteCount);
                    tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(b, byteCount), visitor);

                    rem -= chunkCharCount;
                    c += chunkCharCount;
                }

                if (rem > 0)
                {
                    int byteCount = encoding.GetByteCount(c, rem);
                    encoding.GetBytes(c, rem, b, byteCount);
                    tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(b, byteCount), visitor);
                }
            }
        }
    }
}
