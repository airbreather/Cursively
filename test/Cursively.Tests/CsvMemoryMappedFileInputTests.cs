using System;
using System.Collections.Generic;
using System.IO;

using Xunit;

using static Cursively.Tests.TestHelpers;

namespace Cursively.Tests
{
    public sealed class CsvMemoryMappedFileInputTests : CsvSyncInputTestBase
    {
        public static IEnumerable<object[]> TestCsvFiles => GetTestCsvFiles();

        [Fact]
        public void FluentConfigurationShouldValidateInputs()
        {
            var sut = CsvSyncInput.ForMemoryMappedFile(Path.Combine(TestCsvFilesFolderPath, "empty.csv"));

            Assert.Throws<ArgumentException>("delimiter", () => sut.WithDelimiter((byte)'"'));
            Assert.Throws<ArgumentException>("delimiter", () => sut.WithDelimiter((byte)'\r'));
            Assert.Throws<ArgumentException>("delimiter", () => sut.WithDelimiter((byte)'\n'));
        }

        [Fact]
        public void FluentConfigurationShouldFailAfterProcessing()
        {
            var sut = CsvSyncInput.ForMemoryMappedFile(Path.Combine(TestCsvFilesFolderPath, "empty.csv"));

            sut.Process(null);

            // shouldn't be able to further configure a Stream input after processing starts...
            Assert.Throws<InvalidOperationException>(() => sut.WithDelimiter((byte)'\t'));
            Assert.Throws<InvalidOperationException>(() => sut.WithIgnoreUTF8ByteOrderMark(false));
        }

        [Theory]
        [MemberData(nameof(TestCsvFiles))]
        public void WithoutIgnoringUTF8BOM(string filePath)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);

            var sut = CsvSyncInput.ForMemoryMappedFile(filePath)
                                  .WithIgnoreUTF8ByteOrderMark(false);

            // act, assert
            RunTest(sut, filePath, false);
        }

        [Theory]
        [MemberData(nameof(TestCsvFiles))]
        public void IgnoreUTF8BOM(string filePath)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);

            var sut = CsvSyncInput.ForMemoryMappedFile(filePath)
                                  .WithIgnoreUTF8ByteOrderMark(true);

            // act, assert
            RunTest(sut, filePath, true);
        }
    }
}
