using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Xunit;

using static Cursively.Tests.TestHelpers;

namespace Cursively.Tests
{
    public sealed class CsvStreamInputTests : CsvInputTestBase
    {
        public static IEnumerable<object[]> TestCsvFilesWithChunkLengths => GetTestCsvFilesWithChunkLengths();

        [Theory]
        [MemberData(nameof(TestCsvFilesWithChunkLengths))]
        public void WithoutIgnoringUTF8BOM(string filePath, int chunkLength)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            using (var stream = File.OpenRead(filePath))
            {
                var sut = CsvInput.ForStream(stream)
                                  .WithMinReadBufferByteCount(chunkLength)
                                  .WithIgnoreUTF8ByteOrderMark(false);

                // act, assert
                RunTest(sut, filePath, (byte)',', false);
            }
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithChunkLengths))]
        public void IgnoreUTF8BOM(string filePath, int chunkLength)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            using (var stream = File.OpenRead(filePath))
            {
                var sut = CsvInput.ForStream(stream)
                                  .WithMinReadBufferByteCount(chunkLength)
                                  .WithIgnoreUTF8ByteOrderMark(true);

                // act, assert
                RunTest(sut, filePath, (byte)',', true);
            }
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithChunkLengths))]
        public async ValueTask WithoutIgnoringUTF8BOMAsync(string filePath, int chunkLength)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            using (var stream = File.OpenRead(filePath))
            {
                var sut = CsvInput.ForStream(stream)
                                  .WithMinReadBufferByteCount(chunkLength)
                                  .WithIgnoreUTF8ByteOrderMark(false);

                // act, assert
                await RunTestAsync(sut, filePath, (byte)',', false).ConfigureAwait(false);
            }
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithChunkLengths))]
        public async ValueTask IgnoreUTF8BOMAsync(string filePath, int chunkLength)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            using (var stream = File.OpenRead(filePath))
            {
                var sut = CsvInput.ForStream(stream)
                                  .WithMinReadBufferByteCount(chunkLength)
                                  .WithIgnoreUTF8ByteOrderMark(true);

                // act, assert
                await RunTestAsync(sut, filePath, (byte)',', true).ConfigureAwait(false);
            }
        }
    }
}
