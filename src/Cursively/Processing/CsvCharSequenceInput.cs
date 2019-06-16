using System;
using System.Buffers;
using System.Text;

namespace Cursively.Processing
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CsvCharSequenceInput : CsvInput
    {
        private readonly ReadOnlySequence<char> _chars;

        private readonly int _chunkCharCount;

        internal CsvCharSequenceInput(byte delimiter, ReadOnlySequence<char> chars, int chunkCharCount)
            : base(delimiter, false)
        {
            _chars = chars;
            _chunkCharCount = chunkCharCount;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public CsvCharSequenceInput WithDelimiter(byte delimiter) =>
            new CsvCharSequenceInput(delimiter, _chars, _chunkCharCount);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chunkCharCount"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public CsvCharSequenceInput WithChunkCharCount(int chunkCharCount)
        {
            if (chunkCharCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(chunkCharCount), chunkCharCount, "Must be greater than zero.");
            }

            return new CsvCharSequenceInput(Delimiter, _chars, chunkCharCount);
        }

        /// <inheritdoc />
        protected override unsafe void Process(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor)
        {
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

            fixed (byte* b = &bytes[0])
            {
                foreach (var segment in _chars)
                {
                    var charSpan = segment.Span;
                    if (charSpan.IsEmpty)
                    {
                        continue;
                    }

                    int rem = charSpan.Length;
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
                    }
                }
            }

            tokenizer.ProcessEndOfStream(visitor);
        }
    }
}
