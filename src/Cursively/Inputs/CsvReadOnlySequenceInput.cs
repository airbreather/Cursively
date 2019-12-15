using System;
using System.Buffers;

namespace Cursively.Inputs
{
    /// <summary>
    /// Implementation of <see cref="CsvSyncInputBase"/> backed by a
    /// <see cref="ReadOnlySequence{T}"/> of bytes.
    /// </summary>
    public sealed class CsvReadOnlySequenceInput : CsvSyncInputBase
    {
        private readonly byte _delimiter;

        private readonly ReadOnlySequence<byte> _sequence;

        private readonly bool _ignoreUTF8ByteOrderMark;

        internal CsvReadOnlySequenceInput(byte delimiter, ReadOnlySequence<byte> sequence, bool ignoreUTF8ByteOrderMark)
        {
            _delimiter = delimiter;
            _sequence = sequence;
            _ignoreUTF8ByteOrderMark = ignoreUTF8ByteOrderMark;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="CsvReadOnlySequenceInput"/> class as a copy of this
        /// one, with the given delimiter.
        /// </summary>
        /// <param name="delimiter">
        /// The delimiter to use.  Use <see cref="CsvTokenizer.IsValidDelimiter"/> to test whether
        /// or not a particular value is valid.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="CsvReadOnlySequenceInput"/> class as a copy of this one, with
        /// the given delimiter.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="delimiter"/> is one of the illegal values.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="CsvSyncInputBase.Process"/> has already been called.
        /// </exception>
        public CsvReadOnlySequenceInput WithDelimiter(byte delimiter)
        {
            if (!CsvTokenizer.IsValidDelimiter(delimiter))
            {
                throw new ArgumentException("Must not be a carriage return, linefeed, or double-quote.", nameof(delimiter));
            }

            ThrowIfProcessingHasAlreadyStarted();
            return new CsvReadOnlySequenceInput(delimiter, _sequence, _ignoreUTF8ByteOrderMark);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="CsvReadOnlySequenceInput"/> class as a copy of this
        /// one, with the given flag indicating whether or not a leading UTF-8 byte order mark, if
        /// present, should be omitted from the first field's data.
        /// </summary>
        /// <param name="ignoreUTF8ByteOrderMark">
        /// A value indicating whether or not a leading UTF-8 byte order mark, if present, should be
        /// omitted from the first field's data.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="CsvReadOnlySequenceInput"/> class as a copy of this one, with
        /// the given flag indicating whether or not a leading UTF-8 byte order mark, if present,
        /// should be omitted from the first field's data.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="CsvSyncInputBase.Process"/> has already been called.
        /// </exception>
        public CsvReadOnlySequenceInput WithIgnoreUTF8ByteOrderMark(bool ignoreUTF8ByteOrderMark)
        {
            ThrowIfProcessingHasAlreadyStarted();
            return new CsvReadOnlySequenceInput(_delimiter, _sequence, ignoreUTF8ByteOrderMark);
        }

        /// <inheritdoc />
        protected override void ProcessCore(CsvReaderVisitorBase visitor)
        {
            var tokenizer = new CsvTokenizer(_delimiter);
            bool ignoreUTF8ByteOrderMark = _ignoreUTF8ByteOrderMark;
            var bytes = _sequence;

            if (bytes.IsSingleSegment)
            {
                CsvReadOnlyMemoryInput.ProcessFullSegment(bytes.First.Span, ignoreUTF8ByteOrderMark, tokenizer, visitor);
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
            if (span.Length >= 3)
            {
                if (span.StartsWith(head))
                {
                    span = span.Slice(3);
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
                    if (++alreadyEaten == 3)
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
