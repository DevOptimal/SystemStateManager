﻿namespace MachineStateManager
{
    internal class Caretaker<TOriginator, TMemento> : IDisposable
        where TOriginator : IOriginator<TMemento>
        where TMemento : IMemento
    {
        public TOriginator Originator { get; }

        public TMemento Memento { get; }

        private bool disposedValue;

        public Caretaker(TOriginator originator)
        {
            Originator = originator ?? throw new ArgumentNullException(nameof(originator));
            Memento = originator.GetState();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                Originator.SetState(Memento);

                disposedValue = true;
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~Caretaker()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
