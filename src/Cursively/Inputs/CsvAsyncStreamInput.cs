using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Cursively.Inputs
{
    /// <summary>
    /// Implementation of <see cref="CsvAsyncInputBase"/> backed by a <see cref="Stream"/>.
    /// </summary>
    public sealed class CsvAsyncStreamInput : CsvAsyncInputBase
    {
        private readonly byte _delimiter;

        private readonly Stream _csvStream;

        private readonly int _minReadBufferByteCount;

        private readonly ArrayPool<byte> _readBufferPool;

        private readonly bool _ignoreUTF8ByteOrderMark;

        internal CsvAsyncStreamInput(byte delimiter, Stream csvStream, int minReadBufferByteCount, ArrayPool<byte> readBufferPool, bool ignoreUTF8ByteOrderMark)
        {
            _delimiter = delimiter;
            _csvStream = csvStream;
            _minReadBufferByteCount = minReadBufferByteCount;
            _readBufferPool = readBufferPool;
            _ignoreUTF8ByteOrderMark = ignoreUTF8ByteOrderMark;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="CsvAsyncStreamInput"/> class as a copy of this
        /// one, with the given delimiter.
        /// </summary>
        /// <param name="delimiter">
        /// The delimiter to use.  Use <see cref="CsvTokenizer.IsValidDelimiter"/> to test whether
        /// or not a particular value is valid.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="CsvAsyncStreamInput"/> class as a copy of this one, with
        /// the given delimiter.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="delimiter"/> is one of the illegal values.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="CsvAsyncInputBase.ProcessAsync"/> has already been called.
        /// </exception>
        public CsvAsyncStreamInput WithDelimiter(byte delimiter)
        {
            if (!CsvTokenizer.IsValidDelimiter(delimiter))
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new ArgumentException("Must not be a carriage return, linefeed, or double-quote.", nameof(delimiter));
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            ThrowIfProcessingHasAlreadyStarted();
            return new CsvAsyncStreamInput(delimiter, _csvStream, _minReadBufferByteCount, _readBufferPool, _ignoreUTF8ByteOrderMark);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="CsvAsyncStreamInput"/> class as a copy of this
        /// one, reading in chunks of sizes that are at least the given number of bytes.
        /// </summary>
        /// <param name="minReadBufferByteCount">
        /// <para>
        /// The minimum size, in bytes, of chunks to read from the buffer.
        /// </para>
        /// <para>
        /// When using an <see cref="ArrayPool{T}"/>, this is the value that will be used for
        /// <see cref="ArrayPool{T}.Rent(int)"/>, so larger chunks should be expected.
        /// </para>
        /// <para>
        /// When not using an <see cref="ArrayPool{T}"/> (i.e., on instances configured by calling
        /// <see cref="WithReadBufferPool(ArrayPool{byte})"/> passing in <see langword="null"/>),
        /// this is the actual size of any arrays that will be allocated on the managed heap.
        /// </para>
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="CsvAsyncStreamInput"/> class as a copy of this one,
        /// using the given <see cref="ArrayPool{T}"/> to provide temporary buffers for the
        /// <see cref="Stream"/> to read into.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="minReadBufferByteCount"/> is not greater than zero.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="CsvAsyncInputBase.ProcessAsync"/> has already been called.
        /// </exception>
        public CsvAsyncStreamInput WithMinReadBufferByteCount(int minReadBufferByteCount)
        {
            if (minReadBufferByteCount < 1)
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new ArgumentOutOfRangeException(nameof(minReadBufferByteCount), minReadBufferByteCount, "Must be greater than zero.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            ThrowIfProcessingHasAlreadyStarted();
            return new CsvAsyncStreamInput(_delimiter, _csvStream, minReadBufferByteCount, _readBufferPool, _ignoreUTF8ByteOrderMark);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="CsvAsyncStreamInput"/> class as a copy of this
        /// one, using the given <see cref="ArrayPool{T}"/> to provide temporary buffers for the
        /// <see cref="Stream"/> to read into.
        /// </summary>
        /// <param name="readBufferPool">
        /// The <see cref="ArrayPool{T}"/> to provide temporary buffers for the <see cref="Stream"/>
        /// to read into, or <see langword="null"/> if the temporary buffers should be allocated
        /// directly on the managed heap.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="CsvAsyncStreamInput"/> class as a copy of this one,
        /// using the given <see cref="ArrayPool{T}"/> to provide temporary buffers for the
        /// <see cref="Stream"/> to read into.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="CsvAsyncInputBase.ProcessAsync"/> has already been called.
        /// </exception>
        public CsvAsyncStreamInput WithReadBufferPool(ArrayPool<byte> readBufferPool)
        {
            ThrowIfProcessingHasAlreadyStarted();
            return new CsvAsyncStreamInput(_delimiter, _csvStream, _minReadBufferByteCount, readBufferPool, _ignoreUTF8ByteOrderMark);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="CsvAsyncStreamInput"/> class as a copy of this
        /// one, with the given flag indicating whether or not a leading UTF-8 byte order mark, if
        /// present, should be omitted from the first field's data.
        /// </summary>
        /// <param name="ignoreUTF8ByteOrderMark">
        /// A value indicating whether or not a leading UTF-8 byte order mark, if present, should be
        /// omitted from the first field's data.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="CsvAsyncStreamInput"/> class as a copy of this one, with
        /// the given flag indicating whether or not a leading UTF-8 byte order mark, if present,
        /// should be omitted from the first field's data.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="CsvAsyncInputBase.ProcessAsync"/> has already been called.
        /// </exception>
        public CsvAsyncStreamInput WithIgnoreUTF8ByteOrderMark(bool ignoreUTF8ByteOrderMark)
        {
            ThrowIfProcessingHasAlreadyStarted();
            return new CsvAsyncStreamInput(_delimiter, _csvStream, _minReadBufferByteCount, _readBufferPool, ignoreUTF8ByteOrderMark);
        }

        /// <inheritdoc />
        protected override async ValueTask ProcessAsyncCore(CsvReaderVisitorBase visitor, IProgress<int> progress, CancellationToken cancellationToken)
        {
            // not all streams support cancellation, so we might as well do this ourselves.  it
            // does involve a volatile read, so don't go overboard.
            cancellationToken.ThrowIfCancellationRequested();

            var tokenizer = new CsvTokenizer(_delimiter);
            var csvStream = _csvStream;
            int minReadBufferByteCount = _minReadBufferByteCount;
            var readBufferPool = _readBufferPool;

            byte[] readBuffer;
            if (readBufferPool is null)
            {
                readBuffer = new byte[minReadBufferByteCount];
            }
            else
            {
                readBuffer = readBufferPool.Rent(minReadBufferByteCount);
            }

            try
            {
                if (_ignoreUTF8ByteOrderMark && await EatUTF8BOMAsync(tokenizer, visitor, csvStream, readBuffer, progress, cancellationToken).ConfigureAwait(false))
                {
                    return;
                }

                int cnt;
                while ((cnt = await csvStream.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
                {
                    // not all streams support cancellation, so we might as well do this ourselves.  it
                    // does involve a volatile read, so don't go overboard.
                    cancellationToken.ThrowIfCancellationRequested();

                    tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(readBuffer, 0, cnt), visitor);
                    progress?.Report(cnt);
                }
            }
            finally
            {
                readBufferPool?.Return(readBuffer, clearArray: true);
            }

            tokenizer.ProcessEndOfStream(visitor);
            progress?.Report(0);
        }

        private static async ValueTask<bool> EatUTF8BOMAsync(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor, Stream csvStream, byte[] readBuffer, IProgress<int> progress, CancellationToken cancellationToken)
        {
            if (readBuffer.Length < 3)
            {
                // don't bother pooling; nobody should really ever care.
                readBuffer = new byte[3];
            }

            int byteCount = 0;
            while (byteCount < 3)
            {
                int readLength = await csvStream.ReadAsync(readBuffer, byteCount, readBuffer.Length - byteCount, cancellationToken).ConfigureAwait(false);

                // not all streams support cancellation, so we might as well do this ourselves.  it
                // does involve a volatile read, so don't go overboard.
                cancellationToken.ThrowIfCancellationRequested();

                if (readLength == 0)
                {
                    if (byteCount != 0)
                    {
                        if (!new ReadOnlySpan<byte>(readBuffer, 0, byteCount).SequenceEqual(new ReadOnlySpan<byte>(UTF8BOM, 0, byteCount)))
                        {
                            tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(readBuffer, 0, byteCount), visitor);
                        }

                        progress?.Report(byteCount);
                    }

                    tokenizer.ProcessEndOfStream(visitor);
                    progress?.Report(0);
                    return true;
                }

                byteCount += readLength;
            }

            var buf = new Memory<byte>(readBuffer, 0, byteCount);
            if (buf.Span.StartsWith(UTF8BOM))
            {
                buf = buf.Slice(3);
            }

            tokenizer.ProcessNextChunk(buf.Span, visitor);
            progress?.Report(byteCount);

            return false;
        }
    }
}
