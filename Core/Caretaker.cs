namespace MachineStateManager.Core
{
    internal class Caretaker<TOriginator, TMemento> : IDisposable
        where TOriginator : IOriginator<TMemento>
        where TMemento : IMemento
    {
        public TOriginator Originator { get; }

        public TMemento Memento { get; }

        private bool disposedValue;

        public Caretaker(TOriginator originator) : this(originator, originator.GetState())
        {
        }

        protected Caretaker(TOriginator originator, TMemento memento)
        {
            Originator = originator ?? throw new ArgumentNullException(nameof(originator));
            Memento = memento ?? throw new ArgumentNullException(nameof(memento));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Originator.SetState(Memento);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Caretaker()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
