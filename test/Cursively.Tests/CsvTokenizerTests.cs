using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Xunit;

using static Cursively.Tests.TestHelpers;

namespace Cursively.Tests
{
    public sealed class CsvTokenizerTests
    {
        public static IEnumerable<object[]> TestCsvFiles => GetTestCsvFiles();

        public static IEnumerable<object[]> TestCsvFilesWithChunkLengthsAndDelimiters => GetTestCsvFilesWithChunkLengthsAndDelimiters();

        public static IEnumerable<object[]> TestValidHeaderedCsvFilesWithChunkLengthsAndDelimiters => GetTestCsvFilesWithChunkLengthsAndDelimiters("with-headers", "valid");

        public static IEnumerable<object[]> AllPossibleChunkLengthsForNonstandardTestFile => GetAllPossibleChunkLengthsForFile("nonstandard.csv");

        [Theory]
        [InlineData((byte)0x0A)]
        [InlineData((byte)0x0D)]
        [InlineData((byte)0x22)]
        public void ConstructorShouldRejectInvalidDelimiters(byte delimiter)
        {
            Assert.Throws<ArgumentException>("delimiter", () => new CsvTokenizer(delimiter));
        }

        [Theory]
        [MemberData(nameof(TestCsvFiles))]
        public void NullVisitorShouldBeFine(string filePath)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            ReadOnlySpan<byte> fileData = File.ReadAllBytes(filePath);
            var tokenizer = new CsvTokenizer();

            // act
            tokenizer.ProcessNextChunk(fileData, null);
            tokenizer.ProcessEndOfStream(null);

