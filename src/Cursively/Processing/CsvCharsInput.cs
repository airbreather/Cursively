using System;
using System.Text;

namespace Cursively.Processing
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CsvCharsInput : CsvInput
    {
        private readonly ReadOnlyMemory<char> _chars;

        private readonly int _chunkCharCount;

        internal CsvCharsInput(byte delimiter, ReadOnlyMemory<char> chars, int chunkCharCount)
            : base(delimiter, false)
        {
            _chars = chars;
            _chunkCharCount = chunkCharCount < chars.Length
                ? chunkCharCount
                : chars.Length;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public CsvCharsInput WithDelimiter(byte delimiter) =>
            new CsvCharsInput(delimiter, _chars, _chunkCharCount);

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

            return new CsvCharsInput(Delimiter, _chars, chunkCharCount);
        }

        /// <inheritdoc />
        protected override unsafe void Process(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor)
        {
            var charSpan = _chars.Span;
            if (charSpan.IsEmpty)
            {
                return;
            }

            int chunkCharCount = _chunkCharCount;

            var encoding = Encoding.UTF8;

            int maxByteCount = encoding.GetMaxByteCount(chunkCharCount);
            Span<byte> bytes = stackalloc byte[0];
            if (maxByteCount < 1024)
            {
                bytes = stackalloc byte[maxByteCount];
            }
            else
            {
                bytes = new byte[maxByteCount];
            }

            int rem = charSpan.Length;
            fixed (byte* b = &bytes[0])
            fixed (char* cHead = &charSpan[0])
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

                tokenizer.ProcessEndOfStream(visitor);
            }
        }
    }
}
