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

        private readonly bool _ignoreByteOrderMark;

        internal CsvCharSequenceInput(byte delimiter, ReadOnlySequence<char> chars, int chunkCharCount, bool ignoreByteOrderMark)
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
        public CsvCharSequenceInput WithDelimiter(byte delimiter) =>
            new CsvCharSequenceInput(delimiter, _chars, _chunkCharCount, _ignoreByteOrderMark);

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

            return new CsvCharSequenceInput(Delimiter, _chars, chunkCharCount, _ignoreByteOrderMark);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ignoreByteOrderMark"></param>
        /// <returns></returns>
        public CsvCharSequenceInput WithIgnoreByteOrderMark(bool ignoreByteOrderMark) =>
            new CsvCharSequenceInput(Delimiter, _chars, _chunkCharCount, ignoreByteOrderMark);

        /// <inheritdoc />
        protected override void Process(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor)
        {
            if (_chars.IsSingleSegment)
            {
                CsvCharsInput.ProcessFullSegment(_chars.First.Span, _chunkCharCount, _ignoreByteOrderMark, tokenizer, visitor);
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

            var enumerator = _chars.GetEnumerator();
            if (_ignoreByteOrderMark && EatBOM(tokenizer, visitor, _chunkCharCount, bytes, ref enumerator))
            {
                return;
            }

            while (enumerator.MoveNext())
            {
                CsvCharsInput.ProcessSegment(tokenizer, visitor, enumerator.Current.Span, bytes, _chunkCharCount);
            }
        }

        private static bool EatBOM(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor, int chunkCharCount, Span<byte> bytes, ref ReadOnlySequence<char>.Enumerator enumerator)
        {
            while (enumerator.MoveNext())
            {
                var segment = enumerator.Current;
                if (segment.IsEmpty)
                {
                    continue;
                }

                var span = segment.Span;
                if (span[0] == '\uFEFF')
                {
                    span = span.Slice(1);
                }

                if (!span.IsEmpty)
                {
                    CsvCharsInput.ProcessSegment(tokenizer, visitor, span, bytes, chunkCharCount);
                }

                return false;
            }

            return true;
        }
    }
}
