using System.Collections.Generic;
using System.IO;

using Xunit;

using static Cursively.Tests.TestHelpers;

namespace Cursively.Tests
{
    public sealed class CsvBytesInputTests : CsvInputTestBase
    {
        public static IEnumerable<object[]> TestCsvFiles => GetTestCsvFiles();

        [Theory]
        [MemberData(nameof(TestCsvFiles))]
        public void WithoutIgnoringUTF8BOM(string filePath)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            byte[] fileData = File.ReadAllBytes(filePath);

            var sut = CsvInput.ForBytes(fileData)
                              .WithIgnoreUTF8ByteOrderMark(false);

            // act, assert
            RunTest(sut, filePath, (byte)',', false);
        }

        [Theory]
        [MemberData(nameof(TestCsvFiles))]
        public void IgnoreUTF8BOM(string filePath)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            byte[] fileData = File.ReadAllBytes(filePath);

            var sut = CsvInput.ForBytes(fileData)
                              .WithIgnoreUTF8ByteOrderMark(true);

            // act, assert
            RunTest(sut, filePath, (byte)',', true);
        }
    }
}
