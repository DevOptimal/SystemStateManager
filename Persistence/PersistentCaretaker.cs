using LiteDB;
using MachineStateManager.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private readonly bool persisted = false;

        private bool disposedValue;

        public PersistentCaretaker(string id, TOriginator originator) : base(originator)
        {
            ID = id;
            ProcessID = Process.GetCurrentProcess().Id;
            ProcessStartTime = Process.GetCurrentProcess().StartTime;

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
                    throw new Exception();
                }
            }
        }

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
                                throw new Exception();
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

            return new LiteDatabase(connectionString: new ConnectionString(databaseFilePath)
            {
                Connection = ConnectionType.Shared
            });
        }

        protected static IEnumerable<IDisposable> GetAbandonedCaretakers<T>(Dictionary<int, DateTime?> processes)
            where T : PersistentCaretaker<TOriginator, TMemento>
        {
            using (var database = GetDatabase())
            {
                return database.GetCollection<T>(typeof(T).Name).FindAll()
                    .Where(c => !(processes.ContainsKey(c.ProcessID) &&
                        (
                            processes[c.ProcessID] == c.ProcessStartTime ||
                            processes[c.ProcessID] == null
                        )))
                    .Cast<IDisposable>();
            }
        }
        private static BsonValue SerializeEnvironment<T>(T obj)
        {
            var type = obj.GetType();
            var bsonValue = BsonMapper.Global.ToDocument(type, obj);
            bsonValue["_type"] = obj.GetType().AssemblyQualifiedName;
            return bsonValue;
        }

        private static T DeserializeEnvironment<T>(BsonValue bsonValue)
        {
            var assemblyQualifiedName = bsonValue["_type"];
            var type = Type.GetType(assemblyQualifiedName);
            if (type == null) // Means the type was not found in this app domain.
            {
                throw new TypeLoadException($"Unable to find the type '{assemblyQualifiedName}'. You may be missing an assembly reference.");
            }
            return (T)BsonMapper.Global.ToObject(type, bsonValue.AsDocument);
        }
    }
}
