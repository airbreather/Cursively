﻿using System;
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
                tokenizer.ProcessNextChunk(enumerator.Current.Span, visitor);
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

            ReadOnlySpan<byte> head = UTF8BOM;

            // this greed should **probably** pay off most of the time.
            if (span.Length >= head.Length)
            {
                if (span.StartsWith(head))
                {
                    span = span.Slice(head.Length);
                }

                tokenizer.ProcessNextChunk(span, visitor);
                return false;
            }

            int alreadyEaten = 0;
            while (true)
            {
                if (span[0] == head[alreadyEaten])
                {
                    span = span.Slice(1);
                    if (++alreadyEaten == head.Length)
                    {
                        tokenizer.ProcessNextChunk(span, visitor);
                        return false;
                    }
                }
                else
                {
                    tokenizer.ProcessNextChunk(head.Slice(0, alreadyEaten), visitor);
                    tokenizer.ProcessNextChunk(span, visitor);
                    return false;
                }

                if (span.IsEmpty)
                {
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

                    span = segment.Span;
                }
            }
        }
    }
}
