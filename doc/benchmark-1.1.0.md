This benchmark tests the simple act of counting how many records are in a CSV file.  It's not a simple count of how many lines are in the text file: line breaks within quoted fields must be treated as data, and multiple line breaks in a row must be treated as one, since each record must have at least one field.  Therefore, assuming correct implementations, this benchmark should test the raw CSV processing speed.

Cursively eliminates a ton of overhead found in libraries such as CsvHelper by restricting the allowed input encodings and using the visitor pattern as its only means of output.  Cursively can scan through the original bytes of the input to do its work, and it can give slices of the input data directly to the consumer without having to copy or allocate.

Therefore, these benchmarks are somewhat biased in favor of Cursively, as CsvHelper relies on external code to transform the data to UTF-16.  This isn't as unfair as that makes it sound: the overwhelming majority of input files are probably UTF-8 anyway (or a compatible SBCS), so this transformation is something that practically every user will experience.

- Input files can be found here: https://github.com/airbreather/Cursively/tree/v1.1.0/test/Cursively.Benchmark/large-csv-files.zip
- Benchmark source code is this: https://github.com/airbreather/Cursively/tree/v1.1.0/test/Cursively.Benchmark

Raw BenchmarkDotNet output is at the bottom, but here are some numbers derived from it.  The data was fully loaded in main memory when running these tests.  This summary also does not indicate anything about the GC pressure:

