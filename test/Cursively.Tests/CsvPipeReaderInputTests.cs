using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Cursively.Inputs;

using Pipelines.Sockets.Unofficial;

using Xunit;

using static Cursively.Tests.TestHelpers;

namespace Cursively.Tests
{
    public sealed class CsvPipeReaderInputTests : CsvAsyncInputTestBase
    {
        public static IEnumerable<object[]> TestCsvFilesWithChunkLengths => GetTestCsvFilesWithChunkLengths();

        [Fact]
        public void FluentConfigurationShouldValidateInputs()
        {
            var sut = CsvAsyncInput.ForPipeReader(null);

            Assert.Throws<ArgumentException>("delimiter", () => sut.WithDelimiter((byte)'"'));
            Assert.Throws<ArgumentException>("delimiter", () => sut.WithDelimiter((byte)'\r'));
            Assert.Throws<ArgumentException>("delimiter", () => sut.WithDelimiter((byte)'\n'));
        }

        [Fact]
        public async Task FluentConfigurationShouldFailAfterProcessing()
        {
            var sut = CsvAsyncInput.ForPipeReader(null);

            await sut.ProcessAsync(null).ConfigureAwait(true);

            // shouldn't be able to further configure a Stream input after processing starts...
            Assert.Throws<InvalidOperationException>(() => sut.WithDelimiter((byte)'\t'));
            Assert.Throws<InvalidOperationException>(() => sut.WithIgnoreUTF8ByteOrderMark(false));
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithChunkLengths))]
        public async Task WithoutIgnoringUTF8BOM(string filePath, int chunkLength)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);

            if (new FileInfo(filePath).Length == 0)
            {
                // Pipelines.Sockets.Unofficial seems to fail here.
                return;
            }

            // act, assert
            await RunTestAsync(CreateSut, filePath, false).ConfigureAwait(true);
            CsvAsyncInputBase CreateSut()
            {
                var pipeReader = MemoryMappedPipeReader.Create(filePath, chunkLength);
                return CsvAsyncInput.ForPipeReader(pipeReader)
                                    .WithIgnoreUTF8ByteOrderMark(false);
            }
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithChunkLengths))]
        public async Task IgnoreUTF8BOM(string filePath, int chunkLength)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);

            if (new FileInfo(filePath).Length == 0)
            {
                // Pipelines.Sockets.Unofficial seems to fail here.
                return;
            }

            // act, assert
            await RunTestAsync(CreateSut, filePath, true).ConfigureAwait(true);
            CsvAsyncInputBase CreateSut()
            {
                var pipeReader = MemoryMappedPipeReader.Create(filePath, chunkLength);
                return CsvAsyncInput.ForPipeReader(pipeReader)
                                    .WithIgnoreUTF8ByteOrderMark(true);
            }
        }
    }
}
