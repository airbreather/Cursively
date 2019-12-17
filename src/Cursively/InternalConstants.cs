using System.Text;

namespace Cursively
{
    internal static class InternalConstants
    {
        public const int MaxArrayLengthOnMostRuntimes = 0x7FEFFFFF;

        public const int DefaultMaxFieldCountPerRecord = 1_000;

        public const int DefaultMaxFieldLengthInChars = 100;

        public static readonly DecoderFallback DefaultDecoderFallback = new CursivelyDecoderExceptionFallback();

        public static readonly DecoderFallback ReplacementDecoderFallback = Encoding.UTF8.DecoderFallback;
    }
}
