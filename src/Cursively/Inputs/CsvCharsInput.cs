using System;
using System.Buffers;
using System.Text;

namespace Cursively.Inputs
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CsvCharsInput : CsvInput
    {
        internal static readonly UTF8Encoding TheEncoding = new UTF8Encoding(false, false);

        private readonly ReadOnlyMemory<char> _chars;

        private readonly int _encodeBatchCharCount;

        private readonly bool _ignoreByteOrderMark;

        private readonly MemoryPool<byte> _encodeBufferPool;

        internal CsvCharsInput(byte delimiter, ReadOnlyMemory<char> chars, int encodeBatchCharCount, MemoryPool<byte> encodeBufferPool, bool ignoreByteOrderMark)
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
        public CsvCharsInput WithDelimiter(byte delimiter) =>
            new CsvCharsInput(delimiter, _chars, _encodeBatchCharCount, _encodeBufferPool, _ignoreByteOrderMark);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="encodeBatchCharCount"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public CsvCharsInput WithEncodeBatchCharCount(int encodeBatchCharCount)
        {
            if (encodeBatchCharCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(encodeBatchCharCount), encodeBatchCharCount, "Must be greater than zero.");
            }

            return new CsvCharsInput(Delimiter, _chars, encodeBatchCharCount, _encodeBufferPool, _ignoreByteOrderMark);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="encodeBufferPool"></param>
        /// <returns></returns>
        public CsvCharsInput WithEncodeBufferPool(MemoryPool<byte> encodeBufferPool) =>
            new CsvCharsInput(Delimiter, _chars, _encodeBatchCharCount, encodeBufferPool, _ignoreByteOrderMark);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ignoreByteOrderMark"></param>
        /// <returns></returns>
        public CsvCharsInput WithIgnoreByteOrderMark(bool ignoreByteOrderMark) =>
            new CsvCharsInput(Delimiter, _chars, _encodeBatchCharCount, _encodeBufferPool, ignoreByteOrderMark);

        /// <inheritdoc />
        protected override unsafe void Process(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor)
        {
            ProcessFullSegment(_chars.Span, _encodeBatchCharCount, _ignoreByteOrderMark, _encodeBufferPool, tokenizer, visitor);
        }

        internal static unsafe void ProcessFullSegment(ReadOnlySpan<char> chars, int encodeBatchCharCount, bool ignoreByteOrderMark, MemoryPool<byte> encodeBufferPool, CsvTokenizer tokenizer, CsvReaderVisitorBase visitor)
        {
            var encoding = TheEncoding;
            var encoder = encoding.GetEncoder();

            if (ignoreByteOrderMark && !chars.IsEmpty && chars[0] == '\uFEFF')
            {
                chars = chars.Slice(1);
            }

            if (chars.IsEmpty)
            {
                tokenizer.ProcessEndOfStream(visitor);
                return;
            }

            if (encodeBatchCharCount > chars.Length)
            {
                encodeBatchCharCount = chars.Length;
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
                ProcessSegment(tokenizer, visitor, chars, encodeBuffer, encodeBatchCharCount, encoder, true);
            }

            tokenizer.ProcessEndOfStream(visitor);
        }

        internal static unsafe void ProcessSegment(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor, ReadOnlySpan<char> chars, Span<byte> encodeBuffer, int encodeBatchCharCount, Encoder encoder, bool lastSegment)
        {
            int remainingCharCount = chars.Length;
            if (remainingCharCount == 0)
            {
                ProcessFinalEmptySegment(tokenizer, visitor, encodeBuffer, encoder);
                return;
            }

            fixed (byte* encodePtr = &encodeBuffer[0])
            fixed (char* decodePtrFixed = &chars[0])
            {
                char* decodePtr = decodePtrFixed;

                while (remainingCharCount > encodeBatchCharCount)
                {
                    int encodeByteCount = encoder.GetByteCount(decodePtr, encodeBatchCharCount, false);
                    encoder.GetBytes(decodePtr, encodeBatchCharCount, encodePtr, encodeByteCount, false);
                    tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(encodePtr, encodeByteCount), visitor);

                    remainingCharCount -= encodeBatchCharCount;
                    decodePtr += encodeBatchCharCount;
                }

                if (remainingCharCount > 0)
                {
                    int encodeByteCount = encoder.GetByteCount(decodePtr, remainingCharCount, lastSegment);
                    encoder.GetBytes(decodePtr, remainingCharCount, encodePtr, encodeByteCount, lastSegment);
                    tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(encodePtr, encodeByteCount), visitor);
                }
            }
        }

        internal static unsafe void ProcessFinalEmptySegment(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor, Span<byte> encodeBuffer, Encoder encoder)
        {
            fixed (byte* encodePtr = &encodeBuffer[0])
            {
                char c = '\0';
                int encodeByteCount = encoder.GetByteCount(&c, 0, true);
                encoder.GetBytes(&c, 0, encodePtr, encodeByteCount, true);
                tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(encodePtr, encodeByteCount), visitor);
            }
        }
    }
}
