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
            if (ignoreUTF8ByteOrderMark)
            {
                var head = new ReadOnlySpan<byte>(UTF8BOM, 0, bytes.Length < UTF8BOM.Length ? bytes.Length : UTF8BOM.Length);
                if (bytes.StartsWith(head))
                {
                    bytes = bytes.Slice(head.Length);
                }
            }

            tokenizer.ProcessNextChunk(bytes, visitor);
            tokenizer.ProcessEndOfStream(visitor);
        }
    }
}
