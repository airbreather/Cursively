This benchmark tests the simple act of counting how many records are in a CSV file.  It's not a simple count of how many lines are in the text file: line breaks within quoted fields must be treated as data, and multiple line breaks in a row must be treated as one, since each record must have at least one field.  Therefore, assuming correct implementations, this benchmark should test the raw CSV processing speed.

Cursively eliminates a ton of overhead found in libraries such as CsvHelper by restricting the allowed input encodings and using the visitor pattern as its only means of output.  Cursively can scan through the original bytes of the input to do its work, and it can give slices of the input data directly to the consumer without having to copy or allocate.

Therefore, these benchmarks are somewhat biased in favor of Cursively, as CsvHelper relies on external code to transform the data to UTF-16.  This isn't as unfair as that makes it sound: the overwhelming majority of input files are probably UTF-8 anyway (or a compatible SBCS), so this transformation is something that practically every user will experience.

- Input files can be found here: https://github.com/airbreather/Cursively/tree/v1.0.0/test/Cursively.Benchmark/large-csv-files
- Benchmark source code is a slightly edited* version of this: https://github.com/airbreather/Cursively/tree/v1.0.0/test/Cursively.Benchmark
    - *edited only to remove `CoreRtJob` and the more-or-less redundant `NopUsingCursively`

Raw BenchmarkDotNet output is at the bottom, but here are some numbers derived from it.  The data was fully loaded in main memory when running these tests.  This summary also does not indicate anything about the GC pressure:

|CSV File|Runtime|Library|Throughput|
|-|-|-|-|
|100 records / 10,000 tiny fields each|.NET 4.7.2|Cursively|99.81 MiB/s|
|100 records / 10,000 tiny fields each|.NET 4.7.2|CsvHelper|22.60 MiB/s|
|100 records / 10,000 tiny fields each|.NET Core 2.2.5|Cursively|126.1 MiB/s|
|100 records / 10,000 tiny fields each|.NET Core 2.2.5|CsvHelper|25.32 MiB/s|
|100 records / 10,000 tiny quoted fields each|.NET 4.7.2|Cursively|118.5 MiB/s|
|100 records / 10,000 tiny quoted fields each|.NET 4.7.2|CsvHelper|25.05 MiB/s|
|100 records / 10,000 tiny quoted fields each|.NET Core 2.2.5|Cursively|187.0 MiB/s|
|100 records / 10,000 tiny quoted fields each|.NET Core 2.2.5|CsvHelper|27.96 MiB/s|
|10,000 records / 1,000 empty fields each|.NET 4.7.2|Cursively|64.15 MiB/s|
|10,000 records / 1,000 empty fields each|.NET 4.7.2|CsvHelper|15.57 MiB/s|
|10,000 records / 1,000 empty fields each|.NET Core 2.2.5|Cursively|112.7 MiB/s|
|10,000 records / 1,000 empty fields each|.NET Core 2.2.5|CsvHelper|14.84 MiB/s|
|Mock data from Mockaroo|.NET 4.7.2|Cursively|1.637 GiB/s|
|Mock data from Mockaroo|.NET 4.7.2|CsvHelper|74.81 MiB/s|
|Mock data from Mockaroo|.NET Core 2.2.5|Cursively|1.893 GiB/s|
|Mock data from Mockaroo|.NET Core 2.2.5|CsvHelper|66.86 MiB/s|

Raw BenchmarkDotNet output:

