using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Cursively
{
    /// <summary>
    /// Raised by <see cref="CsvReaderVisitorWithUTF8HeadersBase"/> when the number of headers
    /// exceeds the configured maximum.
    /// </summary>
    [Serializable]
    [SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Exception is not intended to be created externally.")]
    public sealed class CursivelyTooManyHeadersException : CursivelyDataStreamException
    {
        internal CursivelyTooManyHeadersException(int maxHeaderCount)
            : base($"CSV stream contains more headers than the configured maximum of {maxHeaderCount}.")
        {
        }

        private CursivelyTooManyHeadersException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
