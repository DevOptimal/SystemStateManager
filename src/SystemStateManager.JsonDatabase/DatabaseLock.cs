using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace DevOptimal.SystemStateManager.JsonDatabase
{
    internal class DatabaseLock : IDisposable
    {
        private readonly Mutex mutex;

        public DatabaseLock(FileInfo databaseFile, TimeSpan timeout)
        {
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

        public void Dispose()
        {
            mutex?.ReleaseMutex();
        }
    }
}
