using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Cursively
{
    internal unsafe struct UTF8FieldDecoder
    {
        private readonly Decoder _decoder;

        private char[] _buf;

        private int _bufUsed;

        public UTF8FieldDecoder(UTF8FieldDecodingParameters parameters)
        {
            _decoder = parameters.CreateUTF8Decoder();
            MaxFieldLength = parameters.MaxFieldLength;
            _buf = Array.Empty<char>();
            _bufUsed = 0;
        }

        public int MaxFieldLength { get; }

        public void SetToNull()
            => this = default;

        public unsafe bool TryFinish(ReadOnlySpan<byte> chunk, out ReadOnlySpan<char> final)
        {
            fixed (byte* b = &MemoryMarshal.GetReference(chunk))
            {
                if (!VisitHeaderChunk(b, chunk.Length, true))
                {
                    final = default;
                    return false;
                }
            }

            final = new ReadOnlySpan<char>(_buf, 0, _bufUsed);
            _bufUsed = 0;
            return true;
        }

        public unsafe bool TryAppendPartial(ReadOnlySpan<byte> chunk)
        {
            fixed (byte* b = &MemoryMarshal.GetReference(chunk))
            {
                return VisitHeaderChunk(b, chunk.Length, false);
            }
        }

        private unsafe bool VisitHeaderChunk(byte* b, int byteCount, bool flush)
        {
            // Decoder methods require non-null pointers, even if the lengths are zero.  See
            // dotnet/corefx#32861 for some discussion about the issue.  When it starts making sense
            // to target netstandard2.1, then we can stop with all the pointer stuff and just use
            // spans directly.  FWIW, it seems counter-intuitive, but it's actually correct to call
            // this method unconditionally even if byteCount happens to be 0:
            // - the tokenizer never calls VisitPartial* with an empty span, so checking before the
            //   method call in those cases would only benefit external callers of VisitPartial*.
            // - from VisitEnd*, we need to tell the Decoder that the last chunk we sent it was
            //   actually the end of what we had so that it can trigger the fallback logic if a
            //   sequence started off as valid UTF-8 but was terminated abruptly.
            void* garbageNonNullPointer = (void*)0xDEADBEEF;

            if (byteCount == 0)
            {
                b = (byte*)garbageNonNullPointer;
            }

            int charCount = _decoder.GetCharCount(b, byteCount, flush);

            int neededLength = _bufUsed + charCount;
            int maxLength = MaxFieldLength;
            if (neededLength > maxLength)
            {
                return false;
            }

            EnsureHeaderBufferCapacity(neededLength);

            // at this point, _headerBufferConsumed is guaranteed to be an index in _headerBuffer...
            // ...unless charCount is 0, in which case it *might* point to one past the end (#16).
            if (charCount == 0)
            {
                _decoder.GetChars(b, byteCount, (char*)garbageNonNullPointer, 0, flush);
            }
            else
            {
                fixed (char* c = &_buf[_bufUsed])
                {
                    _decoder.GetChars(b, byteCount, c, charCount, flush);
                }

                _bufUsed += charCount;
            }

            return true;
        }

        private void EnsureHeaderBufferCapacity(int neededLength)
        {
            if (neededLength > _buf.Length)
            {
                int maxLength = MaxFieldLength;
                int newLength = _buf.Length;
                if (newLength < 8)
                {
                    newLength = 8;
                }

                while (newLength < neededLength)
                {
                    // double it until we reach the max length
                    newLength = maxLength - newLength > newLength
                        ? newLength + newLength
                        : maxLength;
                }

                Array.Resize(ref _buf, newLength);
            }
        }
    }
}
