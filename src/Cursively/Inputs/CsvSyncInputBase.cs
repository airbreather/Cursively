using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Cursively.Inputs
{
    /// <summary>
    /// Models a CSV source data stream that can be processed synchronously.
    /// </summary>
    public abstract class CsvSyncInputBase
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
        /// <exception cref="InvalidOperationException">
        /// Thrown when this stream has already been processed.
        /// </exception>
        public void Process(CsvReaderVisitorBase visitor)
        {
            if (!(Interlocked.CompareExchange(ref _processingHasStarted, ProcessingHasStartedSentinel, null) is null))
            {
                ThrowProcessingHasAlreadyStartedException();
            }

            ProcessCore(visitor);
        }

        /// <summary>
        /// Implements the inner logic for <see cref="Process"/>.
        /// </summary>
        /// <param name="visitor">
        /// The <see cref="CsvReaderVisitorBase"/> to describe this CSV data stream to.
        /// </param>
        /// <remarks>
        /// The base class will call this method at most once per instance.
        /// </remarks>
        protected abstract void ProcessCore(CsvReaderVisitorBase visitor);

        /// <summary>
        /// Throws if <see cref="Process"/> has already been called for this instance.
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
            throw new InvalidOperationException("Processing has already been started.");
    }
}
