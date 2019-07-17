using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Cursively.Inputs
{
    /// <summary>
    /// Models a CSV source data stream that can be processed asynchronously.
    /// </summary>
    public abstract class CsvAsyncInputBase
    {
        private protected static readonly byte[] UTF8BOM = { 0xEF, 0xBB, 0xBF };

        private static readonly object ProcessingHasStartedSentinel = new object();

        private object _processingHasStarted;

        /// <summary>
        /// Describes the contents of this CSV data stream to a <see cref="CsvReaderVisitorBase"/>.
        /// </summary>
        /// <param name="visitor">
        /// The <see cref="CsvReaderVisitorBase"/> to describe this CSV data stream to.
        /// </param>
        /// <param name="progress">
        /// An optional <see cref="IProgress{T}"/> instance that will receive a report of the size
        /// of each chunk (in bytes) as processing finishes, followed by one more report with a zero
        /// when the last chunk in the stream has been processed.
        /// </param>
        /// <param name="cancellationToken">
        /// An optional <see cref="CancellationToken"/> that may be used to signal cancellation.
        /// </param>
        /// <returns>
        /// A <see cref="ValueTask"/> encapsulating the operation.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when this stream has already been processed.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown to acknowledge a canceled <paramref name="cancellationToken"/>.  Some subclasses
        /// may throw an instance of a subclass, such as <see cref="TaskCanceledException"/>.
        /// </exception>
        public async ValueTask ProcessAsync(CsvReaderVisitorBase visitor, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            if (!(Interlocked.CompareExchange(ref _processingHasStarted, ProcessingHasStartedSentinel, null) is null))
            {
                ThrowProcessingHasAlreadyStartedException();
            }

            await ProcessAsyncCore(visitor, progress, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Implements the inner logic for <see cref="ProcessAsync"/>.
        /// </summary>
        /// <param name="visitor">
        /// The <see cref="CsvReaderVisitorBase"/> to describe this CSV data stream to.
        /// </param>
        /// <param name="progress">
        /// An optional <see cref="IProgress{T}"/> instance that will receive a report of the size
        /// of each chunk (in bytes) as processing finishes, followed by one more report with a zero
        /// when the last chunk in the stream has been processed.
        /// </param>
        /// <param name="cancellationToken">
        /// An optional <see cref="CancellationToken"/> that may be used to signal cancellation.
        /// </param>
        /// <returns>
        /// A <see cref="ValueTask"/> encapsulating the operation.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when this stream has already been processed.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown to acknowledge a canceled <paramref name="cancellationToken"/>.  Some subclasses
        /// may throw an instance of a subclass, such as <see cref="TaskCanceledException"/>.
        /// </exception>
        protected abstract ValueTask ProcessAsyncCore(CsvReaderVisitorBase visitor, IProgress<int> progress, CancellationToken cancellationToken);

        /// <summary>
        /// Throws if <see cref="ProcessAsync"/> has already been called for this instance.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ThrowIfProcessingHasAlreadyStarted()
        {
            if (_processingHasStarted == ProcessingHasStartedSentinel)
            {
                ThrowProcessingHasAlreadyStartedException();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowProcessingHasAlreadyStartedException() =>
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            throw new InvalidOperationException("Processing has already been started.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
    }
}
