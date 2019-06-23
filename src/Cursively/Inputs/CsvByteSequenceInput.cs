using System;
using System.Buffers;

namespace Cursively.Inputs
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CsvByteSequenceInput : CsvInput
    {
        private readonly ReadOnlySequence<byte> _bytes;

        private readonly bool _ignoreUTF8ByteOrderMark;

        internal CsvByteSequenceInput(byte delimiter, ReadOnlySequence<byte> bytes, bool ignoreUTF8ByteOrderMark)
            : base(delimiter, requiresExplicitReset: false)
        {
            _bytes = bytes;
            _ignoreUTF8ByteOrderMark = ignoreUTF8ByteOrderMark;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public CsvByteSequenceInput WithDelimiter(byte delimiter) =>
            new CsvByteSequenceInput(delimiter, _bytes, _ignoreUTF8ByteOrderMark);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ignoreUTF8ByteOrderMark"></param>
        /// <returns></returns>
        public CsvByteSequenceInput WithIgnoreUTF8ByteOrderMark(bool ignoreUTF8ByteOrderMark) =>
            new CsvByteSequenceInput(Delimiter, _bytes, ignoreUTF8ByteOrderMark);

        /// <inheritdoc />
        protected override void Process(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor)
        {
            bool ignoreUTF8ByteOrderMark = _ignoreUTF8ByteOrderMark;
            var bytes = _bytes;

            if (bytes.IsSingleSegment)
            {
                CsvBytesInput.ProcessFullSegment(bytes.First.Span, ignoreUTF8ByteOrderMark, tokenizer, visitor);
                return;
            }

            var enumerator = bytes.GetEnumerator();
            if (ignoreUTF8ByteOrderMark && EatUTF8BOM(tokenizer, visitor, ref enumerator))
            {
                return;
            }

            while (enumerator.MoveNext())
            {
                var segment = enumerator.Current;
                if (!segment.IsEmpty)
                {
                    tokenizer.ProcessNextChunk(segment.Span, visitor);
                }
            }

            tokenizer.ProcessEndOfStream(visitor);
        }

        private static bool EatUTF8BOM(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor, ref ReadOnlySequence<byte>.Enumerator enumerator)
        {
            // greedily optimize for the case where the first non-empty segment has 3 or more bytes.
            ReadOnlyMemory<byte> firstNonEmptySegment;
            while (true)
            {
                if (!enumerator.MoveNext())
                {
                    tokenizer.ProcessEndOfStream(visitor);
                    return true;
                }

                firstNonEmptySegment = enumerator.Current;
                if (!firstNonEmptySegment.IsEmpty)
                {
                    break;
                }
            }

            var span = firstNonEmptySegment.Span;
            if (span.Length >= 3)
            {
                if (span[0] == 0xEF &&
                    span[1] == 0xBB &&
                    span[2] == 0xBF)
                {
                    span = span.Slice(3);
                }

                tokenizer.ProcessNextChunk(span, visitor);
                return false;
            }

            byte nextByteOfUTF8BOM = 0xEF;
            while (true)
            {
                if (span[0] == nextByteOfUTF8BOM)
                {
                    span = span.Slice(1);
                    switch (nextByteOfUTF8BOM)
                    {
                        case 0xEF:
                            nextByteOfUTF8BOM = 0xBB;
                            break;

                        case 0xBB:
                            nextByteOfUTF8BOM = 0xBF;
                            break;

                        default:
                            if (!span.IsEmpty)
                            {
                                tokenizer.ProcessNextChunk(span, visitor);
                            }

                            return false;
                    }
                }
                else if (nextByteOfUTF8BOM == 0xEF)
                {
                    tokenizer.ProcessNextChunk(span, visitor);
                    return false;
                }
                else
                {
                    ReadOnlySpan<byte> head = stackalloc byte[] { 0xEF, 0xBB };
                    if (nextByteOfUTF8BOM == 0xBB)
                    {
                        head = head.Slice(0, 1);
                    }

                    tokenizer.ProcessNextChunk(head, visitor);
                    tokenizer.ProcessNextChunk(span, visitor);
                    return false;
                }

                while (span.IsEmpty)
                {
                    if (!enumerator.MoveNext())
                    {
                        ReadOnlySpan<byte> head = stackalloc byte[] { 0xEF, 0xBB };
                        if (nextByteOfUTF8BOM == 0xBB)
                        {
                            head = head.Slice(0, 1);
                        }

                        tokenizer.ProcessNextChunk(head, visitor);
                        tokenizer.ProcessEndOfStream(visitor);
                        return true;
                    }

                    span = enumerator.Current.Span;
                }
            }
        }
    }
}
