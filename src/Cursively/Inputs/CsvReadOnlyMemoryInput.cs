using System;

namespace Cursively.Inputs
{
    /// <summary>
    /// Implementation of <see cref="CsvSyncInputBase"/> backed by a <see cref="ReadOnlyMemory{T}"/>
    /// of bytes.
    /// </summary>
    public sealed class CsvReadOnlyMemoryInput : CsvSyncInputBase
    {
        private readonly byte _delimiter;

        private readonly ReadOnlyMemory<byte> _memory;

        private readonly bool _ignoreUTF8ByteOrderMark;

        internal CsvReadOnlyMemoryInput(byte delimiter, ReadOnlyMemory<byte> memory, bool ignoreUTF8ByteOrderMark)
        {
            _delimiter = delimiter;
            _memory = memory;
            _ignoreUTF8ByteOrderMark = ignoreUTF8ByteOrderMark;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="CsvReadOnlyMemoryInput"/> class as a copy of this
        /// one, with the given delimiter.
        /// </summary>
        /// <param name="delimiter">
        /// The delimiter to use.  Use <see cref="CsvTokenizer.IsValidDelimiter"/> to test whether
        /// or not a particular value is valid.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="CsvReadOnlyMemoryInput"/> class as a copy of this one, with
        /// the given delimiter.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="delimiter"/> is one of the illegal values.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="CsvSyncInputBase.Process"/> has already been called.
        /// </exception>
        public CsvReadOnlyMemoryInput WithDelimiter(byte delimiter)
        {
            if (!CsvTokenizer.IsValidDelimiter(delimiter))
            {
                throw new ArgumentException("Must not be a carriage return, linefeed, or double-quote.", nameof(delimiter));
            }

            ThrowIfProcessingHasAlreadyStarted();
            return new CsvReadOnlyMemoryInput(delimiter, _memory, _ignoreUTF8ByteOrderMark);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="CsvReadOnlyMemoryInput"/> class as a copy of this
        /// one, with the given flag indicating whether or not a leading UTF-8 byte order mark, if
        /// present, should be omitted from the first field's data.
        /// </summary>
        /// <param name="ignoreUTF8ByteOrderMark">
        /// A value indicating whether or not a leading UTF-8 byte order mark, if present, should be
        /// omitted from the first field's data.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="CsvReadOnlyMemoryInput"/> class as a copy of this one, with
        /// the given flag indicating whether or not a leading UTF-8 byte order mark, if present,
        /// should be omitted from the first field's data.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="CsvSyncInputBase.Process"/> has already been called.
        /// </exception>
        public CsvReadOnlyMemoryInput WithIgnoreUTF8ByteOrderMark(bool ignoreUTF8ByteOrderMark)
        {
            ThrowIfProcessingHasAlreadyStarted();
            return new CsvReadOnlyMemoryInput(_delimiter, _memory, ignoreUTF8ByteOrderMark);
        }

        /// <inheritdoc />
        protected override void ProcessCore(CsvReaderVisitorBase visitor)
        {
            ProcessFullSegment(_memory.Span, _ignoreUTF8ByteOrderMark, new CsvTokenizer(_delimiter), visitor);
        }

        internal static void ProcessFullSegment(ReadOnlySpan<byte> bytes, bool ignoreUTF8ByteOrderMark, CsvTokenizer tokenizer, CsvReaderVisitorBase visitor)
        {
            if (ignoreUTF8ByteOrderMark)
            {
                var head = new ReadOnlySpan<byte>(UTF8BOM, 0, bytes.Length < 3 ? bytes.Length : 3);
                if (bytes.StartsWith(head))
                {
                    bytes = bytes.Slice(head.Length);
                }
            }

            tokenizer.ProcessNextChunk(bytes, visitor);
            tokenizer.ProcessEndOfStream(visitor);
        }
    }
}
