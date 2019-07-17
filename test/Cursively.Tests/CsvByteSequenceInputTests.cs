using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

using Xunit;

using static Cursively.Tests.TestHelpers;

namespace Cursively.Tests
{
    public sealed class CsvByteSequenceInputTests : CsvSyncInputTestBase
    {
        public static IEnumerable<object[]> TestCsvFilesWithChunkLengths => GetTestCsvFilesWithChunkLengths();

        [Fact]
        public void FluentConfigurationShouldValidateInputs()
        {
            var sut = CsvSyncInput.ForSequence(ReadOnlySequence<byte>.Empty);

            Assert.Throws<ArgumentException>("delimiter", () => sut.WithDelimiter((byte)'"'));
            Assert.Throws<ArgumentException>("delimiter", () => sut.WithDelimiter((byte)'\r'));
            Assert.Throws<ArgumentException>("delimiter", () => sut.WithDelimiter((byte)'\n'));
        }

        [Fact]
        public void FluentConfigurationShouldFailAfterProcessing()
        {
            var sut = CsvSyncInput.ForSequence(ReadOnlySequence<byte>.Empty);

            sut.Process(null);

            // shouldn't be able to further configure a Stream input after processing starts...
            Assert.Throws<InvalidOperationException>(() => sut.WithDelimiter((byte)'\t'));
            Assert.Throws<InvalidOperationException>(() => sut.WithIgnoreUTF8ByteOrderMark(false));
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithChunkLengths))]
        public void WithoutIgnoringUTF8BOM(string filePath, int chunkLength)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            ReadOnlyMemory<byte> fileData = File.ReadAllBytes(filePath);
            var bytes = CreateSequence(fileData, chunkLength);

            var sut = CsvSyncInput.ForSequence(bytes)
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
            ReadOnlyMemory<byte> fileData = File.ReadAllBytes(filePath);
            var bytes = CreateSequence(fileData, chunkLength);

            var sut = CsvSyncInput.ForSequence(bytes)
                                  .WithIgnoreUTF8ByteOrderMark(true);

            // act, assert
            RunTest(sut, filePath, true);
        }

        // inspired by:
        // https://github.com/dotnet/corefx/tree/2861ef06530df06b70f17a91616d979c8f18f75e/src/System.Memory/tests/ReadOnlyBuffer
        internal static ReadOnlySequence<T> CreateSequence<T>(ReadOnlyMemory<T> full, int chunkLength)
        {
            if (full.Length > 0 && full.Length <= chunkLength)
            {
                return new ReadOnlySequence<T>(full);
            }

            var first = new BufferSegment<T>(default);
            var last = first;
            for (int i = 0; i < 10; i++)
            {
                last = last.Append(Array.Empty<T>());
                last = last.Append(default);
            }

            int sizeOfFullChunks = full.Length - (full.Length % chunkLength);
            for (int pos = 0; pos < sizeOfFullChunks; pos += chunkLength)
            {
                var chunk = full.Slice(pos, chunkLength);
                last = last.Append(chunk);
                last = last.Append(default);
                last = last.Append(Array.Empty<T>());
            }

            if (full.Length != sizeOfFullChunks)
            {
                last = last.Append(full.Slice(sizeOfFullChunks));
                last = last.Append(Array.Empty<T>());
                last = last.Append(default);
            }

            return new ReadOnlySequence<T>(first, 0, last, last.Memory.Length);
        }

        private sealed class BufferSegment<T> : ReadOnlySequenceSegment<T>
        {
            public BufferSegment(ReadOnlyMemory<T> memory) =>
                Memory = memory;

            public BufferSegment<T> Append(ReadOnlyMemory<T> memory)
            {
                var segment = new BufferSegment<T>(memory);
                segment.RunningIndex += Memory.Length;
                Next = segment;
                return segment;
            }
        }
    }
}
