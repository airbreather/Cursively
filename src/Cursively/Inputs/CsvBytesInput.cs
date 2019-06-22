using System;

namespace Cursively.Inputs
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CsvBytesInput : CsvInput
    {
        private readonly ReadOnlyMemory<byte> _bytes;

        private readonly bool _ignoreUTF8ByteOrderMark;

        internal CsvBytesInput(byte delimiter, ReadOnlyMemory<byte> bytes, bool ignoreUTF8ByteOrderMark)
            : base(delimiter, requiresExplicitReset: false)
        {
            _bytes = bytes;
            _ignoreUTF8ByteOrderMark = ignoreUTF8ByteOrderMark;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public CsvBytesInput WithDelimiter(byte delimiter) =>
            new CsvBytesInput(delimiter, _bytes, _ignoreUTF8ByteOrderMark);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ignoreUTF8ByteOrderMark"></param>
        /// <returns></returns>
        public CsvBytesInput WithIgnoreUTF8ByteOrderMark(bool ignoreUTF8ByteOrderMark) =>
            new CsvBytesInput(Delimiter, _bytes, ignoreUTF8ByteOrderMark);

        /// <inheritdoc />
        protected override void Process(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor)
        {
            var span = _bytes.Span;
            if (_ignoreUTF8ByteOrderMark &&
                span.Length >= 3 &&
                span[0] == 0xEF &&
                span[1] == 0xBB &&
                span[2] == 0xBF)
            {
                span = span.Slice(3);
            }

            if (!span.IsEmpty)
            {
                tokenizer.ProcessNextChunk(span, visitor);
            }

            tokenizer.ProcessEndOfStream(visitor);
        }
    }
}
