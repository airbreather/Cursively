using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Cursively
{
    /// <summary>
    /// Raised by <see cref="CsvReaderVisitorWithUTF8HeadersBase"/> when the length of a header
    /// exceeds the configured maximum.
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public sealed class CursivelyHeaderIsTooLongException : CursivelyFieldIsTooLongException
    {
        internal CursivelyHeaderIsTooLongException(int maxLength)
            : base($"CSV stream contains a header that is longer than the configured max length of {maxLength}.")
        {
        }

        private CursivelyHeaderIsTooLongException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
