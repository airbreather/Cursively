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
            ProcessFullSegment(_bytes.Span, _ignoreUTF8ByteOrderMark, tokenizer, visitor);
        }

        internal static unsafe void ProcessFullSegment(ReadOnlySpan<byte> bytes, bool ignoreUTF8ByteOrderMark, CsvTokenizer tokenizer, CsvReaderVisitorBase visitor)
        {
            if (ignoreUTF8ByteOrderMark &&
                bytes.Length >= 3 &&
                bytes[0] == 0xEF &&
                bytes[1] == 0xBB &&
                bytes[2] == 0xBF)
            {
                bytes = bytes.Slice(3);
            }

            if (!bytes.IsEmpty)
            {
                tokenizer.ProcessNextChunk(bytes, visitor);
            }

            tokenizer.ProcessEndOfStream(visitor);
        }
    }
}
