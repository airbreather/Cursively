using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
        public virtual IEnumerable<IEnumerable<string>> AsEnumerableFromUTF8(UTF8FieldDecodingParameters parameters)
        {
            if (parameters is null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            var th = new Thread(obj =>
            {
                using (var parameter = (UTF8EventVisitor)obj)
                {
                    Process(parameter);
                    parameter.VisitEndOfInput();
                }
            });
            th.IsBackground = true;

#pragma warning disable CA2000 // Dispose objects before losing scope
            var vis = new UTF8EventVisitor(parameters);
#pragma warning restore CA2000 // Dispose objects before losing scope
            try
            {
                th.Start(vis);
                return vis.Records.Select(Observable.ToEnumerable).ToEnumerable();
            }
            catch
            {
                vis.Dispose();
                throw;
            }
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

        private sealed class UTF8EventVisitor : CsvReaderVisitorBase, IDisposable
        {
            private UTF8FieldDecoder _decoder;

#pragma warning disable CA2213 // Disposable fields should be disposed
            private ReplaySubject<string> _currentRecord;
#pragma warning restore CA2213 // Disposable fields should be disposed

            public UTF8EventVisitor(UTF8FieldDecodingParameters parameters)
            {
                _decoder = new UTF8FieldDecoder(parameters);
            }

            public ReplaySubject<IObservable<string>> Records { get; } = new ReplaySubject<IObservable<string>>();

            public override void VisitPartialFieldContents(ReadOnlySpan<byte> chunk)
            {
                if (_currentRecord is null)
                {
                    Records.OnNext(_currentRecord = new ReplaySubject<string>());
                }

                if (!_decoder.TryAppendPartial(chunk))
                {
                    _currentRecord.OnError(new Exception());
                }
            }

            public override void VisitEndOfField(ReadOnlySpan<byte> chunk)
            {
                if (_currentRecord is null)
                {
                    Records.OnNext(_currentRecord = new ReplaySubject<string>());
                }

                if (!_decoder.TryFinish(chunk, out var field))
                {
                    _currentRecord.OnError(new Exception());
                }

                _currentRecord.OnNext(field.ToString());
            }

            public override void VisitEndOfRecord()
            {
                _currentRecord.OnCompleted();
                _currentRecord = null;
            }

            public void VisitEndOfInput()
            {
                Records.OnCompleted();
            }

            public void Dispose()
            {
            }
        }
    }
}
