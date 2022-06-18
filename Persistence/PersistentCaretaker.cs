using LiteDB;
using MachineStateManager.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace MachineStateManager.Persistence
{
    internal abstract class PersistentCaretaker<TOriginator, TMemento> : Caretaker<TOriginator, TMemento>
        where TOriginator : IOriginator<TMemento>
        where TMemento : IMemento
    {
        [BsonId]
        public string ID { get; }

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
        protected PersistentCaretaker(string id, TOriginator originator) : base(originator)
        {
            ID = id;

            var currentProcess = Process.GetProcessById(Process.GetCurrentProcess().Id);
            ProcessID = currentProcess.Id;
            ProcessStartTime = currentProcess.StartTime;

            using (var database = GetDatabase())
            {
                if (database.BeginTrans())
                {
                    try
                    {
                        var col = database.GetCollection<PersistentCaretaker<TOriginator, TMemento>>(GetType().Name);
                        col.Insert(this);
                        database.Commit();
                        persisted = true;
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

        /// <summary>
        /// This constructor is used when deserializing a caretaker from a persisted medium.
        /// </summary>
        /// <param name="id">The unique ID of this resource.</param>
        /// <param name="processID">The ID of the process that created the caretaker.</param>
        /// <param name="processStartTime">The start time of the process that created the caretaker. Process IDs are reused, so start time is required to identify a unique process.</param>
        /// <param name="originator">The caretaker's originator, used for getting and setting a memento from the resource.</param>
        /// <param name="memento">The caretaker's memento, which stores the current state of the resource.</param>
        protected PersistentCaretaker(string id, int processID, DateTime processStartTime, TOriginator originator, TMemento memento) : base(originator, memento)
        {
            ID = id;
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
                        using (var database = GetDatabase())
                        {
                            if (database.BeginTrans())
                            {
                                try
                                {
                                    var col = database.GetCollection<PersistentCaretaker<TOriginator, TMemento>>(GetType().Name);
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

        public static LiteDatabase GetDatabase()
        {
            var databaseDirectory = new DirectoryInfo(Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData),
                nameof(MachineStateManager)));

            if (!databaseDirectory.Exists)
            {
                databaseDirectory.Create();

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var directorySecurity = databaseDirectory.GetAccessControl();
                    directorySecurity.AddAccessRule(new FileSystemAccessRule(
                        identity: new SecurityIdentifier(WellKnownSidType.WorldSid, domainSid: null),
                        fileSystemRights: FileSystemRights.FullControl,
                        inheritanceFlags: InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                        propagationFlags: PropagationFlags.NoPropagateInherit,
                        type: AccessControlType.Allow));
                    databaseDirectory.SetAccessControl(directorySecurity);
                }
            }

            var databaseFilePath = Path.Combine(
                databaseDirectory.FullName,
                $"{nameof(Persistence)}.litedb");

            // LiteDB adheres to the BSON specification, and only stores DateTime values up to the milliseconds.
            // The following overrides the default behavior to store the exact DateTime, down to the tick.
            // For more information, see https://github.com/mbdavid/LiteDB/issues/1765.
            var mapper = new BsonMapper();
            mapper.RegisterType(
                serialize: value => value.ToString("o", CultureInfo.InvariantCulture),
                deserialize: bson => DateTime.ParseExact(bson, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));

            return new LiteDatabase(
                connectionString: new ConnectionString(databaseFilePath)
                {
                    Connection = ConnectionType.Shared
                },
                mapper: mapper);
        }

        /// <summary>
        /// Gets abandoned caretakers on the current machine. An "abandoned caretaker" is a caretaker that was created by a process that no longer exists.
        /// </summary>
        /// <typeparam name="T">The type of abandoned caretakers to get.</typeparam>
        /// <param name="processes">A dictionary of process ID to process start time key value pairs that each uniquely identify a currently running process.
        /// A null value indicates that the current process does not have permission to the process - try rerunning in an elevated process. Pass this data in
        /// instead of calling Process.GetProcesses() because it is an expensive call.</param>
        /// <returns>An enumeration of all caretakers on this machine that have been abandoned.</returns>
        protected static IEnumerable<IDisposable> GetAbandonedCaretakers<T>(Dictionary<int, DateTime?> processes)
            where T : PersistentCaretaker<TOriginator, TMemento>
        {
            using (var database = GetDatabase())
            {
                var allCaretakers = database.GetCollection<T>(typeof(T).Name).FindAll().ToList();

                var result = new List<IDisposable>();
                foreach (var caretaker in allCaretakers)
                {
                    if (!(processes.ContainsKey(caretaker.ProcessID) &&
                        (
                            processes[caretaker.ProcessID] == caretaker.ProcessStartTime ||
                            processes[caretaker.ProcessID] == null
                        )))
                    {
                        result.Add(caretaker);
                    }
                }

                return result;

                //return database.GetCollection<T>(typeof(T).Name).FindAll()
                //    .Where(c => !(processes.ContainsKey(c.ProcessID) &&
                //        (
                //            processes[c.ProcessID] == c.ProcessStartTime ||
                //            processes[c.ProcessID] == null
                //        )))
                //    .Cast<IDisposable>();
            }
        }
    }
}
