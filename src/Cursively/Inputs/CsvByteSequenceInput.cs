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
            ReadOnlyMemory<byte> segment;
            while (true)
            {
                if (!enumerator.MoveNext())
                {
                    tokenizer.ProcessEndOfStream(visitor);
                    return true;
                }

                segment = enumerator.Current;
                if (!segment.IsEmpty)
                {
                    break;
                }
            }

            var span = segment.Span;

            // this greed should **probably** pay off most of the time.
            if (span.Length >= 3)
            {
                if (span[0] == 0xEF &&
                    span[1] == 0xBB &&
                    span[2] == 0xBF)
                {
                    span = span.Slice(3);

                    if (span.IsEmpty)
                    {
                        return false;
                    }
                }

                tokenizer.ProcessNextChunk(span, visitor);
                return false;
            }

            ReadOnlySpan<byte> head = stackalloc byte[] { 0xEF, 0xBB, 0xBF };
            int alreadyEaten = 0;
            while (true)
            {
                if (span[0] == head[alreadyEaten])
                {
                    span = span.Slice(1);
                    if (++alreadyEaten == 3)
                    {
                        if (!span.IsEmpty)
                        {
                            tokenizer.ProcessNextChunk(span, visitor);
                        }

                        return false;
                    }
                }
                else
                {
                    if (alreadyEaten != 0)
                    {
                        tokenizer.ProcessNextChunk(head.Slice(0, alreadyEaten), visitor);
                    }

                    tokenizer.ProcessNextChunk(span, visitor);
                    return false;
                }

                if (span.IsEmpty)
                {
                    while (true)
                    {
                        if (!enumerator.MoveNext())
                        {
                            if (alreadyEaten != 0)
                            {
                                tokenizer.ProcessNextChunk(head.Slice(0, alreadyEaten), visitor);
                            }

                            tokenizer.ProcessEndOfStream(visitor);
                            return true;
                        }

                        segment = enumerator.Current;
                        if (!segment.IsEmpty)
                        {
                            break;
                        }
                    }

                    span = segment.Span;
                }
            }
        }
    }
}
