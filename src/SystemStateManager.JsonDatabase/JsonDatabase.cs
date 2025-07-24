using DevOptimal.SystemStateManager.FileSystem;
using DevOptimal.SystemStateManager.JsonDatabase.Serialization;
using DevOptimal.SystemUtilities.Environment.Abstractions;
using DevOptimal.SystemUtilities.FileSystem.Abstractions;
using DevOptimal.SystemUtilities.FileSystem.Extensions;
using DevOptimal.SystemUtilities.Registry.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace DevOptimal.SystemStateManager.JsonDatabase
{
    public class JsonDatabase : IDatabase
    {
        private readonly IEnvironment environment;
        private readonly IFileSystem fileSystem;
        private readonly IRegistry registry;
        private readonly IFileCache fileCache;

        private readonly TimeSpan databaseLockTimeout = TimeSpan.FromMinutes(5);

        private static readonly DirectoryInfo databaseDirectory = new DirectoryInfo(
            Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData),
                nameof(SystemStateManager),
                nameof(JsonDatabase)));

        private static readonly FileInfo databaseFile = databaseDirectory.GetFile("database.json");
        private static readonly FileInfo transactionFile = databaseDirectory.GetFile("transaction.json");

        public JsonDatabase(IEnvironment environment, IFileSystem fileSystem, IRegistry registry, IFileCache fileCache)
        {
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

            this.environment = environment;
            this.fileSystem = fileSystem;
            this.registry = registry;
            this.fileCache = fileCache;
        }

        public void AddSnapshot(ISnapshot snapshot)
        {
            using (new DatabaseLock(databaseFile, databaseLockTimeout))
            {
                WriteSnapshots(ReadSnapshots().Concat(new[] { snapshot }));
            }
        }

        public ISnapshot GetSnapshot(string id)
        {
            using (new DatabaseLock(databaseFile, databaseLockTimeout))
            {
                return ReadSnapshots().First(s => s.ID == id);
            }
        }

        public IEnumerable<ISnapshot> GetSnapshots()
        {
            using (new DatabaseLock(databaseFile, databaseLockTimeout))
            {
                return ReadSnapshots();
            }
        }

        public void RemoveSnapshot(ISnapshot snapshot)
        {
            using (new DatabaseLock(databaseFile, databaseLockTimeout))
            {
                WriteSnapshots(ReadSnapshots().Except(new[] { snapshot }));
            }
        }
    }
}
