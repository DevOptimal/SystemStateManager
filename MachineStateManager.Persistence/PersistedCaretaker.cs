using LiteDB;

namespace MachineStateManager.Persistence
{
    internal class PersistedCaretaker<TOriginator, TMemento> : Caretaker<TOriginator, TMemento>
        where TOriginator : IPersistedOriginator<TMemento>
        where TMemento : IMemento
    {
        public string ID => Originator.ID;

        private readonly LiteDatabase database;

        public PersistedCaretaker(TOriginator originator, LiteDatabase database) : base(originator)
        {
            this.database = database;

            if (database.BeginTrans())
            {
                try
                {
                    var col = database.GetCollection<Caretaker<TOriginator, TMemento>>();
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

        public PersistedCaretaker(TOriginator originator, TMemento memento, LiteDatabase database) : base(originator, memento)
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
                    var col = database.GetCollection<Caretaker<TOriginator, TMemento>>();
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
