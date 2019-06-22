using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Cursively.Inputs
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CsvStreamInput : CsvAsyncInput
    {
        private readonly Stream _stream;

        private readonly int _bufferSize;

        private readonly long _pos;

        private readonly bool _ignoreUTF8ByteOrderMark;

        internal CsvStreamInput(byte delimiter, Stream stream, int bufferSize, bool ignoreUTF8ByteOrderMark)
            : base(delimiter, true)
        {
            _stream = stream;
            _bufferSize = bufferSize;
            _pos = stream.CanSeek ? stream.Position : -1;
            _ignoreUTF8ByteOrderMark = ignoreUTF8ByteOrderMark;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public CsvStreamInput WithDelimiter(byte delimiter) =>
            new CsvStreamInput(delimiter, _stream, _bufferSize, _ignoreUTF8ByteOrderMark);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ignoreUTF8ByteOrderMark"></param>
        /// <returns></returns>
        public CsvStreamInput WithIgnoreUTF8ByteOrderMark(bool ignoreUTF8ByteOrderMark) =>
            new CsvStreamInput(Delimiter, _stream, _bufferSize, ignoreUTF8ByteOrderMark);

        /// <inheritdoc />
        protected override void Process(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor)
        {
            var stream = _stream;

            byte[] buffer = new byte[_bufferSize];
            if (_ignoreUTF8ByteOrderMark && EatUTF8BOM(tokenizer, visitor, stream, buffer))
            {
                return;
            }

            int cnt;
            while ((cnt = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(buffer, 0, cnt), visitor);
            }

            tokenizer.ProcessEndOfStream(visitor);
        }

        /// <inheritdoc />
        protected override async ValueTask ProcessAsync(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor, IProgress<int> progress, CancellationToken cancellationToken)
        {
            var stream = _stream;

            byte[] buffer = new byte[_bufferSize];
            if (_ignoreUTF8ByteOrderMark && await EatUTF8BOMAsync(tokenizer, visitor, stream, buffer, progress, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            int cnt;
            while ((cnt = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(buffer, 0, cnt), visitor);
                progress?.Report(cnt);

                // not all streams support cancellation, so we might as well do this ourselves.  it
                // does involve a volatile read, so don't go overboard.
                cancellationToken.ThrowIfCancellationRequested();
            }

            tokenizer.ProcessEndOfStream(visitor);
            progress?.Report(0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override bool TryResetCore()
        {
            if (_pos < 0)
            {
                return false;
            }

            _stream.Seek(_pos, SeekOrigin.Begin);
            return true;
        }

        private static bool EatUTF8BOM(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor, Stream stream, byte[] buffer)
        {
            if (buffer.Length < 3)
            {
                buffer = new byte[3];
            }

            int off = 0;
            while (true)
            {
                int cnt = stream.Read(buffer, off, buffer.Length - off);
                if (cnt == 0)
                {
                    if (off != 0)
                    {
                        tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(buffer, 0, off), visitor);
                    }

                    tokenizer.ProcessEndOfStream(visitor);
                    return true;
                }

                off += cnt;
                if (off >= 3)
                {
                    break;
                }
            }

            int len = off;
            off = 0;
            if (buffer[0] == 0xEF &&
                buffer[1] == 0xBB &&
                buffer[2] == 0xBF)
            {
                off = 3;
                len -= 3;
            }

            if (len != 0)
            {
                tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(buffer, off, len), visitor);
            }

            return false;
        }

        private static async ValueTask<bool> EatUTF8BOMAsync(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor, Stream stream, byte[] buffer, IProgress<int> progress, CancellationToken cancellationToken)
        {
            if (buffer.Length < 3)
            {
                buffer = new byte[3];
            }

            int off = 0;
            while (true)
            {
                int cnt = await stream.ReadAsync(buffer, off, buffer.Length - off, cancellationToken).ConfigureAwait(false);
                if (cnt == 0)
                {
                    if (off != 0)
                    {
                        tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(buffer, 0, off), visitor);
                        progress?.Report(off);
                    }

                    tokenizer.ProcessEndOfStream(visitor);
                    return true;
                }

                off += cnt;
                if (off >= 3)
                {
                    break;
                }
            }

            int len = off;
            int rptLen = len;
            off = 0;
            if (buffer[0] == 0xEF &&
                buffer[1] == 0xBB &&
                buffer[2] == 0xBF)
            {
                off = 3;
                len -= 3;
            }

            if (len != 0)
            {
                tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(buffer, off, len), visitor);
            }

            progress?.Report(rptLen);
            return false;
        }
    }
}
