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

        public static IEnumerable<object[]> TestCsvFiles =>
            from filePath in Directory.EnumerateFiles(TestCsvFilesFolderPath, "*.csv")
            let fileName = Path.GetFileNameWithoutExtension(filePath)
            from chunkLength in TestChunkLengths
            select new object[] { fileName, chunkLength };

        [Theory]
        [MemberData(nameof(TestCsvFiles))]
        public void NullVisitorShouldBeFine(string fileName, int chunkLength)
        {
            // arrange
            string fullCsvFilePath = Path.Combine(TestCsvFilesFolderPath, fileName + ".csv");
            ReadOnlySpan<byte> fileData = File.ReadAllBytes(fullCsvFilePath);
            var tokenizer = new CsvTokenizer();

            // act
            while (fileData.Length >= chunkLength)
            {
                tokenizer.ProcessNextChunk(fileData.Slice(0, chunkLength), null);
                fileData = fileData.Slice(chunkLength);
            }

            tokenizer.ProcessNextChunk(fileData, null);
            tokenizer.ProcessEndOfStream(null);

            // assert (empty)
        }

        [Theory]
        [MemberData(nameof(TestCsvFiles))]
        public void CsvTokenizationShouldMatchCsvHelper(string fileName, int chunkLength)
        {
            // arrange
            byte[] fileDataTemplate = File.ReadAllBytes(Path.Combine(TestCsvFilesFolderPath, fileName + ".csv"));

            int randomSeed = HashCode.Combine(fileName, chunkLength);
            foreach (byte[] fileData in VaryLineEndings(fileDataTemplate, randomSeed))
            {
                // act
                var actual = TokenizeCsvFileUsingMine(fileData, chunkLength);

                // assert
                var expected = TokenizeCsvFileUsingCsvHelper(fileData);
                Assert.Equal(expected, actual);
            }
        }

        private static List<string[]> TokenizeCsvFileUsingMine(ReadOnlySpan<byte> fileData, int chunkLength)
        {
            var tokenizer = new CsvTokenizer();
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

        private static IEnumerable<string[]> TokenizeCsvFileUsingCsvHelper(byte[] csvData)
        {
            using (var stream = new MemoryStream(csvData, false))
            using (var streamReader = new StreamReader(stream, new UTF8Encoding(false, false), false))
            using (var csvReader = new CsvReader(streamReader, new Configuration { BadDataFound = null }))
            {
                while (csvReader.Read())
                {
                    yield return csvReader.Context.Record;
                }
            }
        }

        private static byte[][] VaryLineEndings(ReadOnlySpan<byte> fileData, int randomSeed)
        {
            var resultLists = new List<byte>[]
            {
                new List<byte>(),
                new List<byte>(),
                new List<byte>(),
                new List<byte>(),
                new List<byte>(),
                new List<byte>(),
            };

            var lineEndings = new byte[][]
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
