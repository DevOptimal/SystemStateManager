using DevOptimal.SystemStateManager.FileSystem;
using DevOptimal.SystemStateManager.JsonDatabase.Serialization;
using DevOptimal.SystemUtilities.Environment.Abstractions;
using DevOptimal.SystemUtilities.FileSystem.Abstractions;
using DevOptimal.SystemUtilities.FileSystem.Extensions;
using DevOptimal.SystemUtilities.Registry.Abstractions;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace DevOptimal.SystemStateManager.JsonDatabase
{
    public class JsonDatabaseTransaction : IDisposable
    {
        private readonly Mutex mutex;
        private readonly FileInfo databaseFile;
        private readonly FileInfo transactionFile;

        private readonly IEnvironment environment;
        private readonly IFileSystem fileSystem;
        private readonly IRegistry registry;
        private readonly IFileCache fileCache;

        public JsonDatabaseTransaction(FileInfo databaseFile, IEnvironment environment, IFileSystem fileSystem, IRegistry registry, IFileCache fileCache, TimeSpan timeout)
        {
            this.databaseFile = databaseFile;
            transactionFile = databaseFile.Directory.GetFile("transaction.json");
            this.environment = environment;
            this.fileSystem = fileSystem;
            this.registry = registry;
            this.fileCache = fileCache;
            if (!databaseFile.Exists(fileSystem))
            {
                if (transactionFile.Exists(fileSystem))
                {
                    // If the database file doesn't exist but the transaction file does, then something probably went wrong when we were trying to overwrite the database file.
                    // In this case, just complete the overwrite operation.
                    transactionFile.MoveTo(databaseFile, fileSystem);
                }
                else
                {
                    // If neither the database nor transaction file exist, then create a new database file.
                    databaseFile.WriteAllText("[]", fileSystem);
                }
            }

            // Get normalized file path
            var normalizedDatabaseID = Path.GetFullPath(databaseFile.FullName).Replace('\\', '/');
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                normalizedDatabaseID = normalizedDatabaseID.ToLower();
            }

            // unique id for global mutex - Global prefix means it is global to the machine
            var mutexId = $@"Global\{nameof(SystemStateManager)}.{nameof(JsonDatabase)}:/{normalizedDatabaseID}";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // edited by Jeremy Wiebe to add example of setting up security for multi-user usage
                // edited by 'Marc' to work also on localized systems (don't use just "Everyone") 
                var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, domainSid: null), MutexRights.FullControl, AccessControlType.Allow);
                var securitySettings = new MutexSecurity();
                securitySettings.AddAccessRule(allowEveryoneRule);

                // edited by MasonGZhwiti to prevent race condition on security settings via VanNguyen
#if NETSTANDARD2_0
                mutex = MutexAcl.Create(false, mutexId, out var createdNew, securitySettings);
#else
                mutex = new Mutex(false, mutexId, out var createdNew, securitySettings);
#endif
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                mutex = new Mutex(false, mutexId, out var createdNew);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            try
            {
                if (!mutex.WaitOne(timeout, exitContext: false))
                {
                    throw new TimeoutException("Timeout waiting for exclusive access");
                }
            }
            catch (AbandonedMutexException)
            {
                // Log the fact that the mutex was abandoned in another process,
                // it will still get acquired
            }
        }



        public void Commit()
        {
            transactionFile.MoveTo(databaseFile, overwrite: true, fileSystem: fileSystem);
            mutex?.ReleaseMutex();
            mutex = null;
        }

        public void Rollback()
        {
            transactionFile.Delete();
        }

        public void Dispose()
        {
            mutex?.ReleaseMutex();
        }

        private Stream OpenDatabaseFile()
        {
            return File.Open(databaseFile.FullName, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);
        }

        private Stream OpenTransactionFile()
        {
            return File.Open(transactionFile.FullName, FileMode.Create, FileAccess.Write, FileShare.None);
        }

        private IEnumerable<ISnapshot> ReadSnapshots()
        {
            if (!File.Exists(databaseFile.FullName))
            {
                if (File.Exists(transactionFile.FullName))
                {
                    // If the database file doesn't exist but the transaction file does, then something probably went wrong when we were trying to overwrite the database file.
                    // In this case, just complete the overwrite operation.
                    File.Move(transactionFile.FullName, databaseFile.FullName);
                }
                else
                {
                    // If neither the database nor transaction file exist, then create a new database file.
                    File.WriteAllText(databaseFile.FullName, "[]");
                }
            }
            using (var stream = OpenDatabaseFile())
            using (var reader = new JsonReader(stream))
            {
                return reader.ReadSnapshots(this, environment, fileSystem, registry, fileCache);
            }
        }

        private void WriteSnapshots(IEnumerable<ISnapshot> snapshots)
        {
            if (File.Exists(transactionFile.FullName))
            {
                File.Delete(transactionFile.FullName);
            }
            using (var stream = OpenTransactionFile())
            using (var writer = new JsonWriter(stream))
            {
                writer.WriteSnapshots(snapshots);
            }
            File.Delete(databaseFile.FullName);
            File.Move(transactionFile.FullName, databaseFile.FullName);
        }
    }
}
