using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Cursively
{
    /// <summary>
    /// Serves as the base class for exceptions thrown by this library to indicate problems with the
    /// actual contents of a CSV stream.
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public abstract class CursivelyDataStreamException : Exception
    {
        private protected CursivelyDataStreamException(string message)
            : base(message)
        {
        }

        private protected CursivelyDataStreamException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        [SuppressMessage("Microsoft.Usage", "CA2229:ImplementSerializationConstructors")]
        private protected CursivelyDataStreamException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
