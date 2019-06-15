# Cursively
A fast, [RFC 4180](https://tools.ietf.org/html/rfc4180)-conforming CSV reading library for .NET.  Written in C#.

| License | CI (AppVeyor) | NuGet | MyGet (pre-release) |
| ------- | ------------- | ----- | ------------------- |
| [![License](https://img.shields.io/github/license/airbreather/Cursively.svg)](https://github.com/airbreather/Cursively/blob/develop/LICENSE.md) | [![CI](https://ci.appveyor.com/api/projects/status/aqr1kmj9qqfx6ple?svg=true)](https://ci.appveyor.com/project/airbreather/Cursively) | [![NuGet](https://img.shields.io/nuget/v/Cursively.svg)](https://www.nuget.org/packages/Cursively/) | [![MyGet](https://img.shields.io/myget/airbreather/vpre/Cursively.svg?style=flat)](https://myget.org/feed/airbreather/package/nuget/Cursively) |

## Documentation
Documentation is currently being published as [GitHub Pages](https://airbreather.github.io/Cursively/index.html).

## Usage
Create a subclass of `CsvReaderVisitorBase` (or one of its own built-in subclasses) with your own logic for processing the individual elements in order.  Then, you have some options.

### Example Visitor
```csharp
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
```

### Fastest
All of the other methods of processing the data are built on top of this, so it gives you the most control:
1. Create a new instance of your visitor.
1. Create a new instance of `CsvTokenizer`.
1. Call `CsvTokenizer.ProcessNextChunk` for each chunk of the file.
1. Call `CsvTokenizer.ProcessEndOfStream` after the last chunk of the file.

Example:
```csharp
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
```

### Simpler
1. Create a new instance of your visitor.
1. Call one of the `Csv.Process*` methods, passing in whatever format your data is in along with your visitor.

Examples:
```csharp
public static void ProcessCsvFile(string csvFilePath)
{
    Console.WriteLine($"Started reading '{csvFilePath}'.");
    Csv.ProcessFile(csvFilePath, new MyVisitor(maxFieldLength: 1000));
    Console.WriteLine($"Finished reading '{csvFilePath}'.");
}

public static void ProcessCsvStream(Stream csvStream)
{
    Console.WriteLine($"Started reading '{csvFilePath}'.");
    Csv.ProcessStream(csvStream, new MyVisitor(maxFieldLength: 1000));
    Console.WriteLine($"Finished reading '{csvFilePath}'.");
}

public static async ValueTask ProcessCsvStreamAsync(Stream csvStream, IProgress<int> progress = null, CancellationToken cancellationToken = default)
{
    Console.WriteLine($"Started reading '{csvFilePath}'.");
    await Csv.ProcessStreamAsync(csvStream, new MyVisitor(maxFieldLength: 1000), progress, cancellationToken);
    Console.WriteLine($"Finished reading '{csvFilePath}'.");
}
```
