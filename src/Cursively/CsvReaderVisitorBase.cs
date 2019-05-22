using System;

namespace Cursively
{
    /// <summary>
    /// Base class for listeners that process a stream of RFC 4180 (CSV) tokens from an instance of
    /// <see cref="CsvTokenizer"/>.
    /// </summary>
    /// <remarks>
    /// Remarks on the documentation of individual abstract methods indicate when the tokenizer is
    /// legally allowed to call that method.
    /// </remarks>
    public abstract class CsvReaderVisitorBase
    {
        /// <summary>
        /// An implementation of <see cref="CsvReaderVisitorBase"/> that does nothing when it sees
        /// any of the tokens.
        /// </summary>
        public static readonly CsvReaderVisitorBase Null = new NullVisitor();

        /// <summary>
        /// Visits part of a field's data.
        /// </summary>
        /// <param name="chunk">
        /// The data from this part of the field.
        /// </param>
        /// <remarks>
        /// <para>
        /// This method may be called at any time.
        /// </para>
        /// <para>
        /// Only <see cref="VisitPartialFieldContents"/> and <see cref="VisitEndOfField"/> may be
        /// called directly after a call to this method.
        /// </para>
        /// <para>
        /// There are multiple reasons why this method may be called instead of going straight to
        /// calling <see cref="VisitEndOfField"/>:
        /// </para>
        /// <list type="number">
        /// <item>
        /// Field is split across multiple read buffer chunks, or else it runs up to the very end of
        /// a read buffer chunk, but we can't prove it without the first byte of the next chunk or a
        /// <see cref="CsvTokenizer.ProcessEndOfStream"/> call.
        /// </item>
        /// <item>
        /// Quoted field contains a literal quote that was escaped in the original stream, and so we
        /// cannot yield the entire field data as-is.
        /// </item>
        /// <item>
        /// Stream does not conform to RFC 4180, and optimizing such streams to avoid this case.
        /// </item>
        /// </list>
        /// </remarks>
        public abstract void VisitPartialFieldContents(ReadOnlySpan<byte> chunk);

        /// <summary>
        /// Visits the last part of a field's data.
        /// </summary>
        /// <param name="chunk">
        /// The data from the last part of the field.
        /// </param>
        /// <remarks>
        /// <para>
        /// This method may be called at any time.
        /// </para>
        /// <para>
        /// Any method, including this one, may be called directly after a call to this method.
        /// </para>
        /// <para>
        /// This method may be called without a preceding <see cref="VisitPartialFieldContents"/>
        /// call, if the field's entire data is contained within the given chunk.
        /// </para>
        /// </remarks>
        public abstract void VisitEndOfField(ReadOnlySpan<byte> chunk);

        /// <summary>
        /// Notifies that all fields in the current record have been visited.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method may only be called as the very next method that gets called after a call to
        /// <see cref="VisitEndOfField"/>.
        /// </para>
        /// <para>
        /// Only <see cref="VisitPartialFieldContents"/> and <see cref="VisitEndOfField"/> may be
        /// called directly after a call to this method.
        /// </para>
        /// </remarks>
        public abstract void VisitEndOfRecord();

        private sealed class NullVisitor : CsvReaderVisitorBase
        {
            public override void VisitEndOfRecord() { }

            public override void VisitPartialFieldContents(ReadOnlySpan<byte> chunk) { }

            public override void VisitEndOfField(ReadOnlySpan<byte> chunk) { }
        }
    }
}
