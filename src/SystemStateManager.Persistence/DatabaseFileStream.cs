using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace DevOptimal.SystemStateManager.Persistence
{
    internal class DatabaseFileStream : FileStream
    {
        private readonly Mutex mutex;

        private readonly string targetFilePath;

        private readonly string backupFilePath;

        private readonly FileStream targetFileStream;

        private bool disposedValue;

        public DatabaseFileStream(FileInfo databaseFile)
            : this(databaseFile, Timeout.InfiniteTimeSpan)
        { }

        public DatabaseFileStream(FileInfo databaseFile, TimeSpan timeout)
            : this(GetFileMutex(databaseFile, timeout), databaseFile.FullName, GetBackupFilePath(databaseFile))
        { }

        private DatabaseFileStream(Mutex mutex, string targetFilePath, string backupFilePath)
            : base(backupFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)
        {
            this.mutex = mutex;
            this.targetFilePath = targetFilePath;
            this.backupFilePath = backupFilePath;

            targetFileStream = new FileStream(targetFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            SetLength(0);
            Flush();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposedValue)
            {
                if (disposing)
                {
                    targetFileStream.Dispose();
                    File.Copy(backupFilePath, targetFilePath, overwrite: true);
                    File.Delete(backupFilePath);

                    mutex.ReleaseMutex();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        private static string GetBackupFilePath(FileInfo databaseFile)
        {
            return $"{databaseFile.FullName}.backup";
        }

        private static Mutex GetFileMutex(FileInfo databaseFile, TimeSpan timeout)
        {
            // Get normalized file path
            var databaseFilePath = Path.GetFullPath(databaseFile.FullName).Replace('\\', '/');
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                databaseFilePath = databaseFilePath.ToLower();
            }

            // unique id for global mutex - Global prefix means it is global to the machine
            var mutexId = $"Global\\{nameof(PersistentSystemStateManager)}:/{databaseFilePath}";

            // edited by Jeremy Wiebe to add example of setting up security for multi-user usage
            // edited by 'Marc' to work also on localized systems (don't use just "Everyone") 
            var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, domainSid: null), MutexRights.FullControl, AccessControlType.Allow);
            var securitySettings = new MutexSecurity();
            securitySettings.AddAccessRule(allowEveryoneRule);

            // edited by MasonGZhwiti to prevent race condition on security settings via VanNguyen
            var mutex = new Mutex(false, mutexId, out var createdNew, securitySettings);

            try
            {
                if (!mutex.WaitOne(timeout, false))
                {
                    throw new TimeoutException("Timeout waiting for exclusive access");
                }
            }
            catch (AbandonedMutexException)
            {
                // Log the fact that the mutex was abandoned in another process,
                // it will still get acquired
            }

            return mutex;
        }
    }
}
