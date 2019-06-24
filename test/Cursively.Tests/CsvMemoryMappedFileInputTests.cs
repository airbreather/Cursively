using System.Collections.Generic;
using System.IO;

using Xunit;

using static Cursively.Tests.TestHelpers;

namespace Cursively.Tests
{
    public sealed class CsvMemoryMappedFileInputTests : CsvInputTestBase
    {
        public static IEnumerable<object[]> TestCsvFiles => GetTestCsvFiles();

        [Theory]
        [MemberData(nameof(TestCsvFiles))]
        public void WithoutIgnoringUTF8BOM(string filePath)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);

            var sut = CsvInput.ForFile(filePath)
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

            var sut = CsvInput.ForFile(filePath)
                              .WithIgnoreUTF8ByteOrderMark(true);

            // act, assert
            RunTest(sut, filePath, (byte)',', true);
        }
    }
}
