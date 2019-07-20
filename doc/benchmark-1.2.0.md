This benchmark tests the simple act of counting how many records are in a CSV file.  It's not a simple count of how many lines are in the text file: line breaks within quoted fields must be treated as data, and multiple line breaks in a row must be treated as one, since each record must have at least one field.  Therefore, assuming correct implementations, this benchmark should test the raw CSV processing speed.

Cursively eliminates a ton of overhead found in libraries such as CsvHelper by restricting the allowed input encodings and using the visitor pattern as its only means of output.  Cursively can scan through the original bytes of the input to do its work, and it can give slices of the input data directly to the consumer without having to copy or allocate.

Therefore, these benchmarks are somewhat biased in favor of Cursively, as CsvHelper relies on external code to transform the data to UTF-16.  This isn't as unfair as that makes it sound: the overwhelming majority of input files are probably UTF-8 anyway (or a compatible SBCS), so this transformation is something that practically every user will experience.

- Input files can be found here: https://github.com/airbreather/Cursively/tree/v1.2.0/test/Cursively.Benchmark/large-csv-files.zip
- Benchmark source code is this: https://github.com/airbreather/Cursively/tree/v1.2.0/test/Cursively.Benchmark

As of version 1.2.0, these benchmarks no longer run on .NET Framework targets, because earlier benchmarks have shown comparable ratios.

Raw BenchmarkDotNet output is at the bottom, but here are some numbers derived from it showing the throughput of CsvHelper compared to the throughput of each of five different ways of using Cursively on multiple different kinds of files.  This summary does not indicate anything about the GC pressure:

| File                    | Size (bytes) | CsvHelper (MB/s) | Cursively 1* (MB/s) | Cursively 2* (MB/s) | Cursively 3* (MB/s) | Cursively 4* (MB/s) | Cursively 5* (MB/s) |
|-------------------------|-------------:|-----------------:|--------------------:|--------------------:|--------------------:|--------------------:|--------------------:|
| 100-huge-records        | 2900444      | 27.68            | 482.15 (x17.42)     | 528.08 (x19.08)     | 443.99 (x16.04)     | 448.17 (x16.19)     | 408.70 (x14.77)     |
| 100-huge-records-quoted | 4900444      | 29.40            | 304.64 (x10.36)     | 325.31 (x11.07)     | 295.21 (x10.04)     | 293.08 (x09.97)     | 285.03 (x09.70)     |
| 10k-empty-records       | 10020000     | 14.59            | 311.97 (x21.38)     | 311.27 (x21.33)     | 283.40 (x19.42)     | 297.41 (x20.38)     | 268.23 (x18.38)     |
| mocked                  | 12731500     | 74.29            | 3871.72 (x52.11)    | 3771.89 (x50.77)    | 1748.01 (x23.53)    | 2103.55 (x28.31)    | 1240.09 (x16.69)    |
| worldcitiespop          | 151492068    | 39.39            | 622.85 (x15.81)     | 617.22 (x15.67)     | 518.00 (x13.15)     | 538.54 (x13.67)     | 450.63 (x11.44)     |

\*Different Cursively methods are:
1. Directly using `CsvTokenizer`
1. `CsvSyncInput.ForMemory`
1. `CsvSyncInput.ForMemoryMappedFile`
1. `CsvSyncInput.ForStream` (using a `FileStream`)
1. `CsvAsyncInput.ForStream` (using a `FileStream` opened in asynchronous mode)

Raw BenchmarkDotNet output:

