using System;

namespace bradselw.MachineStateManager
{
    internal class Caretaker<TOriginator, TMemento> : ICaretaker
        where TOriginator : IOriginator<TMemento>
        where TMemento : IMemento
    {
        public string ID { get; }

        public TOriginator Originator { get; }

        public TMemento Memento { get; }

        private bool disposedValue;

        public Caretaker(TOriginator originator) : this(originator.ID, originator)
        {
        }

        public Caretaker(string id, TOriginator originator) : this(id, originator, originator.GetState())
        {
        }

        protected Caretaker(string id, TOriginator originator, TMemento memento)
        {
            if (originator == null)
            {
                throw new ArgumentNullException(nameof(originator));
            }

            if (memento == null)
            {
                throw new ArgumentNullException(nameof(memento));
            }

            ID = id ?? throw new ArgumentNullException(nameof(id));
            Originator = originator;
            Memento = memento;
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
