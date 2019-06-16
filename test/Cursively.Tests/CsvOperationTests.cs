using System.IO;
using System.Linq;
using System.Text;

using Cursively.Processing;

using Xunit;

namespace Cursively.Tests
{
    public sealed class CsvOperationTests
    {
        // I think AppVeyor is cloning with Unix-style line endings despite running the tests on
        // Windows... it doesn't matter for the main source code, but it makes these tests really
        // annoying unless I do something like this to make it all consistent.
        private const string NewLineInSourceCode = @"
";

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(200)]
        [InlineData(2_000)]
        public void CountRecordsTest(int expected)
        {
            // arrange
            string record = $"A,\"B{NewLineInSourceCode}\",\"C\",";

            string input = string.Join(NewLineInSourceCode, Enumerable.Repeat(record, expected));

            // act
            long actual = CsvOperation.CountRecords()
                                      .Run(CsvInput.ForString(input));

            // assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void WriteFlattenedTest()
        {
            // arrange
            const string Input = @"
A,BB,CCC
1,2,3
B,B,C
3,2,1
";
            var stringBuilder = new StringBuilder();
            using (var writer = new StringWriter(stringBuilder))
            {
                writer.NewLine = NewLineInSourceCode;

                // act
                CsvOperation.WriteFlattened()
                            .WithOutputSink(writer)
                            .Run(CsvInput.ForString(Input));
            }

            // assert
            const string Expected =
@"  [A] = 1
 [BB] = 2
[CCC] = 3
  [A] = B
 [BB] = B
[CCC] = C
  [A] = 3
 [BB] = 2
[CCC] = 1
";

            Assert.Equal(Expected, stringBuilder.ToString());
        }
    }
}
