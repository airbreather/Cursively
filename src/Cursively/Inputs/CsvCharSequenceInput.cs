using System;
using System.Buffers;
using System.Text;

namespace Cursively.Inputs
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
        protected override void Process(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor)
        {
            if (_chars.IsSingleSegment)
            {
                CsvCharsInput.ProcessFullSegment(_chars.First.Span, _chunkCharCount, tokenizer, visitor);
                return;
            }

            int maxByteCount = Encoding.UTF8.GetMaxByteCount(_chunkCharCount);
            Span<byte> bytes = stackalloc byte[0];
            if (maxByteCount < 1024)
            {
                bytes = stackalloc byte[maxByteCount];
            }
            else
            {
                bytes = new byte[maxByteCount];
            }

            foreach (var segment in _chars)
            {
                CsvCharsInput.ProcessSegment(segment.Span, bytes, _chunkCharCount, tokenizer, visitor);
            }

            tokenizer.ProcessEndOfStream(visitor);
        }
    }
}
