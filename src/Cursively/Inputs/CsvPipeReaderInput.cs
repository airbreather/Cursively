using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Cursively.Inputs
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CsvPipeReaderInput : CsvAsyncInput
    {
        private readonly PipeReader _reader;

        private readonly bool _ignoreUTF8ByteOrderMark;

        internal CsvPipeReaderInput(byte delimiter, PipeReader reader, bool ignoreUTF8ByteOrderMark)
            : base(delimiter, requiresExplicitReset: true)
        {
            _reader = reader;
            _ignoreUTF8ByteOrderMark = ignoreUTF8ByteOrderMark;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public CsvPipeReaderInput WithDelimiter(byte delimiter) =>
            new CsvPipeReaderInput(delimiter, _reader, _ignoreUTF8ByteOrderMark);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ignoreUTF8ByteOrderMark"></param>
        /// <returns></returns>
        public CsvPipeReaderInput WithIgnoreUTF8ByteOrderMark(bool ignoreUTF8ByteOrderMark) =>
            new CsvPipeReaderInput(Delimiter, _reader, ignoreUTF8ByteOrderMark);

        /// <inheritdoc />
        protected override void Process(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor)
        {
            ProcessAsync(tokenizer, visitor, null, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        protected override async ValueTask ProcessAsync(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor, IProgress<int> progress, CancellationToken cancellationToken)
        {
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
                long totalLength = 0;
                foreach (var segment in buffer)
                {
                    if (!segment.IsEmpty)
                    {
                        tokenizer.ProcessNextChunk(segment.Span, visitor);
                        totalLength += segment.Length;
                    }
                }

                reader.AdvanceTo(buffer.End);
                if (progress != null)
                {
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
                    if (segment.IsEmpty)
                    {
                        continue;
                    }

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
