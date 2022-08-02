using LiteDB;
using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace DevOptimal.SystemStateManager.Persistence
{
    internal static class LiteDatabaseFactory
    {
        public static BsonMapper Mapper { get; }

        static LiteDatabaseFactory()
        {
            Mapper = new BsonMapper();

            // LiteDB adheres to the BSON specification, and only stores DateTime values up to the milliseconds.
            // The following overrides the default behavior to store the exact DateTime, down to the tick.
            // For more information, see https://github.com/mbdavid/LiteDB/issues/1765.
            Mapper.RegisterType(
                serialize: value => value.ToString("o", CultureInfo.InvariantCulture),
                deserialize: bson => DateTime.ParseExact(bson, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
        }

        public static LiteDatabase GetDatabase()
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

            return new LiteDatabase(
                connectionString: new ConnectionString(databaseFile.FullName)
                {
                    Connection = ConnectionType.Shared
                },
                mapper: Mapper);
        }
    }
}
