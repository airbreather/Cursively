using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Cursively.Inputs;

using Xunit;

using static Cursively.Tests.TestHelpers;

namespace Cursively.Tests
{
    public abstract class CsvAsyncInputTestBase
    {
        protected async Task RunTestAsync(Func<CsvAsyncInputBase> sutCreator, string filePath, bool ignoreUTF8ByteOrderMark)
        {
            (byte[] fileData, int originalLength) = GetExpectedCsvData(filePath, ignoreUTF8ByteOrderMark);
            var expected = TokenizeCsvFileUsingCursively(fileData, fileData.Length, (byte)',');

            // run without progress
            {
                var sut = sutCreator();
                var inputVisitor = new StringBufferingVisitor();

                await sut.ProcessAsync(inputVisitor).ConfigureAwait(false);

                Assert.Equal(expected, inputVisitor.Records);

                await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ProcessAsync(null).AsTask());
            }

            // run with progress
            {
                var sut = sutCreator();
                var inputVisitor = new StringBufferingVisitor();

                var readSoFar = new List<int>();
                var progress = new ImmediateProgress<int>(readSoFar.Add);

                await sut.ProcessAsync(inputVisitor, progress).ConfigureAwait(false);

                Assert.Equal(expected, inputVisitor.Records);

                Assert.Equal(originalLength, readSoFar.Sum());
                Assert.Equal(0, readSoFar.Last());

                await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ProcessAsync(null).AsTask());
            }
        }

        // Progress<T> posts to a sync context.  We can't have that.
        private sealed class ImmediateProgress<T> : IProgress<T>
        {
            private readonly Action<T> _handler;

            public ImmediateProgress(Action<T> handler) =>
                _handler = handler;

            public void Report(T value) => _handler(value);
        }
    }
}
