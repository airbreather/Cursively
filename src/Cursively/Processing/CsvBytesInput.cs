using System;

namespace Cursively.Processing
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CsvBytesInput : CsvInput
    {
        private readonly ReadOnlyMemory<byte> _bytes;

        internal CsvBytesInput(byte delimiter, ReadOnlyMemory<byte> bytes)
            : base(delimiter, false)
        {
            _bytes = bytes;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public CsvBytesInput WithDelimiter(byte delimiter) =>
            new CsvBytesInput(delimiter, _bytes);

        /// <inheritdoc />
        protected override void Process(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor)
        {
            tokenizer.ProcessNextChunk(_bytes.Span, visitor);
            tokenizer.ProcessEndOfStream(visitor);
        }
    }
}
