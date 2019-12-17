using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Cursively.Inputs
{
    [SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable")]
    internal sealed class SyncEnumerableStateMachine : IEnumerable<IEnumerable<string>>
    {
        private readonly AutoResetEvent _waiter = new AutoResetEvent(false);

        private OuterEnumerableState _state;

        private InnerEnumerableStateMachine _currentRecord;

        private ExceptionDispatchInfo _errorInfo;

        private enum OuterEnumerableState
        {
            BeforeFirstFieldOfRecord,
            InRecord,
            Done,
            Error,
        }

        public AutoResetEvent InputConsumedWaiter { get; } = new AutoResetEvent(false);

        public IEnumerator<IEnumerable<string>> GetEnumerator()
        {
            using (_waiter)
            using (InputConsumedWaiter)
            {
                while (true)
                {
                    _waiter.WaitOne();
                    if (_state > OuterEnumerableState.InRecord)
                    {
                        _errorInfo?.Throw();
                        break;
                    }

                    yield return _currentRecord;
                }
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            => GetEnumerator();

        public void Receive(string field)
        {
            switch (_state)
            {
                case OuterEnumerableState.BeforeFirstFieldOfRecord:
                    _currentRecord = new InnerEnumerableStateMachine(InputConsumedWaiter);
                    _state = OuterEnumerableState.InRecord;
                    _waiter.Set();
                    goto case OuterEnumerableState.InRecord;

                case OuterEnumerableState.InRecord:
                    _currentRecord.Receive(field);
                    break;
            }
        }

        public void EndOfRecord()
        {
            if (_state != OuterEnumerableState.InRecord)
            {
                return;
            }

            _state = OuterEnumerableState.BeforeFirstFieldOfRecord;
            _currentRecord.EndOfRecord();
        }

        public void EndOfFile()
        {
            if (_state != OuterEnumerableState.BeforeFirstFieldOfRecord)
            {
                return;
            }

            _state = OuterEnumerableState.Done;
            _waiter.Set();
        }

        public void Error(ExceptionDispatchInfo errorInfo)
        {
            _errorInfo = errorInfo;
            _state = OuterEnumerableState.Error;
            _currentRecord.Error(errorInfo);
            _waiter.Set();
        }

        [SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable")]
        private sealed class InnerEnumerableStateMachine : IEnumerable<string>
        {
            private readonly AutoResetEvent _waiter = new AutoResetEvent(false);

            private readonly AutoResetEvent _inputConsumedWaiter;

            private InnerEnumerableState _state;

            private ExceptionDispatchInfo _errorInfo;

            private string _currentField;

            public InnerEnumerableStateMachine(AutoResetEvent inputConsumedWaiter)
                => _inputConsumedWaiter = inputConsumedWaiter;

            private enum InnerEnumerableState
            {
                InRecord,
                Done,
                Error,
            }

            public IEnumerator<string> GetEnumerator()
            {
                using (_waiter)
                {
                    while (true)
                    {
                        _waiter.WaitOne();
                        if (_state > InnerEnumerableState.InRecord)
                        {
                            _errorInfo?.Throw();
                            break;
                        }

                        yield return _currentField;
                        _inputConsumedWaiter.Set();
                    }
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
                => GetEnumerator();

            public void Receive(string field)
            {
                if (_state != InnerEnumerableState.InRecord)
                {
                    return;
                }

                _currentField = field;
                _waiter.Set();
            }

            public void EndOfRecord()
            {
                if (_state != InnerEnumerableState.InRecord)
                {
                    return;
                }

                _state = InnerEnumerableState.Done;
                _waiter.Set();
            }

            public void Error(ExceptionDispatchInfo errorInfo)
            {
                _errorInfo = errorInfo;
                _state = InnerEnumerableState.Error;
                _waiter.Set();
            }
        }
    }
}
