using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
                    innerVisitor.VisitEndOfFile();
                }
                catch (Exception ex)
                {
                    innerVisitor.VisitError(ExceptionDispatchInfo.Capture(ex));
                }
#pragma warning restore CA1031 // Do not catch general exception types
            });

            th.IsBackground = true;
            var vis = new EnumerableStateMachineVisitor(parameters);
            th.Start(vis);
            return vis.Enumerable;
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

        [SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable")]
        private sealed class EnumerableStateMachineVisitor : CsvReaderVisitorBase
        {
            private readonly UTF8FieldDecoder _decoder;

            private readonly BlockingCollection<IEnumerable<string>> _outer = new BlockingCollection<IEnumerable<string>>();

            private BlockingCollection<string> _inner = new BlockingCollection<string>();

            private bool _firstInRecord = true;

            private ExceptionDispatchInfo _exceptionInfo;

            public EnumerableStateMachineVisitor(UTF8FieldDecodingParameters parameters)
            {
                _decoder = new UTF8FieldDecoder(parameters);
                Enumerable = EnumerateOuter();
            }

            public IEnumerable<IEnumerable<string>> Enumerable { get; }

            public override void VisitPartialFieldContents(ReadOnlySpan<byte> chunk)
            {
                if (_decoder.TryAppendPartial(chunk))
                {
                    EnsureRecord();
                }
                else
                {
                    FieldTooLong();
                }
            }

            public override void VisitEndOfField(ReadOnlySpan<byte> chunk)
            {
                if (_decoder.TryFinish(chunk, out var chars))
                {
                    EnsureRecord();
                    _inner.Add(chars.ToString());
                }
                else
                {
                    FieldTooLong();
                }
            }

            public override void VisitEndOfRecord()
            {
                _inner.CompleteAdding();
                _inner = new BlockingCollection<string>();
                _firstInRecord = true;
            }

            public void VisitEndOfFile()
            {
                _inner.CompleteAdding();
                _outer.CompleteAdding();
            }

            public void VisitError(ExceptionDispatchInfo exceptionInfo)
            {
                _exceptionInfo = exceptionInfo;
                VisitEndOfFile();
            }

            private void EnsureRecord()
            {
                if (_firstInRecord)
                {
                    _outer.Add(EnumerateInner());
                    _firstInRecord = false;
                }
            }

            private void FieldTooLong()
            {
                VisitError(ExceptionDispatchInfo.Capture(new CursivelyFieldIsTooLongException(_decoder.MaxFieldLength)));
            }

            private IEnumerable<IEnumerable<string>> EnumerateOuter()
            {
                foreach (var s in _outer.GetConsumingEnumerable())
                {
                    yield return s;
                }

                _exceptionInfo?.Throw();
            }

            private IEnumerable<string> EnumerateInner()
            {
                foreach (string s in _inner.GetConsumingEnumerable())
                {
                    yield return s;
                }

                _exceptionInfo?.Throw();
            }
        }
    }
}
