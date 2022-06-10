using LiteDB;
using MachineStateManager.Core;
using System.Diagnostics;

namespace MachineStateManager.Persistence
{
    internal abstract class PersistentCaretaker<TOriginator, TMemento> : Caretaker<TOriginator, TMemento>, IPersistentCaretaker
        where TOriginator : IOriginator<TMemento>
        where TMemento : IMemento
    {
        public string ID { get; }

        public int ProcessID { get; }

        public DateTime ProcessStartTime { get; }

        public string CollectionName => GetType().Name;

        protected readonly LiteDatabase database;

        public PersistentCaretaker(string id, TOriginator originator, LiteDatabase database) : base(originator)
        {
            ID = id;
            ProcessID = System.Environment.ProcessId;
            ProcessStartTime = Process.GetCurrentProcess().StartTime;

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

        protected PersistentCaretaker(string id, int processID, DateTime processStartTime, TOriginator originator, TMemento memento, LiteDatabase database) : base(originator, memento)
        {
            ID = id;
            ProcessID = processID;
            ProcessStartTime = processStartTime;

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
