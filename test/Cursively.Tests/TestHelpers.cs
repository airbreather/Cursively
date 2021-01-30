using System;
using System.Collections.Generic;
using System.Globalization;
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

        public static readonly int[] TestChunkLengths = { 1, 2, 3, 5, 8, 13, 21, 34, 65536 };

        public static readonly byte[] TestDelimiters = { (byte)',', (byte)'\t' };

        public static List<string[]> TokenizeCsvFileUsingCursively(ReadOnlySpan<byte> fileData, int chunkLength, byte delimiter)
        {
            var tokenizer = new CsvTokenizer(delimiter);
            var visitor = new StringBufferingVisitor();
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
            var visitor = new HeaderedStringBufferingVisitor(0x7FEFFFFF, 0x7FEFFFFF);
            while (fileData.Length > chunkLength)
            {
                tokenizer.ProcessNextChunk(fileData.Slice(0, chunkLength), visitor);
                fileData = fileData.Slice(chunkLength);
            }

            tokenizer.ProcessNextChunk(fileData, visitor);
            tokenizer.ProcessEndOfStream(visitor);
            return visitor.Records;
        }

        public static List<string[]> TokenizeHeaderedCsvFileUsingCursivelyWithTheseHeaderLimits(ReadOnlySpan<byte> fileData, int chunkLength, byte delimiter, int maxHeaderCount, int maxHeaderLength)
        {
            var tokenizer = new CsvTokenizer(delimiter);
            var visitor = new HeaderedStringBufferingVisitor(maxHeaderCount, maxHeaderLength);
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
            string txt = new UTF8Encoding(false, false).GetString(csvData);
            using var stringReader = new StringReader(txt);
            using var csvParser = new CsvParser(stringReader, new CsvConfiguration(CultureInfo.InvariantCulture) { BadDataFound = null, Delimiter = delimiter });
            while (csvParser.Read())
            {
                yield return csvParser.Record;
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

        public static (byte[] fileData, int originalLength) GetExpectedCsvData(string filePath, bool ignoreUTF8ByteOrderMark)
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            int originalLength = fileData.Length;
            int offset = 0;
            if (ignoreUTF8ByteOrderMark)
            {
                if (fileData.Length > 0)
                {
                    if (fileData[0] == 0xEF)
                    {
                        offset = 1;
                        if (fileData.Length > 1)
                        {
                            if (fileData[1] == 0xBB)
                            {
                                offset = 2;
                                if (fileData.Length > 2)
                                {
                                    if (fileData[2] == 0xBF)
                                    {
                                        offset = 3;
                                    }
                                    else
                                    {
                                        offset = 0;
                                    }
                                }
                            }
                            else
                            {
                                offset = 0;
                            }
                        }
                    }
                    else
                    {
                        offset = 0;
                    }
                }
            }

            if (offset != 0)
            {
                byte[] result = new byte[fileData.Length - offset];
                Buffer.BlockCopy(fileData, offset, result, 0, result.Length);
                fileData = result;
            }

            return (fileData, originalLength);
        }

        public static void EnsureCapacity<T>(ref T[] array, int neededLength)
        {
            int newLength = array.Length;
            while (newLength < neededLength)
            {
                newLength += newLength;
            }

            Array.Resize(ref array, newLength);
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

        public static IEnumerable<object[]> GetTestCsvFilesWithChunkLengthsAndDelimiters(params string[] pathParts) =>
            from filePath in Directory.EnumerateFiles(Path.Combine(TestCsvFilesFolderPath, Path.Combine(pathParts)), "*.csv", SearchOption.AllDirectories)
            let relativePath = Path.GetRelativePath(TestCsvFilesFolderPath, filePath)
            from chunkLength in TestChunkLengths
            from delimiter in TestDelimiters
            select new object[] { relativePath, chunkLength, delimiter };

        public static IEnumerable<object[]> GetAllPossibleChunkLengthsForFile(params string[] pathParts) =>
            from i in Enumerable.Range(1, checked((int)new FileInfo(Path.Combine(TestCsvFilesFolderPath, Path.Combine(pathParts))).Length))
            select new object[] { i };

        private sealed class HeaderedStringBufferingVisitor : CsvReaderVisitorWithUTF8HeadersBase
        {
            private static readonly UTF8Encoding TheEncoding = new UTF8Encoding(false, false);

            private readonly List<string> _fields = new List<string>();

            private byte[] _cutBuffer;

            private int _cutBufferConsumed;

            public HeaderedStringBufferingVisitor()
                : this(DefaultMaxHeaderCount, DefaultMaxHeaderLength)
            {
            }

            public HeaderedStringBufferingVisitor(int maxHeaderCount, int maxHeaderLength)
                : base(maxHeaderCount, maxHeaderLength, false, DefaultDecoderFallback)
            {
                _cutBuffer = new byte[100];
            }

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
                EnsureCapacity(ref _cutBuffer, _cutBufferConsumed + chunk.Length);
                chunk.CopyTo(new Span<byte>(_cutBuffer, _cutBufferConsumed, chunk.Length));
                _cutBufferConsumed += chunk.Length;
            }
        }
    }
}
