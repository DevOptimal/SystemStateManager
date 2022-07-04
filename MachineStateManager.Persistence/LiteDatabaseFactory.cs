﻿using LiteDB;
using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace DevOptimal.MachineStateManager.Persistence
{
    internal static class LiteDatabaseFactory
    {
        public static LiteDatabase GetDatabase()
        {
            if (!PersistentMachineStateManager.PersistenceURI.IsFile)
            {
                throw new NotSupportedException($"{nameof(PersistentMachineStateManager.PersistenceURI)} is invalid. Only local file paths are supported.");
            }

            var databaseFile = new FileInfo(PersistentMachineStateManager.PersistenceURI.LocalPath);

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

            // LiteDB adheres to the BSON specification, and only stores DateTime values up to the milliseconds.
            // The following overrides the default behavior to store the exact DateTime, down to the tick.
            // For more information, see https://github.com/mbdavid/LiteDB/issues/1765.
            var mapper = new BsonMapper();
            mapper.RegisterType(
                serialize: value => value.ToString("o", CultureInfo.InvariantCulture),
                deserialize: bson => DateTime.ParseExact(bson, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));

            return new LiteDatabase(
                connectionString: new ConnectionString(databaseFile.FullName)
                {
                    Connection = ConnectionType.Shared
                },
                mapper: mapper);
        }
    }
}
