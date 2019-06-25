using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Xunit;

using static Cursively.Tests.TestHelpers;

namespace Cursively.Tests
{
    public sealed class CsvCharSequenceInputTests : CsvInputTestBase
    {
        public static IEnumerable<object[]> TestCsvFilesWithTwoChunkLengths => GetTestCsvFilesWithTwoChunkLengths();

        [Theory]
        [MemberData(nameof(TestCsvFilesWithTwoChunkLengths))]
        public void WithoutIgnoringBOM(string filePath, int chunkLength1, int chunkLength2)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            var fileData = new UTF8Encoding(false, false).GetString(File.ReadAllBytes(filePath)).AsMemory();
            var chars = CsvByteSequenceInputTests.CreateSequence(fileData, chunkLength1);

            var sut = CsvInput.ForChars(chars)
                              .WithEncodeBatchCharCount(chunkLength2)
                              .WithIgnoreByteOrderMark(false);

            // act, assert
            RunUTF16Test(sut, filePath, (byte)',', false);
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithTwoChunkLengths))]
        public void IgnoreBOM(string filePath, int chunkLength1, int chunkLength2)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            var fileData = new UTF8Encoding(false, false).GetString(File.ReadAllBytes(filePath)).AsMemory();
            var chars = CsvByteSequenceInputTests.CreateSequence(fileData, chunkLength1);

            var sut = CsvInput.ForChars(chars)
                              .WithEncodeBatchCharCount(chunkLength2)
                              .WithIgnoreByteOrderMark(true);

            // act, assert
            RunUTF16Test(sut, filePath, (byte)',', true);
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithTwoChunkLengths))]
        public void NoEncodeBufferPool(string filePath, int chunkLength1, int chunkLength2)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            var fileData = new UTF8Encoding(false, false).GetString(File.ReadAllBytes(filePath)).AsMemory();
            var chars = CsvByteSequenceInputTests.CreateSequence(fileData, chunkLength1);

            var sut = CsvInput.ForChars(chars)
                              .WithEncodeBufferPool(null)
                              .WithEncodeBatchCharCount(chunkLength2)
                              .WithIgnoreByteOrderMark(false);

            // act, assert
            RunUTF16Test(sut, filePath, (byte)',', false);
        }
    }
}
