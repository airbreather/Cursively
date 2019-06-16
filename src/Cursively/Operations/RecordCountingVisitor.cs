using System;

namespace Cursively.Operations
{
    internal sealed class RecordCountingVisitor : CsvReaderVisitorBase
    {
        private long _recordCount;

        public long RecordCount => _recordCount;

        public override void VisitPartialFieldContents(ReadOnlySpan<byte> chunk) { }

        public override void VisitEndOfField(ReadOnlySpan<byte> chunk) { }

        public override void VisitEndOfRecord() => ++_recordCount;
    }
}
