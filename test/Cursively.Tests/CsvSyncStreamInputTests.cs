using System;
using System.Collections.Generic;
using System.IO;

using Xunit;

using static Cursively.Tests.TestHelpers;

namespace Cursively.Tests
{
    public sealed class CsvSyncStreamInputTests : CsvSyncInputTestBase
    {
        public static IEnumerable<object[]> TestCsvFilesWithChunkLengths => GetTestCsvFilesWithChunkLengths();

        [Fact]
        public void ShouldFailForUnreadableStream()
        {
            // arrange
            using var stream = new TweakableStream(Stream.Null);
            stream.SetCanRead(false);
            Assert.Throws<ArgumentException>("csvStream", () => CsvSyncInput.ForStream(stream));
        }

        [Fact]
        public void FluentConfigurationShouldValidateInputs()
        {
            var sut = CsvSyncInput.ForStream(null);

            Assert.Throws<ArgumentException>("delimiter", () => sut.WithDelimiter((byte)'"'));
            Assert.Throws<ArgumentException>("delimiter", () => sut.WithDelimiter((byte)'\r'));
            Assert.Throws<ArgumentException>("delimiter", () => sut.WithDelimiter((byte)'\n'));
            Assert.Throws<ArgumentOutOfRangeException>("minReadBufferByteCount", () => sut.WithMinReadBufferByteCount(-1));
            Assert.Throws<ArgumentOutOfRangeException>("minReadBufferByteCount", () => sut.WithMinReadBufferByteCount(0));
        }

        [Fact]
        public void FluentConfigurationShouldFailAfterProcessing()
        {
            var sut = CsvSyncInput.ForStream(null);

            sut.Process(null);

            // shouldn't be able to further configure a Stream input after processing starts...
            Assert.Throws<InvalidOperationException>(() => sut.WithDelimiter((byte)'\t'));
            Assert.Throws<InvalidOperationException>(() => sut.WithIgnoreUTF8ByteOrderMark(false));
            Assert.Throws<InvalidOperationException>(() => sut.WithMinReadBufferByteCount(5));
            Assert.Throws<InvalidOperationException>(() => sut.WithReadBufferPool(null));
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithChunkLengths))]
        public void WithoutIgnoringUTF8BOM(string filePath, int chunkLength)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            using var stream = File.OpenRead(filePath);
            var sut = CsvSyncInput.ForStream(stream)
                                  .WithMinReadBufferByteCount(chunkLength)
                                  .WithIgnoreUTF8ByteOrderMark(false);

            // act, assert
            RunTest(sut, filePath, false);
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithChunkLengths))]
        public void IgnoreUTF8BOM(string filePath, int chunkLength)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            using var stream = File.OpenRead(filePath);
            var sut = CsvSyncInput.ForStream(stream)
                                  .WithMinReadBufferByteCount(chunkLength)
                                  .WithIgnoreUTF8ByteOrderMark(true);

            // act, assert
            RunTest(sut, filePath, true);
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithChunkLengths))]
        public void NoPool(string filePath, int chunkLength)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            using var stream = File.OpenRead(filePath);
            var sut = CsvSyncInput.ForStream(stream)
                                  .WithMinReadBufferByteCount(chunkLength)
                                  .WithReadBufferPool(null)
                                  .WithIgnoreUTF8ByteOrderMark(true);

            // act, assert
            RunTest(sut, filePath, true);
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithChunkLengths))]
        public void ShouldWorkWithoutSeeking(string filePath, int chunkLength)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            using var stream = new TweakableStream(File.OpenRead(filePath));
            stream.SetCanSeek(false);
            var sut = CsvSyncInput.ForStream(stream)
                                  .WithMinReadBufferByteCount(chunkLength)
                                  .WithIgnoreUTF8ByteOrderMark(false);

            // act, assert
            RunTest(sut, filePath, false);
        }
    }
}