``` ini

BenchmarkDotNet=v0.11.5, OS=Windows 10.0.18362
Intel Core i7-6850K CPU 3.60GHz (Skylake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=3.0.100-preview6-012264
  [Host]     : .NET Core 2.2.6 (CoreCLR 4.6.27817.03, CoreFX 4.6.27818.02), 64bit RyuJIT
  Job-UPPUKA : .NET Core 2.2.6 (CoreCLR 4.6.27817.03, CoreFX 4.6.27818.02), 64bit RyuJIT

Server=True  

```
|                                       Method |              csvFile |         Mean |      Error |     StdDev | Ratio | RatioSD |      Gen 0 |    Gen 1 | Gen 2 |    Allocated |
|--------------------------------------------- |--------------------- |-------------:|-----------:|-----------:|------:|--------:|-----------:|---------:|------:|-------------:|
|                   CountRowsUsingCursivelyRaw |     100-huge-records |     5.737 ms |  0.0037 ms |  0.0033 ms |  1.00 |    0.00 |          - |        - |     - |         48 B |
|            CountRowsUsingCursivelyArrayInput |     100-huge-records |     5.238 ms |  0.0066 ms |  0.0062 ms |  0.91 |    0.00 |          - |        - |     - |         96 B |
| CountRowsUsingCursivelyMemoryMappedFileInput |     100-huge-records |     6.230 ms |  0.0159 ms |  0.0141 ms |  1.09 |    0.00 |          - |        - |     - |        544 B |
|       CountRowsUsingCursivelyFileStreamInput |     100-huge-records |     6.172 ms |  0.0080 ms |  0.0067 ms |  1.08 |    0.00 |          - |        - |     - |        272 B |
|  CountRowsUsingCursivelyAsyncFileStreamInput |     100-huge-records |     6.768 ms |  0.1349 ms |  0.1262 ms |  1.18 |    0.02 |          - |        - |     - |       1360 B |
|                      CountRowsUsingCsvHelper |     100-huge-records |    99.938 ms |  0.3319 ms |  0.3105 ms | 17.42 |    0.06 |   400.0000 | 200.0000 |     - |  110256320 B |
|                                              |                      |              |            |            |       |         |            |          |       |              |
|                   CountRowsUsingCursivelyRaw | 100-h(...)uoted [23] |    15.341 ms |  0.0305 ms |  0.0255 ms |  1.00 |    0.00 |          - |        - |     - |         48 B |
|            CountRowsUsingCursivelyArrayInput | 100-h(...)uoted [23] |    14.366 ms |  0.0167 ms |  0.0156 ms |  0.94 |    0.00 |          - |        - |     - |         96 B |
| CountRowsUsingCursivelyMemoryMappedFileInput | 100-h(...)uoted [23] |    15.831 ms |  0.0487 ms |  0.0455 ms |  1.03 |    0.00 |          - |        - |     - |        544 B |
|       CountRowsUsingCursivelyFileStreamInput | 100-h(...)uoted [23] |    15.946 ms |  0.0383 ms |  0.0358 ms |  1.04 |    0.00 |          - |        - |     - |        272 B |
|  CountRowsUsingCursivelyAsyncFileStreamInput | 100-h(...)uoted [23] |    16.396 ms |  0.2821 ms |  0.2771 ms |  1.07 |    0.02 |          - |        - |     - |       1360 B |
|                      CountRowsUsingCsvHelper | 100-h(...)uoted [23] |   158.968 ms |  0.1382 ms |  0.1154 ms | 10.36 |    0.02 |   333.3333 |        - |     - |  153579848 B |
|                                              |                      |              |            |            |       |         |            |          |       |              |
|                   CountRowsUsingCursivelyRaw |    10k-empty-records |    30.631 ms |  0.1009 ms |  0.0894 ms |  1.00 |    0.00 |          - |        - |     - |         48 B |
|            CountRowsUsingCursivelyArrayInput |    10k-empty-records |    30.699 ms |  0.0624 ms |  0.0584 ms |  1.00 |    0.00 |          - |        - |     - |         96 B |
| CountRowsUsingCursivelyMemoryMappedFileInput |    10k-empty-records |    33.718 ms |  0.0873 ms |  0.0817 ms |  1.10 |    0.00 |          - |        - |     - |        544 B |
|       CountRowsUsingCursivelyFileStreamInput |    10k-empty-records |    32.130 ms |  0.0944 ms |  0.0737 ms |  1.05 |    0.00 |          - |        - |     - |        272 B |
|  CountRowsUsingCursivelyAsyncFileStreamInput |    10k-empty-records |    35.625 ms |  0.7018 ms |  0.7801 ms |  1.17 |    0.03 |          - |        - |     - |       1360 B |
|                      CountRowsUsingCsvHelper |    10k-empty-records |   654.743 ms | 13.0238 ms | 16.9346 ms | 21.42 |    0.51 |  2000.0000 |        - |     - |  420832856 B |
|                                              |                      |              |            |            |       |         |            |          |       |              |
|                   CountRowsUsingCursivelyRaw |               mocked |     3.136 ms |  0.0038 ms |  0.0034 ms |  1.00 |    0.00 |          - |        - |     - |         48 B |
|            CountRowsUsingCursivelyArrayInput |               mocked |     3.219 ms |  0.0623 ms |  0.0741 ms |  1.02 |    0.02 |          - |        - |     - |         96 B |
| CountRowsUsingCursivelyMemoryMappedFileInput |               mocked |     6.946 ms |  0.0553 ms |  0.0490 ms |  2.21 |    0.02 |          - |        - |     - |        544 B |
|       CountRowsUsingCursivelyFileStreamInput |               mocked |     5.772 ms |  0.0365 ms |  0.0324 ms |  1.84 |    0.01 |          - |        - |     - |        272 B |
|  CountRowsUsingCursivelyAsyncFileStreamInput |               mocked |     9.791 ms |  0.1129 ms |  0.1056 ms |  3.12 |    0.04 |          - |        - |     - |       1360 B |
|                      CountRowsUsingCsvHelper |               mocked |   163.426 ms |  3.2351 ms |  3.1773 ms | 52.08 |    0.97 |   333.3333 |        - |     - |  115757736 B |
|                                              |                      |              |            |            |       |         |            |          |       |              |
|                   CountRowsUsingCursivelyRaw |       worldcitiespop |   231.955 ms |  1.0755 ms |  0.9534 ms |  1.00 |    0.00 |          - |        - |     - |         48 B |
|            CountRowsUsingCursivelyArrayInput |       worldcitiespop |   234.071 ms |  1.0749 ms |  0.9529 ms |  1.01 |    0.01 |          - |        - |     - |         96 B |
| CountRowsUsingCursivelyMemoryMappedFileInput |       worldcitiespop |   278.909 ms |  3.0866 ms |  2.8872 ms |  1.20 |    0.01 |          - |        - |     - |        544 B |
|       CountRowsUsingCursivelyFileStreamInput |       worldcitiespop |   268.271 ms |  3.4632 ms |  2.8920 ms |  1.16 |    0.02 |          - |        - |     - |        272 B |
|  CountRowsUsingCursivelyAsyncFileStreamInput |       worldcitiespop |   320.606 ms |  1.6204 ms |  1.5157 ms |  1.38 |    0.01 |          - |        - |     - |       1360 B |
|                      CountRowsUsingCsvHelper |       worldcitiespop | 3,667.940 ms | 60.8394 ms | 56.9092 ms | 15.82 |    0.24 | 15000.0000 |        - |     - | 3096694312 B |
