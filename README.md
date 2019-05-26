# Cursively
A fast, [RFC 4180](https://tools.ietf.org/html/rfc4180)-conforming CSV reading library for .NET.  Written in C#.

## Usage
1. Create a subclass of `CsvReaderVisitorBase` with your own logic.
1. To read a CSV file:
    - Create a new instance of your visitor.
    - Create a new instance of `CsvTokenizer`.
    - Call `CsvTokenizer.ProcessNextChunk` for each chunk of the file.
    - Call `CsvTokenizer.ProcessEndOfStream` after the last chunk of the file.

## Example
This demonstrates using Cursively to write the details of a particular UTF-8 encoded file to the console.

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
        if (charCount + _bufferConsumed < _buffer.Length)
        {
            _utf8Decoder.GetChars(chunk, new Span<char>(_buffer, _bufferConsumed, charCount), flush);
            _bufferConsumed += charCount;
        }
        else
        {
            throw new InvalidDataException($"Field is longer than {_buffer.Length} characters.");
        }

        if (!flush)
        {
            return;
        }

        Console.Write("Field: ");
        Console.WriteLine(_buffer, 0, _bufferConsumed);
        _bufferConsumed = 0;
    }
}
```
