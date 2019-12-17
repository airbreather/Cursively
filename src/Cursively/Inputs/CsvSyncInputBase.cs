using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
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
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IEnumerable<string>> AsEnumerableFromUTF8()
        {
            return AsEnumerableFromUTF8(UTF8FieldDecodingParameters.Default);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public IEnumerable<IEnumerable<string>> AsEnumerableFromUTF8(UTF8FieldDecodingParameters parameters)
        {
            if (parameters is null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            return AsEnumerableFromUTF8Core(parameters);
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
        /// 
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        protected virtual IEnumerable<IEnumerable<string>> AsEnumerableFromUTF8Core(UTF8FieldDecodingParameters parameters)
        {
            var th = new Thread(obj =>
            {
                var innerVisitor = (EnumerableStateMachineVisitor)obj;

                // our state machine is **very** sensitive to being called incorrectly
                var visitor = new ValidatingCsvReaderVisitorWrapper(innerVisitor);

                // all exceptions get marshaled to the thread that's running the loop.
#pragma warning disable CA1031 // Do not catch general exception types
                try
                {
                    Process(visitor);
                    innerVisitor.StateMachine.EndOfFile();
                }
                catch (Exception ex)
                {
                    innerVisitor.StateMachine.Error(ExceptionDispatchInfo.Capture(ex));
                }
#pragma warning restore CA1031 // Do not catch general exception types
            });

            th.IsBackground = true;
            var vis = new EnumerableStateMachineVisitor(parameters);
            th.Start(vis);
            return vis.StateMachine;
        }

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

        private sealed class EnumerableStateMachineVisitor : CsvReaderVisitorBase
        {
            private readonly UTF8FieldDecoder _decoder;

            public EnumerableStateMachineVisitor(UTF8FieldDecodingParameters parameters)
            {
                _decoder = new UTF8FieldDecoder(parameters);
            }

            public SyncEnumerableStateMachine StateMachine { get; } = new SyncEnumerableStateMachine();

            public override void VisitPartialFieldContents(ReadOnlySpan<byte> chunk)
            {
                if (!_decoder.TryAppendPartial(chunk))
                {
                    StateMachine.Error(ExceptionDispatchInfo.Capture(new CursivelyFieldIsTooLongException(_decoder.MaxFieldLength)));
                }
            }

            public override void VisitEndOfField(ReadOnlySpan<byte> chunk)
            {
                if (_decoder.TryFinish(chunk, out var chars))
                {
                    StateMachine.Receive(chars.ToString());
                    StateMachine.InputConsumedWaiter.WaitOne();
                }
                else
                {
                    StateMachine.Error(ExceptionDispatchInfo.Capture(new CursivelyFieldIsTooLongException(_decoder.MaxFieldLength)));
                }
            }

            public override void VisitEndOfRecord()
            {
                StateMachine.EndOfRecord();
            }
        }
    }
}
