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
                RunBinaryTest(sut, filePath, (byte)',', false);
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
                RunBinaryTest(sut, filePath, (byte)',', true);
            }
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithChunkLengths))]
        public async Task WithoutIgnoringUTF8BOMAsync(string filePath, int chunkLength)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            using (var stream = File.OpenRead(filePath))
            {
                var sut = CsvInput.ForStream(stream)
                                  .WithMinReadBufferByteCount(chunkLength)
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
            using (var stream = File.OpenRead(filePath))
            {
                var sut = CsvInput.ForStream(stream)
                                  .WithMinReadBufferByteCount(chunkLength)
                                  .WithIgnoreUTF8ByteOrderMark(true);

                // act, assert
                await RunBinaryTestAsync(sut, filePath, (byte)',', true).ConfigureAwait(true);
            }
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithChunkLengths))]
        public void NoPool(string filePath, int chunkLength)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            using (var stream = File.OpenRead(filePath))
            {
                var sut = CsvInput.ForStream(stream)
                                  .WithMinReadBufferByteCount(chunkLength)
                                  .WithReadBufferPool(null)
                                  .WithIgnoreUTF8ByteOrderMark(true);

                // act, assert
                RunBinaryTest(sut, filePath, (byte)',', true);
            }
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithChunkLengths))]
        public async Task NoPoolAsync(string filePath, int chunkLength)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            using (var stream = File.OpenRead(filePath))
            {
                var sut = CsvInput.ForStream(stream)
                                  .WithMinReadBufferByteCount(chunkLength)
                                  .WithReadBufferPool(null)
                                  .WithIgnoreUTF8ByteOrderMark(true);

                // act, assert
                await RunBinaryTestAsync(sut, filePath, (byte)',', true).ConfigureAwait(true);
            }
        }
    }
}
