using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Voron.Graph.Algorithms
{
    public abstract class BaseAlgorithm
    {
        public AlgorithmState State { get; private set; }

        protected readonly object _syncObject = new object();
        protected readonly CancellationToken _cancelToken;

        public BaseAlgorithm(CancellationToken cancelToken)
        {
            _cancelToken = cancelToken;
            State = AlgorithmState.NotStarted;
        }

        public event Action<AlgorithmState> StateChanged;
        protected virtual void OnStateChange(AlgorithmState state)
        {
            var stateChanged = this.StateChanged;
            if (stateChanged != null)
                stateChanged(state);
        }

        protected void SetState(AlgorithmState newState)
        {
            using (Lock())
            {
                State = newState;
                OnStateChange(newState);
            }
        }

        protected IDisposable Lock(bool isWriteLock = false, int timeout = 10000)
        {
            bool lockTaken = false;
            Monitor.TryEnter(_syncObject, timeout, ref lockTaken);
            if (!lockTaken)
                throw new TimeoutException("failed to acquire lock");

            return new DisposableAction(() => Monitor.Exit(_syncObject));            
        }

    }
}
