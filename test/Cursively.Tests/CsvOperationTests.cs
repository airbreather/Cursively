using System.IO;
using System.Text;

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

        [Fact]
        public void WriteFlattenedTest()
        {
            const string InputText = @"
A,B,C
1,2,3
B,B,C
3,2,1
";
            var stringBuilder = new StringBuilder();
            using (var writer = new StringWriter(stringBuilder))
            {
                writer.NewLine = NewLineInSourceCode;
                var input = CsvInput.ForString(InputText);
                CsvOperation.WriteFlattened()
                            .WithOutputSink(writer)
                            .Run(input);
            }

            const string Expected =
@"[A] = 1
[B] = 2
[C] = 3
[A] = B
[B] = B
[C] = C
[A] = 3
[B] = 2
[C] = 1
";

            Assert.Equal(Expected, stringBuilder.ToString());
        }
    }
}
