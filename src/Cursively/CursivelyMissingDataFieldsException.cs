using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Cursively
{
    /// <summary>
    /// Raised by <see cref="CsvReaderVisitorWithUTF8HeadersBase"/>, by default, when a data record
    /// contains fewer fields than the header record.
    /// </summary>
    [Serializable]
    [SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Exception is not intended to be created externally.")]
    public sealed class CursivelyMissingDataFieldsException : CursivelyDataStreamException
    {
        internal CursivelyMissingDataFieldsException(int headerFieldCount, int dataFieldCount)
            : base($"CSV stream contains a non-header record with only {dataFieldCount} field(s), fewer than the {headerFieldCount} field(s) present in the header record.")
        {
        }

        private CursivelyMissingDataFieldsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
