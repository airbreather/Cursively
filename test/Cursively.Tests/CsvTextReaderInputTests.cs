using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Xunit;

using static Cursively.Tests.TestHelpers;

namespace Cursively.Tests
{
    public sealed class CsvTextReaderInputTests : CsvInputTestBase
    {
        public static IEnumerable<object[]> TestCsvFilesWithTwoChunkLengths => GetTestCsvFilesWithTwoChunkLengths();

        [Theory]
        [MemberData(nameof(TestCsvFilesWithTwoChunkLengths))]
        public void WithoutIgnoringBOM(string filePath, int chunkLength1, int chunkLength2)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            var fileData = new UTF8Encoding(false, false).GetString(File.ReadAllBytes(filePath));
            using (var reader = new StringReader(fileData))
            {
                var sut = CsvInput.ForTextReader(reader)
                                  .WithMinReadBufferCharCount(chunkLength2)
                                  .WithEncodeBatchCharCount(chunkLength1)
                                  .WithIgnoreByteOrderMark(false);

                // act, assert
                RunUTF16Test(sut, filePath, (byte)',', false);
            }
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithTwoChunkLengths))]
        public void IgnoreBOM(string filePath, int chunkLength1, int chunkLength2)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            var fileData = new UTF8Encoding(false, false).GetString(File.ReadAllBytes(filePath));
            using (var reader = new StringReader(fileData))
            {
                var sut = CsvInput.ForTextReader(reader)
                                  .WithMinReadBufferCharCount(chunkLength2)
                                  .WithEncodeBatchCharCount(chunkLength1)
                                  .WithIgnoreByteOrderMark(true);

                // act, assert
                RunUTF16Test(sut, filePath, (byte)',', true);
            }
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithTwoChunkLengths))]
        public async Task WithoutIgnoringBOMAsync(string filePath, int chunkLength1, int chunkLength2)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            var fileData = new UTF8Encoding(false, false).GetString(File.ReadAllBytes(filePath));
            using (var reader = new StringReader(fileData))
            {
                var sut = CsvInput.ForTextReader(reader)
                                  .WithMinReadBufferCharCount(chunkLength2)
                                  .WithEncodeBatchCharCount(chunkLength1)
                                  .WithIgnoreByteOrderMark(false);

                // act, assert
                await RunUTF16TestAsync(sut, filePath, (byte)',', false).ConfigureAwait(true);
            }
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithTwoChunkLengths))]
        public async Task IgnoreBOMAsync(string filePath, int chunkLength1, int chunkLength2)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            var fileData = new UTF8Encoding(false, false).GetString(File.ReadAllBytes(filePath));
            using (var reader = new StringReader(fileData))
            {
                var sut = CsvInput.ForTextReader(reader)
                                  .WithMinReadBufferCharCount(chunkLength2)
                                  .WithEncodeBatchCharCount(chunkLength1)
                                  .WithIgnoreByteOrderMark(true);

                // act, assert
                await RunUTF16TestAsync(sut, filePath, (byte)',', true).ConfigureAwait(true);
            }
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithTwoChunkLengths))]
        public void NoEncodeBufferPool(string filePath, int chunkLength1, int chunkLength2)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            var fileData = new UTF8Encoding(false, false).GetString(File.ReadAllBytes(filePath));
            using (var reader = new StringReader(fileData))
            {
                var sut = CsvInput.ForTextReader(reader)
                                  .WithMinReadBufferCharCount(chunkLength2)
                                  .WithEncodeBatchCharCount(chunkLength1)
                                  .WithEncodeBufferPool(null)
                                  .WithIgnoreByteOrderMark(true);

                // act, assert
                RunUTF16Test(sut, filePath, (byte)',', true);
            }
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithTwoChunkLengths))]
        public void NoReadBufferPool(string filePath, int chunkLength1, int chunkLength2)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            var fileData = new UTF8Encoding(false, false).GetString(File.ReadAllBytes(filePath));
            using (var reader = new StringReader(fileData))
            {
                var sut = CsvInput.ForTextReader(reader)
                                  .WithMinReadBufferCharCount(chunkLength2)
                                  .WithReadBufferPool(null)
                                  .WithEncodeBatchCharCount(chunkLength1)
                                  .WithIgnoreByteOrderMark(true);

                // act, assert
                RunUTF16Test(sut, filePath, (byte)',', true);
            }
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithTwoChunkLengths))]
        public async Task NoEncodeBufferPoolAsync(string filePath, int chunkLength1, int chunkLength2)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            var fileData = new UTF8Encoding(false, false).GetString(File.ReadAllBytes(filePath));
            using (var reader = new StringReader(fileData))
            {
                var sut = CsvInput.ForTextReader(reader)
                                  .WithMinReadBufferCharCount(chunkLength2)
                                  .WithEncodeBatchCharCount(chunkLength1)
                                  .WithEncodeBufferPool(null)
                                  .WithIgnoreByteOrderMark(true);

                // act, assert
                await RunUTF16TestAsync(sut, filePath, (byte)',', true).ConfigureAwait(true);
            }
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithTwoChunkLengths))]
        public async Task NoReadBufferPoolAsync(string filePath, int chunkLength1, int chunkLength2)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            var fileData = new UTF8Encoding(false, false).GetString(File.ReadAllBytes(filePath));
            using (var reader = new StringReader(fileData))
            {
                var sut = CsvInput.ForTextReader(reader)
                                  .WithMinReadBufferCharCount(chunkLength2)
                                  .WithReadBufferPool(null)
                                  .WithEncodeBatchCharCount(chunkLength1)
                                  .WithIgnoreByteOrderMark(true);

                // act, assert
                await RunUTF16TestAsync(sut, filePath, (byte)',', true).ConfigureAwait(true);
            }
        }
    }
}