``` ini

BenchmarkDotNet=v0.11.5, OS=Windows 10.0.17134.765 (1803/April2018Update/Redstone4)
Intel Core i7-6850K CPU 3.60GHz (Skylake), 1 CPU, 12 logical and 6 physical cores
Frequency=3515622 Hz, Resolution=284.4447 ns, Timer=TSC
.NET Core SDK=2.2.300
  [Host]     : .NET Core 2.2.5 (CoreCLR 4.6.27617.05, CoreFX 4.6.27618.01), 64bit RyuJIT
  Job-ASLTDW : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.3416.0
  Job-RICADF : .NET Core 2.2.5 (CoreCLR 4.6.27617.05, CoreFX 4.6.27618.01), 64bit RyuJIT

Server=True  

```
|                  Method | Runtime |              csvFile |       Mean |     Error |    StdDev | Ratio | RatioSD |      Gen 0 |     Gen 1 |    Gen 2 |   Allocated |
|------------------------ |-------- |--------------------- |-----------:|----------:|----------:|------:|--------:|-----------:|----------:|---------:|------------:|
| CountRowsUsingCursively |     Clr |     100-huge-records |  27.714 ms | 0.0126 ms | 0.0105 ms |  1.00 |    0.00 |          - |         - |        - |       256 B |
| CountRowsUsingCsvHelper |     Clr |     100-huge-records | 122.397 ms | 0.1685 ms | 0.1494 ms |  4.42 |    0.01 | 17250.0000 | 6250.0000 | 750.0000 | 110257334 B |
|                         |         |                      |            |           |           |       |         |            |           |          |             |
| CountRowsUsingCursively |    Core |     100-huge-records |  21.932 ms | 0.0254 ms | 0.0226 ms |  1.00 |    0.00 |          - |         - |        - |        56 B |
| CountRowsUsingCsvHelper |    Core |     100-huge-records | 109.261 ms | 0.3319 ms | 0.3104 ms |  4.98 |    0.02 |   400.0000 |  200.0000 |        - | 110256320 B |
|                         |         |                      |            |           |           |       |         |            |           |          |             |
| CountRowsUsingCursively |     Clr | 100-h(...)uoted [23] |  39.453 ms | 0.0974 ms | 0.0864 ms |  1.00 |    0.00 |          - |         - |        - |       683 B |
| CountRowsUsingCsvHelper |     Clr | 100-h(...)uoted [23] | 186.572 ms | 0.4682 ms | 0.4380 ms |  4.73 |    0.01 | 24666.6667 | 9666.6667 | 666.6667 | 153595995 B |
|                         |         |                      |            |           |           |       |         |            |           |          |             |
| CountRowsUsingCursively |    Core | 100-h(...)uoted [23] |  24.995 ms | 0.0160 ms | 0.0142 ms |  1.00 |    0.00 |          - |         - |        - |        56 B |
| CountRowsUsingCsvHelper |    Core | 100-h(...)uoted [23] | 167.160 ms | 0.3437 ms | 0.3215 ms |  6.69 |    0.02 |   333.3333 |         - |        - | 153579848 B |
|                         |         |                      |            |           |           |       |         |            |           |          |             |
| CountRowsUsingCursively |     Clr |    10k-empty-records | 148.952 ms | 0.2502 ms | 0.2340 ms |  1.00 |    0.00 |          - |         - |        - |      2048 B |
| CountRowsUsingCsvHelper |     Clr |    10k-empty-records | 613.718 ms | 0.8869 ms | 0.7862 ms |  4.12 |    0.01 | 66000.0000 | 2000.0000 |        - | 420838944 B |
|                         |         |                      |            |           |           |       |         |            |           |          |             |
| CountRowsUsingCursively |    Core |    10k-empty-records |  84.801 ms | 0.1079 ms | 0.1009 ms |  1.00 |    0.00 |          - |         - |        - |        56 B |
| CountRowsUsingCsvHelper |    Core |    10k-empty-records | 644.051 ms | 2.8782 ms | 2.5515 ms |  7.60 |    0.03 |  2000.0000 |         - |        - | 420832856 B |
|                         |         |                      |            |           |           |       |         |            |           |          |             |
| CountRowsUsingCursively |     Clr |               mocked |   7.242 ms | 0.0233 ms | 0.0207 ms |  1.00 |    0.00 |          - |         - |        - |        64 B |
| CountRowsUsingCsvHelper |     Clr |               mocked | 162.298 ms | 0.2958 ms | 0.2622 ms | 22.41 |    0.08 | 18000.0000 |  333.3333 |        - | 115764389 B |
|                         |         |                      |            |           |           |       |         |            |           |          |             |
| CountRowsUsingCursively |    Core |               mocked |   6.264 ms | 0.0115 ms | 0.0107 ms |  1.00 |    0.00 |          - |         - |        - |        56 B |
| CountRowsUsingCsvHelper |    Core |               mocked | 181.592 ms | 0.3413 ms | 0.3193 ms | 28.99 |    0.09 |   333.3333 |         - |        - | 115757736 B |
