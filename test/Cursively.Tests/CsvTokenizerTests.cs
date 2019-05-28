using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using CsvHelper;
using CsvHelper.Configuration;

using Xunit;

namespace Cursively.Tests
{
    public sealed class CsvTokenizerTests
    {
        private static readonly string TestCsvFilesFolderPath = Path.Combine(Path.GetDirectoryName(typeof(CsvTokenizer).Assembly.Location), "TestCsvFiles");

        private static readonly int[] TestChunkLengths = { 1, 2, 3, 5, 8, 13, 21, 34 };

        private static readonly byte[] TestDelimiters = { (byte)',', (byte)'\t' };

        public static IEnumerable<object[]> TestCsvFiles =>
            from filePath in Directory.EnumerateFiles(TestCsvFilesFolderPath, "*.csv")
            select new object[] { filePath };

        public static IEnumerable<object[]> TestCsvFilesWithChunkLengthsAndDelimiters =>
            from filePath in Directory.EnumerateFiles(TestCsvFilesFolderPath, "*.csv")
            let fileName = Path.GetFileNameWithoutExtension(filePath)
            from chunkLength in TestChunkLengths
            from delimiter in TestDelimiters
            select new object[] { fileName, chunkLength, delimiter };

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
            ReadOnlySpan<byte> fileData = File.ReadAllBytes(filePath);
            var tokenizer = new CsvTokenizer();

            // act
            tokenizer.ProcessNextChunk(fileData, null);
            tokenizer.ProcessEndOfStream(null);

            // assert (empty)
        }

        [Theory]
        [MemberData(nameof(TestCsvFilesWithChunkLengthsAndDelimiters))]
        public void CsvTokenizationShouldMatchCsvHelper(string fileName, int chunkLength, byte delimiter)
        {
            // arrange
            byte[] fileDataTemplate = File.ReadAllBytes(Path.Combine(TestCsvFilesFolderPath, fileName + ".csv"));
            for (int i = 0; i < fileDataTemplate.Length; i++)
            {
                if (fileDataTemplate[i] == (byte)',')
                {
                    fileDataTemplate[i] = delimiter;
                }
            }

            int randomSeed = HashCode.Combine(fileName, chunkLength, delimiter);
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
            var visitor = new StringBufferingVisitor(checked((int)new FileInfo(filePath).Length));

            // act
            Csv.ProcessMemoryMappedFile(filePath, visitor);
            var actual = visitor.Lines;

            // assert
            var expected = TokenizeCsvFileUsingCsvHelper(File.ReadAllBytes(filePath), ",");
            Assert.Equal(expected, actual);
        }

        private static List<string[]> TokenizeCsvFileUsingCursively(ReadOnlySpan<byte> fileData, int chunkLength, byte delimiter)
        {
            var tokenizer = new CsvTokenizer(delimiter);
            var visitor = new StringBufferingVisitor(fileData.Length);
            while (fileData.Length >= chunkLength)
            {
                tokenizer.ProcessNextChunk(fileData.Slice(0, chunkLength), visitor);
                fileData = fileData.Slice(chunkLength);
            }

            tokenizer.ProcessNextChunk(fileData, visitor);
            tokenizer.ProcessEndOfStream(visitor);
            return visitor.Lines;
        }

        private static IEnumerable<string[]> TokenizeCsvFileUsingCsvHelper(byte[] csvData, string delimiter)
        {
            using (var stream = new MemoryStream(csvData, false))
            using (var streamReader = new StreamReader(stream, new UTF8Encoding(false, false), false))
            using (var csvReader = new CsvReader(streamReader, new Configuration { BadDataFound = null, Delimiter = delimiter }))
            {
                while (csvReader.Read())
                {
                    yield return csvReader.Context.Record;
                }
            }
        }

        private static byte[][] VaryLineEndings(ReadOnlySpan<byte> fileData, int randomSeed)
        {
            List<byte>[] resultLists =
            {
                new List<byte>(),
                new List<byte>(),
                new List<byte>(),
                new List<byte>(),
                new List<byte>(),
                new List<byte>(),
            };

            byte[][] lineEndings =
            {
                new byte[] { (byte)'\r' },
                new byte[] { (byte)'\n' },
                new byte[] { (byte)'\r', (byte)'\n' },
            };

            var random = new Random(randomSeed);

            ReadOnlySpan<byte> newLine = new UTF8Encoding(false, false).GetBytes(Environment.NewLine);
            while (!fileData.IsEmpty)
            {
                int newLineIndex = fileData.IndexOf(newLine);
                byte[] dataBeforeEndOfLine = (newLineIndex < 0 ? fileData : fileData.Slice(0, newLineIndex)).ToArray();

                for (int i = 0; i < resultLists.Length; i++)
                {
                    resultLists[i].AddRange(dataBeforeEndOfLine);

                    if (i < lineEndings.Length)
                    {
                        // make sure to have, for every line ending, at least one result that uses
                        // that line ending exclusively.
                        resultLists[i].AddRange(lineEndings[i]);
                    }
                    else
                    {
                        // vary the line endings within the rest of the results pseudo-randomly.
                        resultLists[i].AddRange(lineEndings[random.Next(lineEndings.Length)]);
                    }
                }

                fileData = newLineIndex < 0 ? default : fileData.Slice(newLineIndex + newLine.Length);
            }

            return Array.ConvertAll(resultLists, lst => lst.ToArray());
        }

        private sealed class StringBufferingVisitor : CsvReaderVisitorBase
        {
            private static readonly UTF8Encoding TheEncoding = new UTF8Encoding(false, false);

            private readonly List<string> _fields = new List<string>();

            private readonly byte[] _cutBuffer;

            private int _cutBufferConsumed;

            public StringBufferingVisitor(int fileLength) => _cutBuffer = new byte[fileLength];

            public List<string[]> Lines { get; } = new List<string[]>();

            public override void VisitEndOfRecord()
            {
                Lines.Add(_fields.ToArray());
                _fields.Clear();
            }

            public override void VisitPartialFieldContents(ReadOnlySpan<byte> chunk) => CopyToCutBuffer(chunk);

            public override void VisitEndOfField(ReadOnlySpan<byte> chunk)
            {
                if (_cutBufferConsumed != 0)
                {
                    CopyToCutBuffer(chunk);
                    chunk = new ReadOnlySpan<byte>(_cutBuffer, 0, _cutBufferConsumed);
                }

                _fields.Add(TheEncoding.GetString(chunk));
                _cutBufferConsumed = 0;
            }

            private void CopyToCutBuffer(ReadOnlySpan<byte> chunk)
            {
                chunk.CopyTo(new Span<byte>(_cutBuffer, _cutBufferConsumed, chunk.Length));
                _cutBufferConsumed += chunk.Length;
            }
        }
    }
}
