using System;
using System.Text;

namespace Cursively.Internal
{
    internal sealed class CsvCharsInput : CsvInput
    {
        private readonly ReadOnlyMemory<char> _chars;

        public CsvCharsInput(byte delimiter, ReadOnlyMemory<char> chars)
            : base(delimiter)
        {
            _chars = chars;
        }

        public override CsvInput WithDelimiter(byte delimiter) =>
            new CsvCharsInput(delimiter, _chars);

        protected override unsafe void Process(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor)
        {
            var charSpan = _chars.Span;
            if (charSpan.IsEmpty)
            {
                return;
            }

            byte[] bytes;
            fixed (char* c = &charSpan[0])
            {
                bytes = new byte[Encoding.UTF8.GetByteCount(c, charSpan.Length)];
                fixed (byte* b = &bytes[0])
                {
                    Encoding.UTF8.GetBytes(c, charSpan.Length, b, bytes.Length);
                }
            }

            tokenizer.ProcessNextChunk(bytes, visitor);
            tokenizer.ProcessEndOfStream(visitor);
        }
    }
}
