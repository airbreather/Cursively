using System;

using Cursively.Inputs;

using Xunit;

using static Cursively.Tests.TestHelpers;

namespace Cursively.Tests
{
    public abstract class CsvSyncInputTestBase
    {
        protected void RunTest(CsvSyncInputBase sut, string filePath, bool ignoreUTF8ByteOrderMark)
        {
            // arrange
            (byte[] fileData, _) = GetExpectedCsvData(filePath, ignoreUTF8ByteOrderMark);
            var expected = TokenizeCsvFileUsingCursively(fileData, fileData.Length, (byte)',');

            var inputVisitor = new StringBufferingVisitor(fileData.Length);

            // act
            sut.Process(inputVisitor);

            // assert
            Assert.Equal(expected, inputVisitor.Records);

            Assert.Throws<InvalidOperationException>(() => sut.Process(null));
        }
    }
}
