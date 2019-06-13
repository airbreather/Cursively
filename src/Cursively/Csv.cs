using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Cursively
{
    /// <summary>
    /// Contains helper methods for CSV processing.
    /// </summary>
    public static class Csv
    {
        /// <summary>
        /// Describes the contents of a CSV stream to the given instance of the
        /// <see cref="CsvReaderVisitorBase"/> class.
        /// </summary>
        /// <param name="csvStream">
        /// The CSV stream to describe.
        /// </param>
        /// <param name="visitor">
        /// The <see cref="CsvReaderVisitorBase"/> instance to describe the stream to.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="csvStream"/> is <see langword="null"/>.
        /// </exception>
        public static void ProcessStream(Stream csvStream, CsvReaderVisitorBase visitor) =>
            ProcessStream(csvStream, visitor, 81920);

        /// <summary>
        /// Describes the contents of a CSV stream to the given instance of the
        /// <see cref="CsvReaderVisitorBase"/> class.
        /// </summary>
        /// <param name="csvStream">
        /// The CSV stream to describe.
        /// </param>
        /// <param name="visitor">
        /// The <see cref="CsvReaderVisitorBase"/> instance to describe the stream to.
        /// </param>
        /// <param name="bufferSize">
        /// The length of the buffer to use (default: 81920).
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="csvStream"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="bufferSize"/> is not greater than zero.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="csvStream"/> does not support reading (i.e.,
        /// <see cref="Stream.CanRead"/> is <see langword="false"/>).
        /// </exception>
        public static void ProcessStream(Stream csvStream, CsvReaderVisitorBase visitor, int bufferSize)
        {
            if (csvStream is null)
            {
                throw new ArgumentNullException(nameof(csvStream));
            }

            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, "Must be greater than zero.");
            }

            if (!csvStream.CanRead)
            {
                throw new ArgumentException("Stream does not support reading.", nameof(csvStream));
            }

            byte[] buffer = new byte[bufferSize];
            var tokenizer = new CsvTokenizer();
            int cnt;
            while ((cnt = csvStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(buffer, 0, cnt), visitor);
            }

            tokenizer.ProcessEndOfStream(visitor);
        }

        /// <summary>
        /// Describes the contents of a CSV stream to the given instance of the
        /// <see cref="CsvReaderVisitorBase"/> class.
        /// </summary>
        /// <param name="csvStream">
        /// The CSV stream to describe.
        /// </param>
        /// <param name="visitor">
        /// The <see cref="CsvReaderVisitorBase"/> instance to describe the stream to.
        /// </param>
        /// <param name="progress">
        /// <para>
        /// An <see cref="IProgress{T}"/> that will be notified every time the next chunk of the
        /// stream is processed, with the size of the chunk (in bytes) that was processed.
        /// </para>
        /// <para>
        /// All notifications will receive values less than or equal to the buffer size in bytes
        /// (which, for this overload, is the default value of 81,920).
        /// </para>
        /// <para>
        /// There will be one last notification with value 0 after the entire stream has been
        /// processed and the final few stream elements have been consumed.
        /// </para>
        /// <para>
        /// This may be left as <see langword="null"/> if no progress notifications are needed.
        /// </para>
        /// </param>
        /// <param name="cancellationToken">
        /// <para>
        /// An instance of <see cref="CancellationToken"/> that may be used to signal that results
        /// are no longer needed, and so the method should terminate at its earliest convenience.
        /// </para>
        /// <para>
        /// This may be left as its default value of <see cref="CancellationToken.None"/> if the
        /// operation does not need to support cancellation.
        /// </para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="csvStream"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="csvStream"/> does not support reading (i.e.,
        /// <see cref="Stream.CanRead"/> is <see langword="false"/>).
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown (perhaps asynchronously) to acknowledge cancellation.  A derived exception, such
        /// as <see cref="TaskCanceledException"/>, may also be thrown by the system.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown (perhaps asynchronously) if the underlying <see cref="CancellationTokenSource"/>
        /// object backing <paramref name="cancellationToken"/> is disposed before the asynchronous
        /// operation terminates.
        /// </exception>
        public static ValueTask ProcessStreamAsync(Stream csvStream, CsvReaderVisitorBase visitor, IProgress<int> progress = null, CancellationToken cancellationToken = default) =>
            ProcessStreamAsync(csvStream, visitor, 81920, progress, cancellationToken);

        /// <summary>
        /// Describes the contents of a CSV stream to the given instance of the
        /// <see cref="CsvReaderVisitorBase"/> class.
        /// </summary>
        /// <param name="csvStream">
        /// The CSV stream to describe.
        /// </param>
        /// <param name="visitor">
        /// The <see cref="CsvReaderVisitorBase"/> instance to describe the stream to.
        /// </param>
        /// <param name="bufferSize">
        /// The length of the buffer to use (default: 81920).
        /// </param>
        /// <param name="progress">
        /// <para>
        /// An <see cref="IProgress{T}"/> that will be notified every time the next chunk of the
        /// stream is processed, with the size of the chunk (in bytes) that was processed.
        /// </para>
        /// <para>
        /// All notifications will receive values less than or equal to the buffer size in bytes
        /// (which, for this overload, is the value of <paramref name="bufferSize"/>).
        /// </para>
        /// <para>
        /// There will be one last notification with value 0 after the entire stream has been
        /// processed and the final few stream elements have been consumed.
        /// </para>
        /// <para>
        /// This may be left as <see langword="null"/> if no progress notifications are needed.
        /// </para>
        /// </param>
        /// <param name="cancellationToken">
        /// <para>
        /// An instance of <see cref="CancellationToken"/> that may be used to signal that results
        /// are no longer needed, and so the method should terminate at its earliest convenience.
        /// </para>
        /// <para>
        /// This may be left as its default value of <see cref="CancellationToken.None"/> if the
        /// operation does not need to support cancellation.
        /// </para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="csvStream"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="bufferSize"/> is not greater than zero.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="csvStream"/> does not support reading (i.e.,
        /// <see cref="Stream.CanRead"/> is <see langword="false"/>).
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown (perhaps asynchronously) to acknowledge cancellation.  A derived exception, such
        /// as <see cref="TaskCanceledException"/>, may also be thrown by the system.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown (perhaps asynchronously) if the underlying <see cref="CancellationTokenSource"/>
        /// object backing <paramref name="cancellationToken"/> is disposed before the asynchronous
        /// operation terminates.
        /// </exception>
        public static async ValueTask ProcessStreamAsync(Stream csvStream, CsvReaderVisitorBase visitor, int bufferSize, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            if (csvStream is null)
            {
                throw new ArgumentNullException(nameof(csvStream));
            }

            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, "Must be greater than zero.");
            }

            if (!csvStream.CanRead)
            {
                throw new ArgumentException("Stream does not support reading.", nameof(csvStream));
            }

            byte[] buffer = new byte[bufferSize];
            var tokenizer = new CsvTokenizer();
            int cnt;
            while ((cnt = await csvStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(buffer, 0, cnt), visitor);
                progress?.Report(cnt);

                // not all streams support cancellation, so we might as well do this ourselves.  it
                // does involve a volatile read, so don't go overboard.
                cancellationToken.ThrowIfCancellationRequested();
            }

            tokenizer.ProcessEndOfStream(visitor);
            progress?.Report(0);
        }

        /// <summary>
        /// Describes the entire contents of a CSV file to the given instance of the
        /// <see cref="CsvReaderVisitorBase"/> class.
        /// </summary>
        /// <param name="csvFilePath">
        /// The path to the CSV file to describe.
        /// </param>
        /// <param name="visitor">
        /// The <see cref="CsvReaderVisitorBase"/> instance to describe the file to.
        /// </param>
        /// <remarks>
        /// The current version of this method uses memory-mapping behind the scenes in order to
        /// minimize the overhead of copying and cutting across discrete buffers, at the expense of
        /// slightly more overhead to set up the memory map than a typical read-from-stream pattern.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// See <see cref="FileStream(string, FileMode, FileAccess, FileShare, int, FileOptions)"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// See <see cref="FileStream(string, FileMode, FileAccess, FileShare, int, FileOptions)"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// See <see cref="FileStream(string, FileMode, FileAccess, FileShare, int, FileOptions)"/>.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// See <see cref="FileStream(string, FileMode, FileAccess, FileShare, int, FileOptions)"/>.
        /// </exception>
        /// <exception cref="IOException">
        /// <para>
        /// See <see cref="FileStream(string, FileMode, FileAccess, FileShare, int, FileOptions)"/>.
        /// </para>
        /// <para>
        /// See <see cref="MemoryMappedFile.CreateViewAccessor(long, long, MemoryMappedFileAccess)"/>.
        /// </para>
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        /// See <see cref="FileStream(string, FileMode, FileAccess, FileShare, int, FileOptions)"/>.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// See <see cref="FileStream(string, FileMode, FileAccess, FileShare, int, FileOptions)"/>.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// See <see cref="FileStream(string, FileMode, FileAccess, FileShare, int, FileOptions)"/>.
        /// </exception>
        /// <exception cref="PathTooLongException">
        /// See <see cref="FileStream(string, FileMode, FileAccess, FileShare, int, FileOptions)"/>.
        /// </exception>
        public static unsafe void ProcessEntireFile(string csvFilePath, CsvReaderVisitorBase visitor)
        {
            using (var fl = new FileStream(csvFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan))
            {
                long length = fl.Length;
                if (length == 0)
                {
                    return;
                }

                var tokenizer = new CsvTokenizer();
                using (var memoryMappedFile = MemoryMappedFile.CreateFromFile(fl, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, leaveOpen: true))
                using (var accessor = memoryMappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read))
                {
                    var handle = accessor.SafeMemoryMappedViewHandle;
                    byte* ptr = null;
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try
                    {
                        handle.AcquirePointer(ref ptr);
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
