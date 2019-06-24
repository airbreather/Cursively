using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using CsvHelper;
using CsvHelper.Configuration;

namespace Cursively.Tests
{
    internal static class TestHelpers
    {
        public static readonly string TestCsvFilesFolderPath = Path.Combine(Path.GetDirectoryName(typeof(CsvTokenizer).Assembly.Location), "TestCsvFiles");

        public static readonly int[] TestChunkLengths = { 1, 2, 3, 5, 8, 13, 21, 34 };

        public static readonly byte[] TestDelimiters = { (byte)',', (byte)'\t' };

        public static List<string[]> TokenizeCsvFileUsingCursively(ReadOnlySpan<byte> fileData, int chunkLength, byte delimiter)
        {
            var tokenizer = new CsvTokenizer(delimiter);
            var visitor = new StringBufferingVisitor(fileData.Length);
            while (fileData.Length > chunkLength)
            {
                tokenizer.ProcessNextChunk(fileData.Slice(0, chunkLength), visitor);
                fileData = fileData.Slice(chunkLength);
            }

            tokenizer.ProcessNextChunk(fileData, visitor);
            tokenizer.ProcessEndOfStream(visitor);
            return visitor.Records;
        }

        public static List<string[]> TokenizeHeaderedCsvFileUsingCursively(ReadOnlySpan<byte> fileData, int chunkLength, byte delimiter)
        {
            var tokenizer = new CsvTokenizer(delimiter);
            var visitor = new HeaderedStringBufferingVisitor(fileData.Length);
            while (fileData.Length > chunkLength)
            {
                tokenizer.ProcessNextChunk(fileData.Slice(0, chunkLength), visitor);
                fileData = fileData.Slice(chunkLength);
            }

            tokenizer.ProcessNextChunk(fileData, visitor);
            tokenizer.ProcessEndOfStream(visitor);
            return visitor.Records;
        }

        public static IEnumerable<string[]> TokenizeCsvFileUsingCsvHelper(byte[] csvData, string delimiter)
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

        public static byte[][] VaryLineEndings(ReadOnlySpan<byte> fileData, int randomSeed)
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

        public static IEnumerable<object[]> GetTestCsvFiles(params string[] pathParts) =>
            from filePath in Directory.EnumerateFiles(Path.Combine(TestCsvFilesFolderPath, Path.Combine(pathParts)), "*.csv", SearchOption.AllDirectories)
            let relativePath = Path.GetRelativePath(TestCsvFilesFolderPath, filePath)
            select new object[] { relativePath };

        public static IEnumerable<object[]> GetTestCsvFilesWithChunkLengths(params string[] pathParts) =>
            from filePath in Directory.EnumerateFiles(Path.Combine(TestCsvFilesFolderPath, Path.Combine(pathParts)), "*.csv", SearchOption.AllDirectories)
            let relativePath = Path.GetRelativePath(TestCsvFilesFolderPath, filePath)
            from chunkLength in TestChunkLengths
            select new object[] { relativePath, chunkLength };

        public static IEnumerable<object[]> GetTestCsvFilesWithTwoChunkLengths(params string[] pathParts) =>
            from filePath in Directory.EnumerateFiles(Path.Combine(TestCsvFilesFolderPath, Path.Combine(pathParts)), "*.csv", SearchOption.AllDirectories)
            let relativePath = Path.GetRelativePath(TestCsvFilesFolderPath, filePath)
            from chunkLength1 in TestChunkLengths
            from chunkLength2 in TestChunkLengths
            select new object[] { relativePath, chunkLength1, chunkLength2 };

        public static IEnumerable<object[]> GetTestCsvFilesWithDelimiters(params string[] pathParts) =>
            from filePath in Directory.EnumerateFiles(Path.Combine(TestCsvFilesFolderPath, Path.Combine(pathParts)), "*.csv", SearchOption.AllDirectories)
            let relativePath = Path.GetRelativePath(TestCsvFilesFolderPath, filePath)
            from delimiter in TestDelimiters
            select new object[] { relativePath, delimiter };

        public static IEnumerable<object[]> GetTestCsvFilesWithChunkLengthsAndDelimiters(params string[] pathParts) =>
            from filePath in Directory.EnumerateFiles(Path.Combine(TestCsvFilesFolderPath, Path.Combine(pathParts)), "*.csv", SearchOption.AllDirectories)
            let relativePath = Path.GetRelativePath(TestCsvFilesFolderPath, filePath)
            from chunkLength in TestChunkLengths
            from delimiter in TestDelimiters
            select new object[] { relativePath, chunkLength, delimiter };

        private sealed class HeaderedStringBufferingVisitor : CsvReaderVisitorWithUTF8HeadersBase
        {
            private static readonly UTF8Encoding TheEncoding = new UTF8Encoding(false, false);

            private readonly List<string> _fields = new List<string>();

            private readonly byte[] _cutBuffer;

            private int _cutBufferConsumed;

            public HeaderedStringBufferingVisitor(int fileLength) => _cutBuffer = new byte[fileLength];

            public List<string[]> Records { get; } = new List<string[]>();

            protected override void VisitEndOfHeaderRecord()
            {
                Records.Insert(0, Headers.ToArray());
            }

            protected override void VisitEndOfDataRecord()
            {
                Records.Add(_fields.ToArray());
                _fields.Clear();
            }

            protected override void VisitPartialDataFieldContents(ReadOnlySpan<byte> chunk) => CopyToCutBuffer(chunk);

            protected override void VisitEndOfDataField(ReadOnlySpan<byte> chunk)
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
