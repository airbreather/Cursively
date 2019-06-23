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
            : base(delimiter, requiresExplicitReset: false)
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
                try
                {
                    foreach (var segment in buffer)
                    {
                        if (!segment.IsEmpty)
                        {
                            tokenizer.ProcessNextChunk(segment.Span, visitor);
                            progress?.Report(segment.Length);
                        }
                    }

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
                finally
                {
                    reader.AdvanceTo(buffer.End);
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
                    foreach (var segment in buffer)
                    {
                        if (!segment.IsEmpty)
                        {
                            tokenizer.ProcessNextChunk(segment.Span, visitor);
                            progress?.Report(segment.Length);
                        }
                    }

                    tokenizer.ProcessEndOfStream(visitor);
                    progress?.Report(0);
                    return true;
                }

                reader.AdvanceTo(buffer.Start, buffer.End);
            }

            Finish();
            progress?.Report(3);
            return false;

            void Finish()
            {
                Span<byte> firstThree = stackalloc byte[3];
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

                    segment.Slice(0, lengthToCopy).Span.CopyTo(firstThree.Slice(alreadyEaten, lengthToCopy));
                    alreadyEaten += lengthToCopy;
                    if (alreadyEaten == 3)
                    {
                        break;
                    }
                }

                if (!(firstThree[0] == 0xEF &&
                      firstThree[1] == 0xBB &&
                      firstThree[2] == 0xBF))
                {
                    tokenizer.ProcessNextChunk(firstThree, visitor);
                }

                reader.AdvanceTo(buffer.GetPosition(3));
            }
        }
    }
}
