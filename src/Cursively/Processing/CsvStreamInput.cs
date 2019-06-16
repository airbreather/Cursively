using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Cursively.Processing
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CsvStreamInput : CsvAsyncInput
    {
        private readonly Stream _stream;

        private readonly int _bufferSize;

        internal CsvStreamInput(byte delimiter, Stream stream, int bufferSize)
            : base(delimiter, true)
        {
            _stream = stream;
            _bufferSize = bufferSize;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public CsvStreamInput WithDelimiter(byte delimiter) =>
            new CsvStreamInput(delimiter, _stream, _bufferSize);

        /// <inheritdoc />
        protected override void Process(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor)
        {
            var stream = _stream;

            byte[] buffer = new byte[_bufferSize];
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
            if (!_stream.CanSeek)
            {
                return false;
            }

            _stream.Seek(0, SeekOrigin.Begin);
            return true;
        }
    }
}
