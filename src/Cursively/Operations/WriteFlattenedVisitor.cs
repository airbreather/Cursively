using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Cursively.Operations
{
    internal sealed class WriteFlattenedVisitor : CsvReaderVisitorWithUTF8HeadersBase
    {
        private static readonly UTF8Encoding EncodingToUse = new UTF8Encoding(false, false);

        private readonly TextWriter _outputSink;

        private readonly char[] _fieldBuffer;

        private readonly Decoder _fieldDataDecoder;

        private int _fieldBufferConsumed;

        private string[] _leadingSpaces;

        public WriteFlattenedVisitor(int maxHeaderCount, int maxFieldLength, bool ignoreUTF8IdentifierOnFirstHeaderField, DecoderFallback decoderFallback, TextWriter outputSink)
            : base(maxHeaderCount, maxFieldLength, ignoreUTF8IdentifierOnFirstHeaderField, decoderFallback)
        {
            _outputSink = outputSink;

            _fieldBuffer = new char[maxFieldLength];

            _fieldDataDecoder = EncodingToUse.GetDecoder();
            _fieldDataDecoder.Fallback = decoderFallback;
        }

        protected override void VisitEndOfHeaderRecord()
        {
            var headers = Headers;

            int longestHeaderLength = -1;
            foreach (string header in headers)
            {
                if (header.Length > longestHeaderLength)
                {
                    longestHeaderLength = header.Length;
                }
            }

            var leadingSpacesByLength = new Dictionary<int, string> { [0] = string.Empty };
            string[] leadingSpacesByHeader = new string[headers.Length];
            for (int i = 0; i < leadingSpacesByHeader.Length; i++)
            {
                int leadingSpaceCount = longestHeaderLength - headers[i].Length;
                if (!leadingSpacesByLength.TryGetValue(leadingSpaceCount, out string leadingSpaces))
                {
                    leadingSpaces = leadingSpacesByLength[leadingSpaceCount] = new string(' ', leadingSpaceCount);
                }

                leadingSpacesByHeader[i] = leadingSpaces;
            }

            _leadingSpaces = leadingSpacesByHeader;
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

            string leadingSpaces = _leadingSpaces[CurrentFieldIndex];
            if (leadingSpaces.Length != 0)
            {
                _outputSink.Write(leadingSpaces);
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
