﻿using SQLite;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace DevOptimal.SystemStateManager.Persistence.SQLite
{
    internal static class SQLiteDatabaseFactory
    {
        public static SQLiteConnection GetDatabase()
        {
            if (!PersistentSystemStateManager.PersistenceURI.IsFile)
            {
                throw new NotSupportedException($"{nameof(PersistentSystemStateManager.PersistenceURI)} is invalid. Only local file paths are supported.");
            }

            var databaseFile = new FileInfo(PersistentSystemStateManager.PersistenceURI.LocalPath);

            var databaseDirectory = databaseFile.Directory;

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

            return new SQLiteConnection(databaseFile.FullName);
        }
    }
}
