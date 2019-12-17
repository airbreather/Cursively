using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Cursively
{
    /// <summary>
    /// Raised when the length of some field exceeds the configured maximum.
    /// </summary>
    [Serializable]
    [SuppressMessage("Design", "CA1032:Implement standard exception constructors")]
    public class CursivelyFieldIsTooLongException : CursivelyDataStreamException
    {
        internal CursivelyFieldIsTooLongException(int maxLength)
            : this($"CSV stream contains a field that is longer than the configured max length of {maxLength}.")
        {
        }

        internal CursivelyFieldIsTooLongException(string message)
            : base(message)
        {
        }

        [SuppressMessage("Usage", "CA2229:Implement serialization constructors")]
        private protected CursivelyFieldIsTooLongException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
