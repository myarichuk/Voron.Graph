using System;

namespace Voron.Graph
{
    public class DisposableAction : IDisposable
    {
        private readonly Action _onDisposeAction;

        public DisposableAction(Action onDisposeAction)
        {
            _onDisposeAction = onDisposeAction;
        }

        public void Dispose()
        {
            if (_onDisposeAction != null)
                _onDisposeAction();
        }
    }
}
