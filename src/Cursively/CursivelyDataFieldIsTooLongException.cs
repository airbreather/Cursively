using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Cursively
{
    /// <summary>
    /// Raised when the length of a non-header field exceeds the configured maximum.
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public sealed class CursivelyDataFieldIsTooLongException : CursivelyDataStreamException
    {
        internal CursivelyDataFieldIsTooLongException(int maxLength)
            : base($"CSV stream contains a non-header field that is longer than the configured max length of {maxLength}.")
        {
        }

        private CursivelyDataFieldIsTooLongException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
