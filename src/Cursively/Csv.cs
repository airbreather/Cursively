using System;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace Cursively
{
    /// <summary>
    /// Contains helper methods for CSV processing.
    /// </summary>
    public static class Csv
    {
        /// <summary>
        /// Describes the contents of a CSV file to the given instance of the
        /// <see cref="CsvReaderVisitorBase"/> class, using memory-mapped files behind the scenes.
        /// </summary>
        /// <param name="csvFilePath">
        /// The path to the CSV file to describe.
        /// </param>
        /// <param name="visitor">
        /// The <see cref="CsvReaderVisitorBase"/> instance to describe the file to.
        /// </param>
        public static unsafe void ProcessMemoryMappedFile(string csvFilePath, CsvReaderVisitorBase visitor)
        {
            using (var fl = new FileStream(csvFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
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
                    try
                    {
                        handle.AcquirePointer(ref ptr);
                        for (long rem = length; rem > 0; rem -= int.MaxValue)
                        {
                            int currentChunkLength = rem < int.MaxValue
                                ? unchecked((int)rem)
                                : int.MaxValue;

                            var span = new ReadOnlySpan<byte>(ptr, currentChunkLength);
                            tokenizer.ProcessNextChunk(span, visitor);
                        }

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
