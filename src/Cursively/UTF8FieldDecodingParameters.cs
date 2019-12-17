using System;
using System.Text;

namespace Cursively
{
    /// <summary>
    /// Encapsulates parameters for convenience methods that transform UTF-8 encoded field data into
    /// UTF-16 (<see cref="char"/>) data.
    /// </summary>
    public sealed class UTF8FieldDecodingParameters
    {
        /// <summary>
        /// 
        /// </summary>
        public static readonly UTF8FieldDecodingParameters Default = new UTF8FieldDecodingParameters(InternalConstants.DefaultMaxFieldLengthInChars, InternalConstants.DefaultDecoderFallback);

        /// <summary>
        /// 
        /// </summary>
        public static readonly UTF8FieldDecodingParameters MostPermissive = new UTF8FieldDecodingParameters(InternalConstants.MaxArrayLengthOnMostRuntimes, InternalConstants.ReplacementDecoderFallback);

        private static readonly UTF8Encoding EncodingToUse = new UTF8Encoding(false, false);

        private UTF8FieldDecodingParameters(int maxFieldLength, DecoderFallback decoderFallback)
            => (MaxFieldLength, DecoderFallback) = (maxFieldLength, decoderFallback);

        /// <summary>
        /// 
        /// </summary>
        public int MaxFieldLength { get; }

        /// <summary>
        /// 
        /// </summary>
        public DecoderFallback DecoderFallback { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxFieldLength"></param>
        /// <returns></returns>
        public UTF8FieldDecodingParameters WithMaxFieldLength(int maxFieldLength)
        {
            if (maxFieldLength > InternalConstants.MaxArrayLengthOnMostRuntimes)
            {
                throw new ArgumentOutOfRangeException(nameof(maxFieldLength));
            }

            return maxFieldLength == MaxFieldLength
                ? this
                : new UTF8FieldDecodingParameters(maxFieldLength, DecoderFallback);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="decoderFallback"></param>
        /// <returns></returns>
        public UTF8FieldDecodingParameters WithDecoderFallback(DecoderFallback decoderFallback)
        {
            if (decoderFallback is null)
            {
                throw new ArgumentNullException(nameof(decoderFallback));
            }

            return decoderFallback.Equals(DecoderFallback)
                ? this
                : new UTF8FieldDecodingParameters(MaxFieldLength, decoderFallback);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Decoder CreateUTF8Decoder()
        {
            var result = EncodingToUse.GetDecoder();
            result.Fallback = DecoderFallback;
            return result;
        }
    }
}
