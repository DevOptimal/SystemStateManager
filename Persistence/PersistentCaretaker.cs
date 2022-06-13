using LiteDB;
using MachineStateManager.Core;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;

namespace MachineStateManager.Persistence
{
    internal abstract class PersistentCaretaker<TOriginator, TMemento> : Caretaker<TOriginator, TMemento>, IPersistentCaretaker
        where TOriginator : IOriginator<TMemento>
        where TMemento : IMemento
    {
        [BsonId]
        public string ID { get; }

        public int ProcessID { get; }

        public DateTime ProcessStartTime { get; }

        public string CollectionName => GetType().Name;

        private readonly bool persisted = false;

        public PersistentCaretaker(string id, TOriginator originator) : base(originator)
        {
            ID = id;
            ProcessID = System.Environment.ProcessId;
            ProcessStartTime = Process.GetCurrentProcess().StartTime;

            using var database = GetDatabase();

            if (database.BeginTrans())
            {
                try
                {
                    var col = database.GetCollection<PersistentCaretaker<TOriginator, TMemento>>(CollectionName);
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
                throw new Exception();
            }
        }

        [BsonCtor]
        public PersistentCaretaker(string id, int processID, DateTime processStartTime, TOriginator originator, TMemento memento) : base(originator, memento)
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

                using var database = GetDatabase();

                if (database.BeginTrans())
                {
                    try
                    {
                        var col = database.GetCollection<PersistentCaretaker<TOriginator, TMemento>>(CollectionName);
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

        public static LiteDatabase GetDatabase()
        {
            var databaseDirectory = new DirectoryInfo(Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData),
                nameof(MachineStateManager)));

            if (!databaseDirectory.Exists)
            {
                databaseDirectory.Create();

                if (OperatingSystem.IsWindows())
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
                $"{nameof(Persistence)}.db");

            return new LiteDatabase(connectionString: new ConnectionString(databaseFilePath)
            {
                Connection = ConnectionType.Shared
            });
        }

        protected static IEnumerable<IDisposable> GetAbandonedCaretakers<T>(Dictionary<int, DateTime?> processes)
            where T : PersistentCaretaker<TOriginator, TMemento>
        {
            using var database = GetDatabase();

            return database.GetCollection<T>(typeof(T).Name).FindAll()
                .Where(c => !(processes.ContainsKey(c.ProcessID) &&
                    (
                        processes[c.ProcessID] == c.ProcessStartTime ||
                        processes[c.ProcessID] == null
                    )))
                .Cast<IDisposable>();
        }
    }
}
