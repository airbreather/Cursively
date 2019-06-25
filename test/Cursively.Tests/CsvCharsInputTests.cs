using System.Collections.Generic;
using System.IO;
using System.Text;

using Xunit;

using static Cursively.Tests.TestHelpers;

namespace Cursively.Tests
{
    public sealed class CsvCharsInputTests : CsvInputTestBase
    {
        public static IEnumerable<object[]> TestCsvFiles => GetTestCsvFiles();

        public static IEnumerable<object[]> TestCsvFilesWithChunkLengths => GetTestCsvFilesWithChunkLengths();

        [Theory]
        [MemberData(nameof(TestCsvFiles))]
        public void DefaultShouldAutomaticallyIgnoreBOM(string filePath)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);

            // the system ignores BOM for us automatically, so using it the way that this would be
            // used out-of-the-box should have the same effect.
            string fileData = File.ReadAllText(filePath);

            var sut = CsvInput.ForString(fileData);

            // act, assert
            RunTest(sut, filePath, (byte)',', true);
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithChunkLengths))]
        public void WithoutIgnoringBOM(string filePath, int chunkLength)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            string fileData = new UTF8Encoding(false, false).GetString(File.ReadAllBytes(filePath));

            var sut = CsvInput.ForString(fileData)
                              .WithEncodeBatchCharCount(chunkLength)
                              .WithIgnoreByteOrderMark(false);

            // act, assert
            RunTest(sut, filePath, (byte)',', false);
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithChunkLengths))]
        public void IgnoreBOM(string filePath, int chunkLength)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            string fileData = new UTF8Encoding(false, false).GetString(File.ReadAllBytes(filePath));

            var sut = CsvInput.ForString(fileData)
                              .WithEncodeBatchCharCount(chunkLength)
                              .WithIgnoreByteOrderMark(true);

            // act, assert
            RunTest(sut, filePath, (byte)',', fileData.Length == 0 || fileData[0] == '\uFEFF');
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithChunkLengths))]
        public void NoPool(string filePath, int chunkLength)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            string fileData = File.ReadAllText(filePath);

            var sut = CsvInput.ForString(fileData)
                              .WithEncodeBatchCharCount(chunkLength)
                              .WithEncodeBufferPool(null);

            // act, assert
            RunTest(sut, filePath, (byte)',', true);
        }
    }
}
