using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Cursively
{
    /// <summary>
    /// Raised when the number of headers exceeds the configured maximum.
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
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