|CSV File|Runtime|Library|Throughput|
|-|-|-|-|
|100 records / 10,000 tiny fields each|.NET 4.7.2|Cursively|336.06 MiB/s|
|100 records / 10,000 tiny fields each|.NET 4.7.2|CsvHelper|22.04 MiB/s|
|100 records / 10,000 tiny fields each|.NET Core 2.2.5|Cursively|487.59 MiB/s|
|100 records / 10,000 tiny fields each|.NET Core 2.2.5|CsvHelper|27.31 MiB/s|
|100 records / 10,000 tiny quoted fields each|.NET 4.7.2|Cursively|178.23 MiB/s|
|100 records / 10,000 tiny quoted fields each|.NET 4.7.2|CsvHelper|24.33 MiB/s|
|100 records / 10,000 tiny quoted fields each|.NET Core 2.2.5|Cursively|303.67 MiB/s|
|100 records / 10,000 tiny quoted fields each|.NET Core 2.2.5|CsvHelper|29.20 MiB/s|
|10,000 records / 1,000 empty fields each|.NET 4.7.2|Cursively|176.71 MiB/s|
|10,000 records / 1,000 empty fields each|.NET 4.7.2|CsvHelper|14.45 MiB/s|
|10,000 records / 1,000 empty fields each|.NET Core 2.2.5|Cursively|306.49 MiB/s|
|10,000 records / 1,000 empty fields each|.NET Core 2.2.5|CsvHelper|15.15 MiB/s|
|Mock data from Mockaroo|.NET 4.7.2|Cursively|2,711.41 MiB/s|
|Mock data from Mockaroo|.NET 4.7.2|CsvHelper|72.50 MiB/s|
|Mock data from Mockaroo|.NET Core 2.2.5|Cursively|3,755.55 MiB/s|
|Mock data from Mockaroo|.NET Core 2.2.5|CsvHelper|75.05 MiB/s|
|worldcitiespop.csv ([from here](https://burntsushi.net/stuff/))|.NET 4.7.2|Cursively|390.75 MiB/s|
|worldcitiespop.csv ([from here](https://burntsushi.net/stuff/))|.NET 4.7.2|CsvHelper|40.15 MiB/s|
|worldcitiespop.csv ([from here](https://burntsushi.net/stuff/))|.NET Core 2.2.5|Cursively|607.81 MiB/s|
|worldcitiespop.csv ([from here](https://burntsushi.net/stuff/))|.NET Core 2.2.5|CsvHelper|39.90 MiB/s|

Raw BenchmarkDotNet output:

``` ini

BenchmarkDotNet=v0.11.5, OS=Windows 10.0.18362
Intel Core i7-6850K CPU 3.60GHz (Skylake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=3.0.100-preview6-012264
  [Host]     : .NET Core 2.2.5 (CoreCLR 4.6.27617.05, CoreFX 4.6.27618.01), 64bit RyuJIT
  Job-DDQSKN : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.8.3801.0
  Job-RTHUVO : .NET Core 2.2.5 (CoreCLR 4.6.27617.05, CoreFX 4.6.27618.01), 64bit RyuJIT

Server=True  

```
|                  Method | Runtime |              csvFile |         Mean |     Error |    StdDev | Ratio | RatioSD |       Gen 0 |      Gen 1 |    Gen 2 |    Allocated |
|------------------------ |-------- |--------------------- |-------------:|----------:|----------:|------:|--------:|------------:|-----------:|---------:|-------------:|
| CountRowsUsingCursively |     Clr |     100-huge-records |     8.231 ms | 0.0839 ms | 0.0743 ms |  1.00 |    0.00 |           - |          - |        - |        128 B |
| CountRowsUsingCsvHelper |     Clr |     100-huge-records |   125.493 ms | 1.1717 ms | 1.0387 ms | 15.25 |    0.21 |  17250.0000 |  6750.0000 | 750.0000 |  110560856 B |
|                         |         |                      |              |           |           |       |         |             |            |          |              |
| CountRowsUsingCursively |    Core |     100-huge-records |     5.673 ms | 0.0073 ms | 0.0068 ms |  1.00 |    0.00 |           - |          - |        - |         48 B |
| CountRowsUsingCsvHelper |    Core |     100-huge-records |   101.277 ms | 0.2342 ms | 0.2190 ms | 17.85 |    0.05 |    400.0000 |   200.0000 |        - |  110256320 B |
|                         |         |                      |              |           |           |       |         |             |            |          |              |
| CountRowsUsingCursively |     Clr | 100-h(...)uoted [23] |    26.222 ms | 0.0260 ms | 0.0231 ms |  1.00 |    0.00 |           - |          - |        - |        256 B |
| CountRowsUsingCsvHelper |     Clr | 100-h(...)uoted [23] |   192.090 ms | 0.9954 ms | 0.9311 ms |  7.33 |    0.04 |  25000.0000 | 11000.0000 | 666.6667 |  154027456 B |
|                         |         |                      |              |           |           |       |         |             |            |          |              |
| CountRowsUsingCursively |    Core | 100-h(...)uoted [23] |    15.390 ms | 0.0450 ms | 0.0399 ms |  1.00 |    0.00 |           - |          - |        - |         48 B |
| CountRowsUsingCsvHelper |    Core | 100-h(...)uoted [23] |   160.043 ms | 0.4644 ms | 0.4344 ms | 10.40 |    0.04 |    333.3333 |          - |        - |  153579848 B |
|                         |         |                      |              |           |           |       |         |             |            |          |              |
| CountRowsUsingCursively |     Clr |    10k-empty-records |    54.007 ms | 0.3061 ms | 0.2556 ms |  1.00 |    0.00 |           - |          - |        - |        819 B |
| CountRowsUsingCsvHelper |     Clr |    10k-empty-records |   661.502 ms | 3.1801 ms | 2.9747 ms | 12.24 |    0.08 |  66000.0000 |  2000.0000 |        - |  422077104 B |
|                         |         |                      |              |           |           |       |         |             |            |          |              |
| CountRowsUsingCursively |    Core |    10k-empty-records |    31.178 ms | 0.2056 ms | 0.1924 ms |  1.00 |    0.00 |           - |          - |        - |         48 B |
| CountRowsUsingCsvHelper |    Core |    10k-empty-records |   630.683 ms | 1.2503 ms | 1.1084 ms | 20.23 |    0.13 |   2000.0000 |          - |        - |  420832856 B |
|                         |         |                      |              |           |           |       |         |             |            |          |              |
| CountRowsUsingCursively |     Clr |               mocked |     4.478 ms | 0.0071 ms | 0.0067 ms |  1.00 |    0.00 |           - |          - |        - |         64 B |
| CountRowsUsingCsvHelper |     Clr |               mocked |   167.477 ms | 0.3523 ms | 0.3296 ms | 37.40 |    0.08 |  18333.3333 |   333.3333 |        - |  116105312 B |
|                         |         |                      |              |           |           |       |         |             |            |          |              |
| CountRowsUsingCursively |    Core |               mocked |     3.233 ms | 0.0063 ms | 0.0059 ms |  1.00 |    0.00 |           - |          - |        - |         48 B |
| CountRowsUsingCsvHelper |    Core |               mocked |   161.791 ms | 0.3473 ms | 0.3249 ms | 50.05 |    0.15 |    333.3333 |          - |        - |  115757736 B |
|                         |         |                      |              |           |           |       |         |             |            |          |              |
| CountRowsUsingCursively |     Clr |       worldcitiespop |   369.738 ms | 0.6855 ms | 0.6077 ms |  1.00 |    0.00 |           - |          - |        - |       8192 B |
| CountRowsUsingCsvHelper |     Clr |       worldcitiespop | 3,598.421 ms | 2.0735 ms | 1.9396 ms |  9.73 |    0.02 | 493000.0000 |  7000.0000 |        - | 3105811440 B |
|                         |         |                      |              |           |           |       |         |             |            |          |              |
| CountRowsUsingCursively |    Core |       worldcitiespop |   237.695 ms | 0.2994 ms | 0.2800 ms |  1.00 |    0.00 |           - |          - |        - |         48 B |
| CountRowsUsingCsvHelper |    Core |       worldcitiespop | 3,620.550 ms | 3.1766 ms | 2.8160 ms | 15.23 |    0.02 |  15000.0000 |          - |        - | 3096694312 B |
