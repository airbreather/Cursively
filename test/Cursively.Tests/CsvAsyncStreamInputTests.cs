using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Cursively.Inputs;

using Xunit;

using static Cursively.Tests.TestHelpers;

namespace Cursively.Tests
{
    public sealed class CsvAsyncStreamInputTests : CsvAsyncInputTestBase
    {
        public static IEnumerable<object[]> TestCsvFilesWithChunkLengths => GetTestCsvFilesWithChunkLengths();

        [Fact]
        public void ShouldFailForUnreadableStream()
        {
            using var stream = new TweakableStream(Stream.Null);
            stream.SetCanRead(false);
            Assert.Throws<ArgumentException>("csvStream", () => CsvAsyncInput.ForStream(stream));
        }

        [Fact]
        public void FluentConfigurationShouldValidateInputs()
        {
            var sut = CsvAsyncInput.ForStream(null);

            Assert.Throws<ArgumentException>("delimiter", () => sut.WithDelimiter((byte)'"'));
            Assert.Throws<ArgumentException>("delimiter", () => sut.WithDelimiter((byte)'\r'));
            Assert.Throws<ArgumentException>("delimiter", () => sut.WithDelimiter((byte)'\n'));
            Assert.Throws<ArgumentOutOfRangeException>("minReadBufferByteCount", () => sut.WithMinReadBufferByteCount(-1));
            Assert.Throws<ArgumentOutOfRangeException>("minReadBufferByteCount", () => sut.WithMinReadBufferByteCount(0));
        }

        [Fact]
        public async Task FluentConfigurationShouldFailAfterProcessing()
        {
            var sut = CsvAsyncInput.ForStream(null);

            await sut.ProcessAsync(null).ConfigureAwait(true);

            // shouldn't be able to further configure a Stream input after processing starts...
            Assert.Throws<InvalidOperationException>(() => sut.WithDelimiter((byte)'\t'));
            Assert.Throws<InvalidOperationException>(() => sut.WithIgnoreUTF8ByteOrderMark(false));
            Assert.Throws<InvalidOperationException>(() => sut.WithMinReadBufferByteCount(5));
            Assert.Throws<InvalidOperationException>(() => sut.WithReadBufferPool(null));
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithChunkLengths))]
        public async Task WithoutIgnoringUTF8BOM(string filePath, int chunkLength)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            using var stream = File.OpenRead(filePath);
            // act, assert
            await RunTestAsync(CreateSut, filePath, false).ConfigureAwait(true);

            CsvAsyncInputBase CreateSut()
            {
                stream.Position = 0;
                return CsvAsyncInput.ForStream(stream)
                                    .WithMinReadBufferByteCount(chunkLength)
                                    .WithIgnoreUTF8ByteOrderMark(false);
            }
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithChunkLengths))]
        public async Task IgnoreUTF8BOM(string filePath, int chunkLength)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            using var stream = File.OpenRead(filePath);
            // act, assert
            await RunTestAsync(CreateSut, filePath, true).ConfigureAwait(true);

            CsvAsyncInputBase CreateSut()
            {
                stream.Position = 0;
                return CsvAsyncInput.ForStream(stream)
                                    .WithMinReadBufferByteCount(chunkLength)
                                    .WithIgnoreUTF8ByteOrderMark(true);
            }
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithChunkLengths))]
        public async Task NoPool(string filePath, int chunkLength)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            using var stream = File.OpenRead(filePath);
            // act, assert
            await RunTestAsync(CreateSut, filePath, true).ConfigureAwait(true);

            CsvAsyncInputBase CreateSut()
            {
                stream.Position = 0;
                return CsvAsyncInput.ForStream(stream)
                                    .WithMinReadBufferByteCount(chunkLength)
                                    .WithReadBufferPool(null)
                                    .WithIgnoreUTF8ByteOrderMark(true);
            }
        }
    }
}
