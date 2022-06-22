using LiteDB;
using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace bradselw.MachineStateManager.Persistence
{
    internal static class LiteDatabaseFactory
    {
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
    }
}
