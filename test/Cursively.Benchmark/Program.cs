using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

using CsvHelper;
using CsvHelper.Configuration;

namespace Cursively.Benchmark
{
    [ClrJob]
    [CoreJob]
    [GcServer(true)]
    [MemoryDiagnoser]
    public class Program
    {
        public static void ProcessCsvFile(string csvFilePath)
        {
            var myVisitor = new MyVisitor(maxFieldLength: 1000);
            var tokenizer = new CsvTokenizer();
            using (var file = File.OpenRead(csvFilePath))
            {
                Console.WriteLine($"Started reading '{csvFilePath}'.");
                Span<byte> fileReadBuffer = new byte[4096];
                while (true)
                {
                    int count = file.Read(fileReadBuffer);
                    if (count == 0)
                    {
                        break;
                    }

                    var chunk = fileReadBuffer.Slice(0, count);
                    tokenizer.ProcessNextChunk(chunk, myVisitor);
                }

                tokenizer.ProcessEndOfStream(myVisitor);
            }

            Console.WriteLine($"Finished reading '{csvFilePath}'.");
        }

        public sealed class MyVisitor : CsvReaderVisitorBase
        {
            private readonly Decoder _utf8Decoder = Encoding.UTF8.GetDecoder();

            private readonly char[] _buffer;

            private int _bufferConsumed;

            public MyVisitor(int maxFieldLength) =>
                _buffer = new char[maxFieldLength];

            public override void VisitPartialFieldContents(ReadOnlySpan<byte> chunk) =>
                VisitFieldContents(chunk, flush: false);

            public override void VisitEndOfField(ReadOnlySpan<byte> chunk) =>
                VisitFieldContents(chunk, flush: true);

            public override void VisitEndOfRecord() =>
                Console.WriteLine("End of fields for this record.");

            private void VisitFieldContents(ReadOnlySpan<byte> chunk, bool flush)
            {
                int charCount = _utf8Decoder.GetCharCount(chunk, flush);
                if (charCount + _bufferConsumed <= _buffer.Length)
                {
                    _utf8Decoder.GetChars(chunk, new Span<char>(_buffer, _bufferConsumed, charCount), flush);
                    _bufferConsumed += charCount;
                }
                else
                {
                    throw new InvalidDataException($"Field is longer than {_buffer.Length} characters.");
                }

                if (flush)
                {
                    Console.Write("Field: ");
                    Console.WriteLine(_buffer, 0, _bufferConsumed);
                    _bufferConsumed = 0;
                }
            }
        }

        public static CsvFile[] CsvFiles => GetCsvFiles();

        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(CsvFiles))]
        public long CountRowsUsingCursivelyByteArray(CsvFile csvFile)
        {
            var visitor = new RowCountingVisitor();
            var tokenizer = new CsvTokenizer();
            tokenizer.ProcessNextChunk(csvFile.FileData, visitor);
            tokenizer.ProcessEndOfStream(visitor);
            return visitor.RowCount;
        }

        [Benchmark]
        [ArgumentsSource(nameof(CsvFiles))]
        public long CountRowsUsingCursivelyWithMemoryMappedFile(CsvFile csvFile)
        {
            var visitor = new RowCountingVisitor();
            Csv.ProcessMemoryMappedFile(csvFile.FullPath, visitor);
            return visitor.RowCount;
        }

        [Benchmark]
        [ArgumentsSource(nameof(CsvFiles))]
        public long CountRowsUsingCsvHelper(CsvFile csvFile)
        {
            using (var ms = new MemoryStream(csvFile.FileData, false))
            using (var tr = new StreamReader(ms, new UTF8Encoding(false, true), false))
            using (var rd = new CsvReader(tr, new Configuration { BadDataFound = null }))
            {
                long cnt = 0;
                while (rd.Read())
                {
                    ++cnt;
                }

                return cnt;
            }
        }

        private static int Main()
        {
            var prog = new Program();
            foreach (var csvFile in CsvFiles)
            {
                long rowCount = prog.CountRowsUsingCursivelyByteArray(csvFile);
                if (prog.CountRowsUsingCsvHelper(csvFile) != rowCount ||
                    prog.CountRowsUsingCursivelyWithMemoryMappedFile(csvFile) != rowCount)
                {
                    Console.Error.WriteLine($"Failed on {csvFile}.");
                    return 1;
                }
            }

            BenchmarkRunner.Run<Program>();
            return 0;
        }

        public readonly struct CsvFile
        {
            public CsvFile(string fullPath) =>
                (FullPath, FileName, FileData) = (fullPath, Path.GetFileNameWithoutExtension(fullPath), File.ReadAllBytes(fullPath));

            public string FullPath { get; }

            public string FileName { get; }

            public byte[] FileData { get; }

            public override string ToString() => FileName;
        }

        private static CsvFile[] GetCsvFiles([CallerFilePath]string myLocation = null) =>
            Array.ConvertAll(Directory.GetFiles(Path.Combine(Path.GetDirectoryName(myLocation), "large-csv-files"), "*.csv"),
                             fullPath => new CsvFile(fullPath));

        private sealed class RowCountingVisitor : CsvReaderVisitorBase
        {
            public long RowCount { get; private set; }

            public override void VisitEndOfRecord() => ++RowCount;

            public override void VisitEndOfField(ReadOnlySpan<byte> chunk) { }

            public override void VisitPartialFieldContents(ReadOnlySpan<byte> chunk) { }
        }
    }
}
