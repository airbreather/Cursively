using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Text;

namespace Cursively
{
    /// <summary>
    /// Raised by <see cref="CsvReaderVisitorWithUTF8HeadersBase"/>, by default, when the header
    /// record contains invalid UTF-8 bytes.
    /// </summary>
    [Serializable]
    [SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Exception is not intended to be created externally.")]
    public sealed class CursivelyHeadersAreNotUTF8Exception : CursivelyDataStreamException
    {
        internal CursivelyHeadersAreNotUTF8Exception(DecoderFallbackException innerException)
            : base(innerException.Message, innerException)
        {
        }

        private CursivelyHeadersAreNotUTF8Exception(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Gets the <see cref="DecoderFallbackException"/> instance that holds the actual decoder
        /// state when the current exception was raised.
        /// </summary>
        public DecoderFallbackException InnerDecoderFallbackException => (DecoderFallbackException)InnerException;
    }
}
