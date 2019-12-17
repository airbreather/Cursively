using System;

namespace Cursively
{
    internal sealed class ValidatingCsvReaderVisitorWrapper : CsvReaderVisitorBase
    {
        private readonly CsvReaderVisitorBase _inner;

        private CallStateFlags _callStateFlags;

        public ValidatingCsvReaderVisitorWrapper(CsvReaderVisitorBase inner)
            => _inner = inner;

        [Flags]
        private enum CallStateFlags
        {
            None                            = 0b000,
            VisitedPartialFieldPreviously   = 0b001,
            VisitedEndOfFieldPreviously     = 0b010,
            QuotedFieldIsAlreadyNonstandard = 0b100,
        }

        public override void VisitPartialFieldContents(ReadOnlySpan<byte> chunk)
        {
            _callStateFlags = (_callStateFlags | CallStateFlags.VisitedPartialFieldPreviously) & ~CallStateFlags.VisitedEndOfFieldPreviously;
            _inner.VisitPartialFieldContents(chunk);
        }

        public override void VisitNonstandardQuotedField()
        {
            if ((_callStateFlags & (CallStateFlags.VisitedPartialFieldPreviously | CallStateFlags.QuotedFieldIsAlreadyNonstandard)) != CallStateFlags.VisitedPartialFieldPreviously)
            {
                throw new InvalidOperationException($"{nameof(VisitNonstandardQuotedField)} may only be called immediately after a call to {nameof(VisitPartialFieldContents)}, and only once per field.");
            }

            _callStateFlags = (_callStateFlags | CallStateFlags.QuotedFieldIsAlreadyNonstandard) & ~(CallStateFlags.VisitedPartialFieldPreviously | CallStateFlags.VisitedEndOfFieldPreviously);
            _inner.VisitNonstandardQuotedField();
        }

        public override void VisitEndOfField(ReadOnlySpan<byte> chunk)
        {
            _callStateFlags = CallStateFlags.VisitedEndOfFieldPreviously;
            _inner.VisitEndOfField(chunk);
        }

        public override void VisitEndOfRecord()
        {
            if ((_callStateFlags & CallStateFlags.VisitedEndOfFieldPreviously) != CallStateFlags.VisitedEndOfFieldPreviously)
            {
                throw new InvalidOperationException($"{nameof(VisitEndOfRecord)} may only be called immediately after a call to {nameof(VisitEndOfField)}.");
            }

            _callStateFlags = CallStateFlags.None;
            _inner.VisitEndOfRecord();
        }
    }
}
