using System;
using System.Collections.Generic;
using System.Text;

namespace Cursively.Tests
{
    internal sealed class StringBufferingVisitor : CsvReaderVisitorBase
    {
        private static readonly UTF8Encoding TheEncoding = new UTF8Encoding(false, false);

        private readonly List<string> _fields = new List<string>();

        private readonly byte[] _cutBuffer;

        private int _cutBufferConsumed;

        public StringBufferingVisitor(int fileLength) => _cutBuffer = new byte[Math.Max(fileLength, 3)];

        public List<string[]> Records { get; } = new List<string[]>();

        public override void VisitEndOfRecord()
        {
            Records.Add(_fields.ToArray());
            _fields.Clear();
        }

        public override void VisitPartialFieldContents(ReadOnlySpan<byte> chunk) => CopyToCutBuffer(chunk);

        public override void VisitEndOfField(ReadOnlySpan<byte> chunk)
        {
            if (_cutBufferConsumed != 0)
            {
                CopyToCutBuffer(chunk);
                chunk = new ReadOnlySpan<byte>(_cutBuffer, 0, _cutBufferConsumed);
            }

            _fields.Add(TheEncoding.GetString(chunk));
            _cutBufferConsumed = 0;
        }

        private void CopyToCutBuffer(ReadOnlySpan<byte> chunk)
        {
            chunk.CopyTo(new Span<byte>(_cutBuffer, _cutBufferConsumed, chunk.Length));
            _cutBufferConsumed += chunk.Length;
        }
    }
}
