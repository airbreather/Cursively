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
    [SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Exception is not intended to be created externally.")]
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

        [SuppressMessage("Usage", "CA2229:Implement serialization constructors", Justification = "Exception is not intended to be created externally.")]
        private protected CursivelyDataStreamException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
