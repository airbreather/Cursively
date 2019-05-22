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
    [CoreRtJob]
    [GcServer(true)]
    [MemoryDiagnoser]
    public class Program
    {
        public static CsvFile[] CsvFiles => GetCsvFiles();

        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(CsvFiles))]
        public void NopUsingCursively(CsvFile csvFile)
        {
            var tokenizer = new CsvTokenizer();
            tokenizer.ProcessNextChunk(csvFile.FileData, null);
            tokenizer.ProcessEndOfStream(null);
        }

        [Benchmark]
        [ArgumentsSource(nameof(CsvFiles))]
        public long CountRowsUsingCursively(CsvFile csvFile)
        {
            var visitor = new RowCountingVisitor();
            var tokenizer = new CsvTokenizer();
            tokenizer.ProcessNextChunk(csvFile.FileData, visitor);
            tokenizer.ProcessEndOfStream(visitor);
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
                if (prog.CountRowsUsingCursively(csvFile) != prog.CountRowsUsingCsvHelper(csvFile))
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
            public long CharCount { get; private set; }

            public long RowCount { get; private set; }

            public override void VisitEndOfRecord() => ++RowCount;

            public override void VisitEndOfField(ReadOnlySpan<byte> chunk) { }

            public override void VisitPartialFieldContents(ReadOnlySpan<byte> chunk) { }
        }
    }
}
