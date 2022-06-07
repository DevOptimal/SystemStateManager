using LiteDB;

namespace MachineStateManager.Persistence
{
    internal abstract class PersistedCaretaker<TOriginator, TMemento> : Caretaker<TOriginator, TMemento>
        where TOriginator : IOriginator<TMemento>
        where TMemento : IMemento
    {
        public abstract string ID { get; }

        public string CollectionName => GetType().Name;

        private readonly LiteDatabase database;

        public PersistedCaretaker(TOriginator originator, LiteDatabase database) : base(originator)
        {
            this.database = database;

            if (database.BeginTrans())
            {
                try
                {
                    var col = database.GetCollection<Caretaker<TOriginator, TMemento>>(CollectionName);
                    col.Insert(this);
                    database.Commit();
                }
                catch
                {
                    database.Rollback();
                    throw;
                }
            }
            else
            {
                throw new Exception();
            }
        }

        protected PersistedCaretaker(TOriginator originator, TMemento memento, LiteDatabase database) : base(originator, memento)
        {
            this.database = database;
        }

        protected override void Dispose(bool disposing)
        {
            if (database.BeginTrans())
            {
                try
                {
                    base.Dispose(disposing);
                    var col = database.GetCollection<Caretaker<TOriginator, TMemento>>(CollectionName);
                    col.Delete(ID);
                    database.Commit();
                }
                catch
                {
                    database.Rollback();
                    throw;
                }
            }
            else
            {
                throw new Exception();
            }
        }
    }
}
