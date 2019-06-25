using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Pipelines.Sockets.Unofficial;

using Xunit;

using static Cursively.Tests.TestHelpers;

namespace Cursively.Tests
{
    public sealed class CsvPipeReaderInputTests : CsvInputTestBase
    {
        public static IEnumerable<object[]> TestCsvFilesWithChunkLengths => GetTestCsvFilesWithChunkLengths();

        [Theory]
        [MemberData(nameof(TestCsvFilesWithChunkLengths))]
        public void WithoutIgnoringUTF8BOM(string filePath, int chunkLength)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);

            if (new FileInfo(filePath).Length == 0)
            {
                // Pipelines.Sockets.Unofficial seems to fail here.
                return;
            }

            var pipeReader = MemoryMappedPipeReader.Create(filePath, chunkLength);
            using (pipeReader as IDisposable)
            {
                var sut = CsvInput.ForPipeReader(pipeReader)
                                  .WithIgnoreUTF8ByteOrderMark(false);

                // act, assert
                RunBinaryTest(sut, filePath, (byte)',', false);
            }
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithChunkLengths))]
        public void IgnoreUTF8BOM(string filePath, int chunkLength)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);

            if (new FileInfo(filePath).Length == 0)
            {
                // Pipelines.Sockets.Unofficial seems to fail here.
                return;
            }

            var pipeReader = MemoryMappedPipeReader.Create(filePath, chunkLength);
            using (pipeReader as IDisposable)
            {
                var sut = CsvInput.ForPipeReader(pipeReader)
                                  .WithIgnoreUTF8ByteOrderMark(true);

                // act, assert
                RunBinaryTest(sut, filePath, (byte)',', true);
            }
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithChunkLengths))]
        public async Task WithoutIgnoringUTF8BOMAsync(string filePath, int chunkLength)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);

            if (new FileInfo(filePath).Length == 0)
            {
                // Pipelines.Sockets.Unofficial seems to fail here.
                return;
            }

            var pipeReader = MemoryMappedPipeReader.Create(filePath, chunkLength);
            using (pipeReader as IDisposable)
            {
                var sut = CsvInput.ForPipeReader(pipeReader)
                                  .WithIgnoreUTF8ByteOrderMark(false);

                // act, assert
                await RunBinaryTestAsync(sut, filePath, (byte)',', false).ConfigureAwait(true);
            }
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithChunkLengths))]
        public async Task IgnoreUTF8BOMAsync(string filePath, int chunkLength)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);

            if (new FileInfo(filePath).Length == 0)
            {
                // Pipelines.Sockets.Unofficial seems to fail here.
                return;
            }

            var pipeReader = MemoryMappedPipeReader.Create(filePath, chunkLength);
            using (pipeReader as IDisposable)
            {
                var sut = CsvInput.ForPipeReader(pipeReader)
                                  .WithIgnoreUTF8ByteOrderMark(true);

                // act, assert
                await RunBinaryTestAsync(sut, filePath, (byte)',', true).ConfigureAwait(true);
            }
        }
    }
}
