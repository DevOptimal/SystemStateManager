using Microsoft.Data.Sqlite;
using System;
using System.Diagnostics;

namespace DevOptimal.SystemStateManager.Persistence
{
    internal abstract class PersistentCaretaker<TOriginator, TMemento> : Caretaker<TOriginator, TMemento>, IPersistentSnapshot
        where TOriginator : IOriginator<TMemento>
        where TMemento : IMemento
    {
        public int ProcessID { get; }

        public DateTime ProcessStartTime { get; }

        protected readonly SqliteConnection connection;

        private readonly bool persisted;

        private bool disposedValue;

        /// <summary>
        /// Creates a new caretaker.
        /// </summary>
        /// <param name="id">A string that uniquely identifies the resource represented by the caretaker.</param>
        /// <param name="originator">The caretaker's originator, used for getting and setting a memento from the resource.</param>
        /// <exception cref="Exception"></exception>
        protected PersistentCaretaker(string id, TOriginator originator, SqliteConnection connection) : base(id, originator)
        {
            this.connection = connection;
            var currentProcess = Process.GetCurrentProcess();
            ProcessID = currentProcess.Id;
            ProcessStartTime = currentProcess.StartTime;

            lock (connection) // Sqlite connections are not thread safe: https://github.com/dotnet/efcore/issues/22664#issuecomment-696870423
            {
                using (var transaction = this.connection.BeginTransaction())
                {
                    try
                    {
                        Initialize();
                        Persist();
                        transaction.Commit();
                        persisted = true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();

                        if (ex is SqliteException sqliteEx && sqliteEx.SqliteErrorCode == 19 && sqliteEx.SqliteExtendedErrorCode == 1555)
                        {
                            throw new ResourceLockedException($"The resource '{ID}' is locked by another instance.", sqliteEx);
                        }

                        throw;
                    }
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
        protected PersistentCaretaker(string id, int processID, DateTime processStartTime, TOriginator originator, TMemento memento, SqliteConnection connection) : base(id, originator, memento)
        {
            this.connection = connection;
            ProcessID = processID;
            ProcessStartTime = processStartTime;
            persisted = true;
        }

        protected abstract void Initialize();

        protected abstract void Persist();

        protected abstract void Unpersist();

        protected override void Dispose(bool disposing)
        {
            if (persisted)
            {
                base.Dispose(disposing);

                if (!disposedValue)
                {
                    if (disposing)
                    {
                        lock (connection)
                        {
                            using (var transaction = connection.BeginTransaction())
                            {
                                try
                                {
                                    Unpersist();
                                    transaction.Commit();
                                }
                                catch
                                {
                                    transaction.Rollback();
                                    throw;
                                }
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
