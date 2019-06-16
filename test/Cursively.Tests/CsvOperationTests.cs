using System;
using System.IO;
using System.Text;

using Xunit;

namespace Cursively.Tests
{
    public sealed class CsvOperationTests
    {
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
                var input = CsvInput.ForString(InputText);
                CsvOperation.WriteFlattened()
                            .WithOutputSink(writer)
                            .Run(input);
            }

            string expectedFormat = "[A] = 1{0}[B] = 2{0}[C] = 3{0}[A] = B{0}[B] = B{0}[C] = C{0}[A] = 3{0}[B] = 2{0}[C] = 1{0}";
            string expected = string.Format(expectedFormat, Environment.NewLine);
            Assert.Equal(expected, stringBuilder.ToString());
        }
    }
}
