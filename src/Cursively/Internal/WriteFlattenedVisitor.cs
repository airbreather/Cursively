using System;
using System.IO;
using System.Text;

namespace Cursively.Internal
{
    internal sealed class WriteFlattenedVisitor : CsvReaderVisitorWithUTF8HeadersBase
    {
        private static readonly UTF8Encoding EncodingToUse = new UTF8Encoding(false, false);

        private readonly TextWriter _outputSink;

        private readonly char[] _fieldBuffer;

        private readonly Decoder _fieldDataDecoder;

        private int _fieldBufferConsumed;

        public WriteFlattenedVisitor(int maxHeaderCount, int maxFieldLength, bool ignoreUTF8IdentifierOnFirstHeaderField, DecoderFallback decoderFallback, TextWriter outputSink)
            : base(maxHeaderCount, maxFieldLength, ignoreUTF8IdentifierOnFirstHeaderField, decoderFallback)
        {
            _outputSink = outputSink;

            _fieldBuffer = new char[maxFieldLength];

            _fieldDataDecoder = EncodingToUse.GetDecoder();
            _fieldDataDecoder.Fallback = decoderFallback;
        }

        protected override unsafe void VisitPartialDataFieldContents(ReadOnlySpan<byte> chunk)
        {
            if (chunk.IsEmpty)
            {
                return;
            }

            fixed (byte* b = &chunk[0])
            {
                VisitFieldData(b, chunk.Length, false);
            }
        }

        protected override unsafe void VisitEndOfDataField(ReadOnlySpan<byte> chunk)
        {
            if (chunk.IsEmpty)
            {
                byte b = 0xFF;
                VisitFieldData(&b, 0, true);
            }
            else
            {
                fixed (byte* b = &chunk[0])
                {
                    VisitFieldData(b, chunk.Length, true);
                }
            }

            _outputSink.Write("[" + Headers[CurrentFieldIndex] + "] = ");
            _outputSink.WriteLine(_fieldBuffer, 0, _fieldBufferConsumed);
            _fieldBufferConsumed = 0;
        }

        protected override void VisitEndOfDataRecord() { }

        private unsafe void VisitFieldData(byte* b, int byteCount, bool flush)
        {
            int charCount = _fieldDataDecoder.GetCharCount(b, byteCount, flush);
            if (_fieldBufferConsumed + charCount <= _fieldBuffer.Length)
            {
                fixed (char* c = &_fieldBuffer[_fieldBufferConsumed])
                {
                    _fieldDataDecoder.GetChars(b, byteCount, c, charCount, flush);
                    _fieldBufferConsumed += charCount;
                }
            }
            else
            {
                throw new CursivelyDataFieldIsTooLongException(_fieldBuffer.Length);
            }
        }
    }
}
