using LiteDB;
using System;
using System.Diagnostics;

namespace DevOptimal.MachineStateManager.Persistence
{
    internal abstract class PersistentCaretaker<TOriginator, TMemento> : Caretaker<TOriginator, TMemento>, IPersistentCaretaker
        where TOriginator : IOriginator<TMemento>
        where TMemento : IMemento
    {
        public int ProcessID { get; }

        public DateTime ProcessStartTime { get; }

        private readonly bool persisted;

        private bool disposedValue;

        /// <summary>
        /// Creates a new caretaker.
        /// </summary>
        /// <param name="id">A string that uniquely identifies the resource represented by the caretaker.</param>
        /// <param name="originator">The caretaker's originator, used for getting and setting a memento from the resource.</param>
        /// <exception cref="Exception"></exception>
        protected PersistentCaretaker(string id, TOriginator originator) : base(id, originator)
        {
            var currentProcess = Process.GetCurrentProcess();
            ProcessID = currentProcess.Id;
            ProcessStartTime = currentProcess.StartTime;

            using (var database = LiteDatabaseFactory.GetDatabase())
            {
                if (database.BeginTrans())
                {
                    try
                    {
                        var col = database.GetCollection<IPersistentCaretaker>();
                        col.Insert(this);
                        database.Commit();
                        persisted = true;
                    }
                    catch (Exception ex)
                    {
                        database.Rollback();

                        if (ex is LiteException liteEx && liteEx.ErrorCode == LiteException.INDEX_DUPLICATE_KEY)
                        {
                            throw new ResourceLockedException($"The resource '{ID}' is locked by another instance.", liteEx);
                        }

                        throw;
                    }
                }
                else
                {
                    throw new InvalidOperationException("Cannot open a transaction.");
                }
            }
        }

        /// <summary>
        /// This constructor is used when deserializing a caretaker from a persisted medium.
        /// </summary>
        /// <param name="id">The unique ID of this resource.</param>
        /// <param name="processID">The ID of the process that created the caretaker.</param>
        /// <param name="processStartTime">The start time of the process that created the caretaker. Process IDs are reused, so start time is required to identify a unique process.</param>
        /// <param name="originator">The caretaker's originator, used for getting and setting a memento from the resource.</param>
        /// <param name="memento">The caretaker's memento, which stores the current state of the resource.</param>
        protected PersistentCaretaker(string id, int processID, DateTime processStartTime, TOriginator originator, TMemento memento) : base(id, originator, memento)
        {
            ProcessID = processID;
            ProcessStartTime = processStartTime;
            persisted = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (persisted)
            {
                base.Dispose(disposing);

                if (!disposedValue)
                {
                    if (disposing)
                    {
                        using (var database = LiteDatabaseFactory.GetDatabase())
                        {
                            if (database.BeginTrans())
                            {
                                try
                                {
                                    var col = database.GetCollection<IPersistentCaretaker>();
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
                                throw new InvalidOperationException("Cannot open a transaction.");
                            }
                        }
                    }

                    // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                    // TODO: set large fields to null
                    disposedValue = true;
                }
            }
        }
    }
}
