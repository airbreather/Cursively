using System;
using System.Buffers;
using System.Text;

namespace Cursively.Inputs
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CsvCharSequenceInput : CsvInput
    {
        private readonly ReadOnlySequence<char> _chars;

        private readonly int _encodeBatchCharCount;

        private readonly MemoryPool<byte> _encodeBufferPool;

        private readonly bool _ignoreByteOrderMark;

        internal CsvCharSequenceInput(byte delimiter, ReadOnlySequence<char> chars, int encodeBatchCharCount, MemoryPool<byte> encodeBufferPool, bool ignoreByteOrderMark)
            : base(delimiter, requiresExplicitReset: false)
        {
            _chars = chars;
            _encodeBatchCharCount = encodeBatchCharCount;
            _ignoreByteOrderMark = ignoreByteOrderMark;
            _encodeBufferPool = encodeBufferPool;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public CsvCharSequenceInput WithDelimiter(byte delimiter) =>
            new CsvCharSequenceInput(delimiter, _chars, _encodeBatchCharCount, _encodeBufferPool, _ignoreByteOrderMark);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="encodeBatchCharCount"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public CsvCharSequenceInput WithEncodeBatchCharCount(int encodeBatchCharCount)
        {
            if (encodeBatchCharCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(encodeBatchCharCount), encodeBatchCharCount, "Must be greater than zero.");
            }

            return new CsvCharSequenceInput(Delimiter, _chars, encodeBatchCharCount, _encodeBufferPool, _ignoreByteOrderMark);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="encodeBufferPool"></param>
        /// <returns></returns>
        public CsvCharSequenceInput WithEncodeBufferPool(MemoryPool<byte> encodeBufferPool) =>
            new CsvCharSequenceInput(Delimiter, _chars, _encodeBatchCharCount, encodeBufferPool, _ignoreByteOrderMark);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ignoreByteOrderMark"></param>
        /// <returns></returns>
        public CsvCharSequenceInput WithIgnoreByteOrderMark(bool ignoreByteOrderMark) =>
            new CsvCharSequenceInput(Delimiter, _chars, _encodeBatchCharCount, _encodeBufferPool, ignoreByteOrderMark);

        /// <inheritdoc />
        protected override void Process(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor)
        {
            var encoding = CsvCharsInput.TheEncoding;
            var encoder = encoding.GetEncoder();
            int encodeBatchCharCount = _encodeBatchCharCount;
            bool ignoreByteOrderMark = _ignoreByteOrderMark;
            var encodeBufferPool = _encodeBufferPool;
            var chars = _chars;

            if (chars.IsSingleSegment)
            {
                CsvCharsInput.ProcessFullSegment(chars.First.Span, encodeBatchCharCount, ignoreByteOrderMark, encodeBufferPool, tokenizer, visitor);
                return;
            }

            int encodeBufferLength = encoding.GetMaxByteCount(encodeBatchCharCount);
            Span<byte> encodeBuffer = stackalloc byte[0];
            IMemoryOwner<byte> encodeBufferOwner = null;
            if (encodeBufferLength < 1024)
            {
                encodeBuffer = stackalloc byte[encodeBufferLength];
            }
            else if (encodeBufferPool is null)
            {
                encodeBuffer = new byte[encodeBufferLength];
            }
            else
            {
                encodeBufferOwner = encodeBufferPool.Rent(encodeBufferLength);
                encodeBuffer = encodeBufferOwner.Memory.Span;
            }

            using (encodeBufferOwner)
            {
                var enumerator = chars.GetEnumerator();
                if (ignoreByteOrderMark && EatBOM(tokenizer, visitor, encodeBatchCharCount, encodeBuffer, ref enumerator, encoder))
                {
                    return;
                }

                while (enumerator.MoveNext())
                {
                    var segment = enumerator.Current;
                    if (!segment.IsEmpty)
                    {
                        CsvCharsInput.ProcessSegment(tokenizer, visitor, segment.Span, encodeBuffer, encodeBatchCharCount, encoder, false);
                    }
                }

                CsvCharsInput.ProcessSegment(tokenizer, visitor, Array.Empty<char>(), encodeBuffer, encodeBatchCharCount, encoder, true);
                tokenizer.ProcessEndOfStream(visitor);
            }
        }

        private static bool EatBOM(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor, int encodeBatchCharCount, Span<byte> encodeBuffer, ref ReadOnlySequence<char>.Enumerator enumerator, Encoder encoder)
        {
            while (enumerator.MoveNext())
            {
                var segment = enumerator.Current;
                if (segment.IsEmpty)
                {
                    continue;
                }

                var span = segment.Span;
                if (span[0] == '\uFEFF')
                {
                    span = span.Slice(1);
                }

                if (!span.IsEmpty)
                {
                    CsvCharsInput.ProcessSegment(tokenizer, visitor, span, encodeBuffer, encodeBatchCharCount, encoder, false);
                }

                return false;
            }

            tokenizer.ProcessEndOfStream(visitor);
            return true;
        }
    }
}
