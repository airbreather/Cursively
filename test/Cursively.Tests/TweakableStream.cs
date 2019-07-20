using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Cursively.Tests
{
    internal sealed class TweakableStream : Stream
    {
        private readonly Stream _inner;

        private bool? _canRead;

        private bool? _canSeek;

        private bool? _canWrite;

        private bool? _canTimeout;

        public TweakableStream(Stream inner) => _inner = inner;

        public override bool CanRead => _canRead ?? _inner.CanRead;
        public override bool CanSeek => _canSeek ?? _inner.CanSeek;
        public override bool CanTimeout => _canTimeout ?? _inner.CanTimeout;
        public override bool CanWrite => _canWrite ?? _inner.CanWrite;
        public override long Length => _inner.Length;
        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override int ReadTimeout
        {
            get => _inner.ReadTimeout;
            set => _inner.ReadTimeout = value;
        }

        public override int WriteTimeout
        {
            get => _inner.WriteTimeout;
            set => _inner.WriteTimeout = value;
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => _inner.BeginRead(buffer, offset, count, callback, state);
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => _inner.BeginWrite(buffer, offset, count, callback, state);
        public override void Close() => _inner.Close();
        public override void CopyTo(Stream destination, int bufferSize) => _inner.CopyTo(destination, bufferSize);
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => _inner.CopyToAsync(destination, bufferSize, cancellationToken);
        public override int EndRead(IAsyncResult asyncResult) => _inner.EndRead(asyncResult);
        public override void EndWrite(IAsyncResult asyncResult) => _inner.EndWrite(asyncResult);
        public override void Flush() => _inner.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);
        public override object InitializeLifetimeService() => _inner.InitializeLifetimeService();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override int Read(Span<byte> buffer) => _inner.Read(buffer);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _inner.ReadAsync(buffer, offset, count, cancellationToken);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => _inner.ReadAsync(buffer, cancellationToken);
        public override int ReadByte() => _inner.ReadByte();
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);
        public override void Write(ReadOnlySpan<byte> buffer) => _inner.Write(buffer);
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _inner.WriteAsync(buffer, offset, count, cancellationToken);
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => _inner.WriteAsync(buffer, cancellationToken);
        public override void WriteByte(byte value) => _inner.WriteByte(value);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }

        internal void SetCanRead(bool value) => _canRead = value;

        internal void SetCanSeek(bool value) => _canSeek = value;

        internal void SetCanWrite(bool value) => _canWrite = value;

        internal void SetCanTimeout(bool value) => _canTimeout = value;
    }
}
