using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;

namespace Cursively.Inputs
{
    /// <summary>
    /// Implementation of <see cref="CsvSyncInputBase"/> backed by a file from the filesystem that
    /// will be processed by mapping it into virtual memory and then treating it like a contiguous
    /// array of bytes.
    /// </summary>
    public sealed class CsvMemoryMappedFileInput : CsvSyncInputBase
    {
        private readonly byte _delimiter;

        private readonly string _csvFilePath;

        private readonly bool _ignoreUTF8ByteOrderMark;

        internal CsvMemoryMappedFileInput(byte delimiter, string csvFilePath, bool ignoreUTF8ByteOrderMark)
        {
            _delimiter = delimiter;
            _csvFilePath = csvFilePath;
            _ignoreUTF8ByteOrderMark = ignoreUTF8ByteOrderMark;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="CsvMemoryMappedFileInput"/> class as a copy of this
        /// one, with the given delimiter.
        /// </summary>
        /// <param name="delimiter">
        /// The delimiter to use.  Use <see cref="CsvTokenizer.IsValidDelimiter"/> to test whether
        /// or not a particular value is valid.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="CsvMemoryMappedFileInput"/> class as a copy of this one, with
        /// the given delimiter.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="delimiter"/> is one of the illegal values.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="CsvSyncInputBase.Process"/> has already been called.
        /// </exception>
        public CsvMemoryMappedFileInput WithDelimiter(byte delimiter)
        {
            if (!CsvTokenizer.IsValidDelimiter(delimiter))
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new ArgumentException("Must not be a carriage return, linefeed, or double-quote.", nameof(delimiter));
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            ThrowIfProcessingHasAlreadyStarted();
            return new CsvMemoryMappedFileInput(delimiter, _csvFilePath, _ignoreUTF8ByteOrderMark);
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
        public CsvMemoryMappedFileInput WithIgnoreUTF8ByteOrderMark(bool ignoreUTF8ByteOrderMark)
        {
            ThrowIfProcessingHasAlreadyStarted();
            return new CsvMemoryMappedFileInput(_delimiter, _csvFilePath, ignoreUTF8ByteOrderMark);
        }

        /// <inheritdoc />
        protected override unsafe void ProcessCore(CsvReaderVisitorBase visitor)
        {
            var tokenizer = new CsvTokenizer(_delimiter);

            using (var fl = new FileStream(_csvFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan))
            {
                long length = fl.Length;
                if (length == 0)
                {
                    tokenizer.ProcessEndOfStream(visitor);
                    return;
                }

                using (var memoryMappedFile = MemoryMappedFile.CreateFromFile(fl, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, leaveOpen: true))
                using (var accessor = memoryMappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read))
                {
                    var handle = accessor.SafeMemoryMappedViewHandle;
                    byte* ptr = null;
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try
                    {
                        handle.AcquirePointer(ref ptr);

                        if (_ignoreUTF8ByteOrderMark)
                        {
                            var head = new ReadOnlySpan<byte>(UTF8BOM, 0, length < 3 ? unchecked((int)length) : 3);
                            if (head.SequenceEqual(new ReadOnlySpan<byte>(ptr, head.Length)))
                            {
                                length -= head.Length;
                                ptr += head.Length;
                            }
                        }

                        while (length > int.MaxValue)
                        {
                            tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(ptr, int.MaxValue), visitor);
                            length -= int.MaxValue;
                            ptr += int.MaxValue;
                        }

                        tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(ptr, unchecked((int)length)), visitor);
                        tokenizer.ProcessEndOfStream(visitor);
                    }
                    finally
                    {
                        if (ptr != null)
                        {
                            handle.ReleasePointer();
                        }
                    }
                }
            }
        }
    }
}
