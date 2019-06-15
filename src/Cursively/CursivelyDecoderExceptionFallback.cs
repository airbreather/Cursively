using System.Text;

namespace Cursively
{
    internal sealed class CursivelyDecoderExceptionFallback : DecoderFallback
    {
        public override int MaxCharCount => 0;

        public override DecoderFallbackBuffer CreateFallbackBuffer() => new CursivelyDecoderExceptionFallbackBuffer();

        public override bool Equals(object obj) => obj is CursivelyDecoderExceptionFallback;

        public override int GetHashCode() => 1234;

        private sealed class CursivelyDecoderExceptionFallbackBuffer : DecoderFallbackBuffer
        {
            public override int Remaining => 0;

            public override char GetNextChar() => '\0';

            public override bool MovePrevious() => false;

            public override bool Fallback(byte[] bytesUnknown, int index)
            {
                // use the built-in logic to get a helpful exception message.
                var inner = new DecoderExceptionFallbackBuffer();
                try
                {
                    return inner.Fallback(bytesUnknown, index);
                }
                catch (DecoderFallbackException ex)
                {
                    // wrap it.  C# / .NET do not support multiple inheritance, and I think it's
                    // more important for consumers to be able to catch CursivelyDataStreamException
                    // for all exceptions in the form of "this breaks one of Cursively's rules, but
                    // the system is otherwise operating normally".
                    throw new CursivelyHeadersAreNotUTF8Exception(ex);
                }
            }
        }
    }
}
