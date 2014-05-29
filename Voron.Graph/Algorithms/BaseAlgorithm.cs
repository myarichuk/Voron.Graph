using System;
using System.Diagnostics;
using System.Threading;

namespace Voron.Graph.Algorithms
{
    public abstract class BaseAlgorithm
    {
        protected readonly object _syncObject = new object();
        private int _state;

        protected AlgorithmState State
        {
            get
            {
                Debug.Assert(Enum.IsDefined(typeof(AlgorithmState), _state));
                return (AlgorithmState)_state;
            }
        }

        public event Action Finished;

        public event Action<AlgorithmState> StateChanged;
        protected virtual void OnStateChange(AlgorithmState state)
        {
            if(state == AlgorithmState.Finished ||
               state == AlgorithmState.Aborted)
                OnFinished();

            Interlocked.CompareExchange(ref _state, (int)state, _state);

            var stateChanged = StateChanged;

            if (stateChanged != null)
                stateChanged(state);
        }

        protected void OnFinished()
        {
            var finished = Finished;
            if (finished != null)
                finished();
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
