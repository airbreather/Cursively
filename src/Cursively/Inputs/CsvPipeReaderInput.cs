using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Cursively.Inputs
{
    /// <summary>
    /// Implementation of <see cref="CsvAsyncInputBase"/> backed by a <see cref="PipeReader"/>.
    /// </summary>
    public sealed class CsvPipeReaderInput : CsvAsyncInputBase
    {
        private readonly byte _delimiter;

        private readonly PipeReader _reader;

        private readonly bool _ignoreUTF8ByteOrderMark;

        internal CsvPipeReaderInput(byte delimiter, PipeReader reader, bool ignoreUTF8ByteOrderMark)
        {
            _delimiter = delimiter;
            _reader = reader;
            _ignoreUTF8ByteOrderMark = ignoreUTF8ByteOrderMark;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="CsvPipeReaderInput"/> class as a copy of this
        /// one, with the given delimiter.
        /// </summary>
        /// <param name="delimiter">
        /// The delimiter to use.  Use <see cref="CsvTokenizer.IsValidDelimiter"/> to test whether
        /// or not a particular value is valid.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="CsvPipeReaderInput"/> class as a copy of this one, with
        /// the given delimiter.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="delimiter"/> is one of the illegal values.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="CsvAsyncInputBase.ProcessAsync"/> has already been called.
        /// </exception>
        public CsvPipeReaderInput WithDelimiter(byte delimiter)
        {
            if (!CsvTokenizer.IsValidDelimiter(delimiter))
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new ArgumentException("Must not be a carriage return, linefeed, or double-quote.", nameof(delimiter));
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            ThrowIfProcessingHasAlreadyStarted();
            return new CsvPipeReaderInput(delimiter, _reader, _ignoreUTF8ByteOrderMark);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="CsvPipeReaderInput"/> class as a copy of this
        /// one, with the given flag indicating whether or not a leading UTF-8 byte order mark, if
        /// present, should be omitted from the first field's data.
        /// </summary>
        /// <param name="ignoreUTF8ByteOrderMark">
        /// A value indicating whether or not a leading UTF-8 byte order mark, if present, should be
        /// omitted from the first field's data.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="CsvPipeReaderInput"/> class as a copy of this one, with
        /// the given flag indicating whether or not a leading UTF-8 byte order mark, if present,
        /// should be omitted from the first field's data.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="CsvAsyncInputBase.ProcessAsync"/> has already been called.
        /// </exception>
        public CsvPipeReaderInput WithIgnoreUTF8ByteOrderMark(bool ignoreUTF8ByteOrderMark)
        {
            ThrowIfProcessingHasAlreadyStarted();
            return new CsvPipeReaderInput(_delimiter, _reader, ignoreUTF8ByteOrderMark);
        }

        /// <inheritdoc />
        protected override async ValueTask ProcessAsyncCore(CsvReaderVisitorBase visitor, IProgress<int> progress, CancellationToken cancellationToken)
        {
            var tokenizer = new CsvTokenizer(_delimiter);
            var reader = _reader;

            if (_ignoreUTF8ByteOrderMark && await EatUTF8BOMAsync(tokenizer, visitor, progress, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            while (true)
            {
                var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);

                if (result.IsCanceled)
                {
                    throw new OperationCanceledException(cancellationToken);
                }

                var buffer = result.Buffer;
                foreach (var segment in buffer)
                {
                    tokenizer.ProcessNextChunk(segment.Span, visitor);
                }

                reader.AdvanceTo(buffer.End);
                if (progress != null)
                {
                    long totalLength = buffer.Length;
                    while (totalLength > int.MaxValue)
                    {
                        progress.Report(int.MaxValue);
                        totalLength -= int.MaxValue;
                    }

                    if (totalLength != 0)
                    {
                        progress.Report(unchecked((int)totalLength));
                    }
                }

                if (result.IsCompleted)
                {
                    break;
                }
            }

            tokenizer.ProcessEndOfStream(visitor);
            progress?.Report(0);
        }

        private async ValueTask<bool> EatUTF8BOMAsync(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor, IProgress<int> progress, CancellationToken cancellationToken)
        {
            var reader = _reader;

            ReadOnlySequence<byte> buffer;

            // keep asking for more until we've seen either 3+ bytes or the end of the data.
            while (true)
            {
                var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);

                if (result.IsCanceled)
                {
                    throw new OperationCanceledException(cancellationToken);
                }

                buffer = result.Buffer;
                if (buffer.Length >= 3)
                {
                    // we've seen 3+ bytes.
                    break;
                }

                if (result.IsCompleted)
                {
                    // we've seen the end of the data.
                    Finish();
                    tokenizer.ProcessEndOfStream(visitor);
                    reader.AdvanceTo(buffer.End);
                    progress?.Report(0);
                    return true;
                }

                // tell the reader that we've looked at everything it had to give us, and we weren't
                // able to consume any of it, so the next read should have everything we've seen so
                // far, plus at least one more byte.
                reader.AdvanceTo(buffer.Start, buffer.End);
            }

            Finish();
            return false;

            void Finish()
            {
                Span<byte> upToFirstThreeBytes = stackalloc byte[3];
                int alreadyEaten = 0;
                foreach (var segment in buffer)
                {
                    int lengthToCopy = 3 - alreadyEaten;
                    if (lengthToCopy > segment.Length)
                    {
                        lengthToCopy = segment.Length;
                    }

                    segment.Slice(0, lengthToCopy).Span.CopyTo(upToFirstThreeBytes.Slice(alreadyEaten, lengthToCopy));
                    alreadyEaten += lengthToCopy;
                    if (alreadyEaten == 3)
                    {
                        break;
                    }
                }

                upToFirstThreeBytes = upToFirstThreeBytes.Slice(0, alreadyEaten);
                var head = new ReadOnlySpan<byte>(UTF8BOM, 0, alreadyEaten);
                if (!upToFirstThreeBytes.SequenceEqual(head))
                {
                    tokenizer.ProcessNextChunk(upToFirstThreeBytes, visitor);
                }

                reader.AdvanceTo(buffer.GetPosition(alreadyEaten));
                progress?.Report(alreadyEaten);
            }
        }
    }
}
