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

        private readonly int _encodeBatchCharCount;

        private readonly bool _ignoreByteOrderMark;

        internal CsvCharSequenceInput(byte delimiter, ReadOnlySequence<char> chars, int encodeBatchCharCount, bool ignoreByteOrderMark)
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
        public CsvCharSequenceInput WithDelimiter(byte delimiter) =>
            new CsvCharSequenceInput(delimiter, _chars, _encodeBatchCharCount, _ignoreByteOrderMark);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="encodeBatchCharCount"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public CsvCharSequenceInput WithEncodeBatchCharCount(int encodeBatchCharCount)
        {
            if (encodeBatchCharCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(encodeBatchCharCount), encodeBatchCharCount, "Must be greater than zero.");
            }

            return new CsvCharSequenceInput(Delimiter, _chars, encodeBatchCharCount, _ignoreByteOrderMark);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ignoreByteOrderMark"></param>
        /// <returns></returns>
        public CsvCharSequenceInput WithIgnoreByteOrderMark(bool ignoreByteOrderMark) =>
            new CsvCharSequenceInput(Delimiter, _chars, _encodeBatchCharCount, ignoreByteOrderMark);

        /// <inheritdoc />
        protected override void Process(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor)
        {
            int encodeBatchCharCount = _encodeBatchCharCount;
            if (_chars.IsSingleSegment)
            {
                CsvCharsInput.ProcessFullSegment(_chars.First.Span, encodeBatchCharCount, _ignoreByteOrderMark, tokenizer, visitor);
                return;
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

            var enumerator = _chars.GetEnumerator();
            if (_ignoreByteOrderMark && EatBOM(tokenizer, visitor, encodeBatchCharCount, encodeBuffer, ref enumerator))
            {
                return;
            }

            while (enumerator.MoveNext())
            {
                var segment = enumerator.Current;
                if (!segment.IsEmpty)
                {
                    CsvCharsInput.ProcessSegment(tokenizer, visitor, segment.Span, encodeBuffer, encodeBatchCharCount);
                }
            }
        }

        private static bool EatBOM(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor, int encodeBatchCharCount, Span<byte> encodeBuffer, ref ReadOnlySequence<char>.Enumerator enumerator)
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
                    CsvCharsInput.ProcessSegment(tokenizer, visitor, span, encodeBuffer, encodeBatchCharCount);
                }

                return false;
            }

            return true;
        }
    }
}
