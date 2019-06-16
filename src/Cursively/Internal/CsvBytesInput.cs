using System;

namespace Cursively.Internal
{
    internal sealed class CsvBytesInput : CsvInput
    {
        private readonly ReadOnlyMemory<byte> _bytes;

        public CsvBytesInput(byte delimiter, ReadOnlyMemory<byte> bytes)
            : base(delimiter)
        {
            _bytes = bytes;
        }

        public override CsvInput WithDelimiter(byte delimiter) =>
            new CsvBytesInput(delimiter, _bytes);

        protected override void Process(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor)
        {
            tokenizer.ProcessNextChunk(_bytes.Span, visitor);
            tokenizer.ProcessEndOfStream(visitor);
        }
    }
}
