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
        public void WithoutIgnoringUTF8BOM(string filePath, int chunkLength1, int chunkLength2)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            using (var stream = File.OpenRead(filePath))
            using (var reader = new StreamReader(stream, new UTF8Encoding(false, false), false, chunkLength1))
            {
                var sut = CsvInput.ForTextReader(reader)
                                  .WithMinReadBufferCharCount(chunkLength2)
                                  .WithEncodeBatchCharCount(chunkLength1)
                                  .WithIgnoreByteOrderMark(false);

                // act, assert
                RunTest(sut, filePath, (byte)',', false);
            }
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithTwoChunkLengths))]
        public void IgnoreUTF8BOM(string filePath, int chunkLength1, int chunkLength2)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            using (var stream = File.OpenRead(filePath))
            using (var reader = new StreamReader(stream, new UTF8Encoding(false, false), false, chunkLength1))
            {
                var sut = CsvInput.ForTextReader(reader)
                                  .WithMinReadBufferCharCount(chunkLength2)
                                  .WithEncodeBatchCharCount(chunkLength1)
                                  .WithIgnoreByteOrderMark(true);

                // act, assert
                RunTest(sut, filePath, (byte)',', true);
            }
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithTwoChunkLengths))]
        public async ValueTask WithoutIgnoringUTF8BOMAsync(string filePath, int chunkLength1, int chunkLength2)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            using (var stream = File.OpenRead(filePath))
            using (var reader = new StreamReader(stream, new UTF8Encoding(false, false), false, chunkLength1))
            {
                var sut = CsvInput.ForTextReader(reader)
                                  .WithMinReadBufferCharCount(chunkLength2)
                                  .WithEncodeBatchCharCount(chunkLength1)
                                  .WithIgnoreByteOrderMark(false);

                // act, assert
                await RunTestAsync(sut, filePath, (byte)',', false).ConfigureAwait(false);
            }
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithTwoChunkLengths))]
        public async ValueTask IgnoreUTF8BOMAsync(string filePath, int chunkLength1, int chunkLength2)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            using (var stream = File.OpenRead(filePath))
            using (var reader = new StreamReader(stream, new UTF8Encoding(false, false), false, chunkLength1))
            {
                var sut = CsvInput.ForTextReader(reader)
                                  .WithMinReadBufferCharCount(chunkLength2)
                                  .WithEncodeBatchCharCount(chunkLength1)
                                  .WithIgnoreByteOrderMark(true);

                // act, assert
                await RunTestAsync(sut, filePath, (byte)',', true).ConfigureAwait(false);
            }
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithTwoChunkLengths))]
        public void NoEncodeBufferPool(string filePath, int chunkLength1, int chunkLength2)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            using (var stream = File.OpenRead(filePath))
            using (var reader = new StreamReader(stream, new UTF8Encoding(false, false), false, chunkLength1))
            {
                var sut = CsvInput.ForTextReader(reader)
                                  .WithMinReadBufferCharCount(chunkLength2)
                                  .WithEncodeBatchCharCount(chunkLength1)
                                  .WithEncodeBufferPool(null)
                                  .WithIgnoreByteOrderMark(true);

                // act, assert
                RunTest(sut, filePath, (byte)',', true);
            }
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithTwoChunkLengths))]
        public void NoReadBufferPool(string filePath, int chunkLength1, int chunkLength2)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            using (var stream = File.OpenRead(filePath))
            using (var reader = new StreamReader(stream, new UTF8Encoding(false, false), false, chunkLength1))
            {
                var sut = CsvInput.ForTextReader(reader)
                                  .WithMinReadBufferCharCount(chunkLength2)
                                  .WithReadBufferPool(null)
                                  .WithEncodeBatchCharCount(chunkLength1)
                                  .WithIgnoreByteOrderMark(true);

                // act, assert
                RunTest(sut, filePath, (byte)',', true);
            }
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithTwoChunkLengths))]
        public async ValueTask NoEncodeBufferPoolAsync(string filePath, int chunkLength1, int chunkLength2)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            using (var stream = File.OpenRead(filePath))
            using (var reader = new StreamReader(stream, new UTF8Encoding(false, false), false, chunkLength1))
            {
                var sut = CsvInput.ForTextReader(reader)
                                  .WithMinReadBufferCharCount(chunkLength2)
                                  .WithEncodeBatchCharCount(chunkLength1)
                                  .WithEncodeBufferPool(null)
                                  .WithIgnoreByteOrderMark(true);

                // act, assert
                await RunTestAsync(sut, filePath, (byte)',', true).ConfigureAwait(false);
            }
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithTwoChunkLengths))]
        public async ValueTask NoReadBufferPoolAsync(string filePath, int chunkLength1, int chunkLength2)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            using (var stream = File.OpenRead(filePath))
            using (var reader = new StreamReader(stream, new UTF8Encoding(false, false), false, chunkLength1))
            {
                var sut = CsvInput.ForTextReader(reader)
                                  .WithMinReadBufferCharCount(chunkLength2)
                                  .WithReadBufferPool(null)
                                  .WithEncodeBatchCharCount(chunkLength1)
                                  .WithIgnoreByteOrderMark(true);

                // act, assert
                await RunTestAsync(sut, filePath, (byte)',', true).ConfigureAwait(false);
            }
        }
    }
}