            // assert (empty)
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithChunkLengthsAndDelimiters))]
        public void CsvTokenizationShouldMatchCsvHelper(string filePath, int chunkLength, byte delimiter)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            byte[] fileDataTemplate = File.ReadAllBytes(filePath);
            for (int i = 0; i < fileDataTemplate.Length; i++)
            {
                if (fileDataTemplate[i] == (byte)',')
                {
                    fileDataTemplate[i] = delimiter;
                }
            }

            int randomSeed = HashCode.Combine(filePath, chunkLength, delimiter);
            foreach (byte[] fileData in VaryLineEndings(fileDataTemplate, randomSeed))
            {
                // act
                var actual = TokenizeCsvFileUsingCursively(fileData, chunkLength, delimiter);

                // assert
                var expected = TokenizeCsvFileUsingCsvHelper(fileData, $"{(char)delimiter}");
                Assert.Equal(expected, actual);
            }
        }

        [Theory]
        [MemberData(nameof(TestCsvFiles))]
        public void MemoryMappedCsvShouldMatchCsvHelper(string filePath)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            var visitor = new StringBufferingVisitor();

            // act
            CsvSyncInput.ForMemoryMappedFile(filePath).WithIgnoreUTF8ByteOrderMark(false).Process(visitor);
            var actual = visitor.Records;

            // assert
            var expected = TokenizeCsvFileUsingCsvHelper(File.ReadAllBytes(filePath), ",");
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(AllPossibleChunkLengthsForNonstandardTestFile))]
        [Trait("Regression", "#24")]
        public void NonstandardQuotedFieldsShouldNotify(int chunkLength)
        {
            // arrange
            string csvFilePath = Path.Combine(TestCsvFilesFolderPath, "nonstandard.csv");
            var visitor = new NonstandardFieldVisitor(checked((int)new FileInfo(csvFilePath).Length));

            // act
            ReadOnlySpan<byte> data = File.ReadAllBytes(csvFilePath);
            var tokenizer = new CsvTokenizer();
            while (!data.IsEmpty)
            {
                var nextChunk = data.Slice(0, Math.Min(chunkLength, data.Length));
                data = data.Slice(nextChunk.Length);
                tokenizer.ProcessNextChunk(nextChunk, visitor);
            }

            tokenizer.ProcessEndOfStream(visitor);

            // assert
            string[] expectedContentsBeforeNonstandardFields =
            {
                "hello ",
                "hello ",
                "good\"",
                "100% coverage demands <13, != 10; this is 9:	",
            };
            Assert.Equal(expectedContentsBeforeNonstandardFields, visitor.ContentsBeforeNonstandardFields);
        }

        [Theory]
        [MemberData(nameof(TestValidHeaderedCsvFilesWithChunkLengthsAndDelimiters))]
        public void HeaderedCsvTokenizationShouldMatchCsvHelper(string filePath, int chunkLength, byte delimiter)
        {
            // arrange
            filePath = Path.Combine(TestCsvFilesFolderPath, filePath);
            byte[] fileDataTemplate = File.ReadAllBytes(filePath);
            for (int i = 0; i < fileDataTemplate.Length; i++)
            {
                if (fileDataTemplate[i] == (byte)',')
                {
                    fileDataTemplate[i] = delimiter;
                }
            }

            int randomSeed = HashCode.Combine(filePath, chunkLength, delimiter);
            foreach (byte[] fileData in VaryLineEndings(fileDataTemplate, randomSeed))
            {
                // act
                var actual = TokenizeHeaderedCsvFileUsingCursively(fileData, chunkLength, delimiter);

                // assert
                var expected = TokenizeCsvFileUsingCsvHelper(fileData, $"{(char)delimiter}");
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void HeaderedCsvTokenizationShouldRejectOutlandishHeaderLimits()
        {
            // arrange
            byte[] fileData = File.ReadAllBytes(Path.Combine(TestCsvFilesFolderPath, "with-headers", "valid", "simple.csv"));

            // act, assert
            const int MaxArrayLength = 0x7FEFFFFF;
            Assert.Throws<ArgumentOutOfRangeException>("maxHeaderCount", () => TokenizeHeaderedCsvFileUsingCursivelyWithTheseHeaderLimits(fileData, fileData.Length, (byte)',', maxHeaderCount: -1, maxHeaderLength: MaxArrayLength));
            Assert.Throws<ArgumentOutOfRangeException>("maxHeaderCount", () => TokenizeHeaderedCsvFileUsingCursivelyWithTheseHeaderLimits(fileData, fileData.Length, (byte)',', maxHeaderCount: 0, maxHeaderLength: MaxArrayLength));
            Assert.Throws<ArgumentOutOfRangeException>("maxHeaderCount", () => TokenizeHeaderedCsvFileUsingCursivelyWithTheseHeaderLimits(fileData, fileData.Length, (byte)',', maxHeaderCount: MaxArrayLength + 1, maxHeaderLength: 1));
            Assert.Throws<ArgumentOutOfRangeException>("maxHeaderLength", () => TokenizeHeaderedCsvFileUsingCursivelyWithTheseHeaderLimits(fileData, fileData.Length, (byte)',', maxHeaderCount: MaxArrayLength, maxHeaderLength: -1));
            Assert.Throws<ArgumentOutOfRangeException>("maxHeaderLength", () => TokenizeHeaderedCsvFileUsingCursivelyWithTheseHeaderLimits(fileData, fileData.Length, (byte)',', maxHeaderCount: MaxArrayLength, maxHeaderLength: 0));
            Assert.Throws<ArgumentOutOfRangeException>("maxHeaderLength", () => TokenizeHeaderedCsvFileUsingCursivelyWithTheseHeaderLimits(fileData, fileData.Length, (byte)',', maxHeaderCount: MaxArrayLength, maxHeaderLength: MaxArrayLength + 1));
            TokenizeHeaderedCsvFileUsingCursivelyWithTheseHeaderLimits(fileData, fileData.Length, (byte)',', maxHeaderCount: MaxArrayLength, maxHeaderLength: MaxArrayLength);
        }

        [Fact]
        public void HeaderedCsvTokenizationShouldRejectTooManyHeaderFields()
        {
            // arrange
            byte[] fileData = File.ReadAllBytes(Path.Combine(TestCsvFilesFolderPath, "with-headers", "valid", "simple.csv"));

            // act, assert
            Assert.Throws<CursivelyTooManyHeadersException>(() => TokenizeHeaderedCsvFileUsingCursivelyWithTheseHeaderLimits(fileData, fileData.Length, (byte)',', maxHeaderCount: 1, maxHeaderLength: fileData.Length));
        }

        [Fact]
        public void HeaderedCsvTokenizationShouldHeaderFieldsWithCountBarelyWithinHigherLimit()
        {
            // arrange
            byte[] fileData = File.ReadAllBytes(Path.Combine(TestCsvFilesFolderPath, "with-headers", "valid", "simple.csv"));

            // act, assert (that it doesn't throw)
            TokenizeHeaderedCsvFileUsingCursivelyWithTheseHeaderLimits(fileData, fileData.Length, (byte)',', maxHeaderCount: 3, maxHeaderLength: fileData.Length);
        }

        [Fact]
        public void HeaderedCsvTokenizationShouldRejectOverlongHeaderField()
        {
            // arrange
            byte[] fileData = File.ReadAllBytes(Path.Combine(TestCsvFilesFolderPath, "with-headers", "valid", "for-header-limit-testing.csv"));

            // act, assert
            Assert.Throws<CursivelyHeaderIsTooLongException>(() => TokenizeHeaderedCsvFileUsingCursivelyWithTheseHeaderLimits(fileData, fileData.Length, (byte)',', maxHeaderCount: fileData.Length, maxHeaderLength: 1));
        }

        [Fact]
        public void HeaderedCsvTokenizationShouldAcceptHeaderFieldWithLengthBarelyWithinHigherLimit()
        {
            // arrange
            byte[] fileData = File.ReadAllBytes(Path.Combine(TestCsvFilesFolderPath, "with-headers", "valid", "for-header-limit-testing.csv"));

            // act, assert (that it doesn't throw)
            TokenizeHeaderedCsvFileUsingCursivelyWithTheseHeaderLimits(fileData, fileData.Length, (byte)',', maxHeaderCount: fileData.Length, maxHeaderLength: 101);
        }

        [Fact]
        public void HeaderedCsvTokenizationShouldRejectTooManyDataFieldsByDefault()
        {
            // arrange
            byte[] fileData = File.ReadAllBytes(Path.Combine(TestCsvFilesFolderPath, "with-headers", "invalid", "too-many-data-fields.csv"));

            // act, assert
            Assert.Throws<CursivelyExtraDataFieldsException>(() => TokenizeHeaderedCsvFileUsingCursively(fileData, fileData.Length, (byte)','));
        }

        [Fact]
        public void HeaderedCsvTokenizationShouldRejectMissingDataFieldsByDefault()
        {
            // arrange
            byte[] fileData = File.ReadAllBytes(Path.Combine(TestCsvFilesFolderPath, "with-headers", "invalid", "missing-data-fields.csv"));

            // act, assert
            Assert.Throws<CursivelyMissingDataFieldsException>(() => TokenizeHeaderedCsvFileUsingCursively(fileData, fileData.Length, (byte)','));
        }

        [Fact]
        public void HeaderedCsvTokenizationShouldRejectInvalidUTF8ByDefault()
        {
            // arrange
            byte[] fileData = File.ReadAllBytes(Path.Combine(TestCsvFilesFolderPath, "with-headers", "invalid", "invalid-utf8-in-header.csv"));

            // act, assert
            Assert.Throws<CursivelyHeadersAreNotUTF8Exception>(() => TokenizeHeaderedCsvFileUsingCursively(fileData, fileData.Length, (byte)','));
        }

        private sealed class NonstandardFieldVisitor : CsvReaderVisitorBase
        {
            private readonly Decoder _decoder = new UTF8Encoding(false, true).GetDecoder();

            private readonly char[] _fieldBuffer;

            private int _fieldBufferConsumed;

            public NonstandardFieldVisitor(int byteCount) =>
                _fieldBuffer = new char[Encoding.UTF8.GetMaxCharCount(byteCount)];

            public override void VisitEndOfField(ReadOnlySpan<byte> chunk)
            {
                VisitFieldContents(chunk, true);
                _fieldBufferConsumed = 0;
            }

            public List<string> ContentsBeforeNonstandardFields { get; } = new List<string>();

            public override void VisitEndOfRecord() { }

            public override void VisitPartialFieldContents(ReadOnlySpan<byte> chunk) =>
                VisitFieldContents(chunk, false);

            public override void VisitNonstandardQuotedField()
            {
                VisitFieldContents(default, true);
                ContentsBeforeNonstandardFields.Add(new string(_fieldBuffer, 0, _fieldBufferConsumed));
            }

            private void VisitFieldContents(ReadOnlySpan<byte> chunk, bool flush)
            {
                int cnt = _decoder.GetCharCount(chunk, flush);
                if (cnt > 0)
                {
                    _decoder.GetChars(chunk, new Span<char>(_fieldBuffer, _fieldBufferConsumed, cnt), flush);
                    _fieldBufferConsumed += cnt;
                }
            }
        }
    }
}
