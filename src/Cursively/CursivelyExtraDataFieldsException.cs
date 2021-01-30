using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Cursively
{
    /// <summary>
    /// Raised by <see cref="CsvReaderVisitorWithUTF8HeadersBase"/>, by default, when a data record
    /// contains more fields than the header record.
    /// </summary>
    [Serializable]
    [SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Exception is not intended to be created externally.")]
    public sealed class CursivelyExtraDataFieldsException : CursivelyDataStreamException
    {
        internal CursivelyExtraDataFieldsException(int headerFieldCount)
            : base($"CSV stream contains a non-header record with more fields than the {headerFieldCount} field(s) present in the header record.")
        {
        }

        private CursivelyExtraDataFieldsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
