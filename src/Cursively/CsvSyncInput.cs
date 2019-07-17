using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;

using Cursively.Inputs;

namespace Cursively
{
    /// <summary>
    /// Helpers to create inputs that describe CSV data streams synchronously.
    /// </summary>
    public static class CsvSyncInput
    {
        /// <summary>
        /// Creates an input that can describe the contents of a given <see cref="Stream"/> to an
        /// instance of <see cref="CsvReaderVisitorBase"/>, synchronously.
        /// </summary>
        /// <param name="csvStream">
        /// The <see cref="Stream"/> that contains the CSV data.
        /// </param>
        /// <returns>
        /// An instance of <see cref="CsvSyncStreamInput"/> wrapping <paramref name="csvStream"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="csvStream"/> is non-<see langword="null"/> and its
        /// <see cref="Stream.CanRead"/> is <see langword="false"/>.
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")] // Microsoft.CodeAnalysis.FxCopAnalyzers 2.9.3 has a false positive.  Remove when fixed
        public static CsvSyncStreamInput ForStream(Stream csvStream)
        {
            csvStream = csvStream ?? Stream.Null;
            if (!csvStream.CanRead)
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new ArgumentException("Stream does not support reading.", nameof(csvStream));
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            return new CsvSyncStreamInput((byte)',', csvStream, 65536, ArrayPool<byte>.Shared, true);
        }

        /// <summary>
        /// Creates an input that can describe the contents of a given file to an instance of
        /// <see cref="CsvReaderVisitorBase"/>, synchronously using memory-mapping.
        /// </summary>
        /// <param name="csvFilePath">
        /// <para>
        /// The path to the file that contains the CSV data.
        /// </para>
        /// <para>
        /// The only validation that Cursively does is <see cref="string.IsNullOrWhiteSpace"/>.
        /// </para>
        /// </param>
        /// <returns>
        /// An instance of <see cref="CsvMemoryMappedFileInput"/> wrapping
        /// <paramref name="csvFilePath"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="csvFilePath"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="csvFilePath"/> is non-<see langword="null"/>, but is either
        /// empty or whitespace-only.
        /// </exception>
        public static CsvMemoryMappedFileInput ForMemoryMappedFile(string csvFilePath)
        {
            if (csvFilePath is null)
            {
                throw new ArgumentNullException(nameof(csvFilePath));
            }

            if (string.IsNullOrWhiteSpace(csvFilePath))
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new ArgumentException("Cannot be blank", nameof(csvFilePath));
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            return new CsvMemoryMappedFileInput((byte)',', csvFilePath, true);
        }

        /// <summary>
        /// Creates an input that can describe the contents of a given
        /// <see cref="ReadOnlyMemory{T}"/> of bytes to an instance of
        /// <see cref="CsvReaderVisitorBase"/>, synchronously.
        /// </summary>
        /// <param name="memory">
        /// The <see cref="ReadOnlyMemory{T}"/> of bytes that contains the CSV data.
        /// </param>
        /// <returns>
        /// An instance of <see cref="CsvReadOnlyMemoryInput"/> wrapping <paramref name="memory"/>.
        /// </returns>
        public static CsvReadOnlyMemoryInput ForMemory(ReadOnlyMemory<byte> memory)
        {
            return new CsvReadOnlyMemoryInput((byte)',', memory, true);
        }

        /// <summary>
        /// Creates an input that can describe the contents of a given
        /// <see cref="ReadOnlySequence{T}"/> of bytes to an instance of
        /// <see cref="CsvReaderVisitorBase"/>, synchronously.
        /// </summary>
        /// <param name="sequence">
        /// The <see cref="ReadOnlySequence{T}"/> of bytes that contains the CSV data.
        /// </param>
        /// <returns>
        /// An instance of <see cref="CsvReadOnlySequenceInput"/> wrapping <paramref name="sequence"/>.
        /// </returns>
        public static CsvReadOnlySequenceInput ForSequence(ReadOnlySequence<byte> sequence)
        {
            return new CsvReadOnlySequenceInput((byte)',', sequence, true);
        }
    }
}
