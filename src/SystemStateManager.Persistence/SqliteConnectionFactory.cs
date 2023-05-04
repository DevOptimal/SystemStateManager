using Microsoft.Data.Sqlite;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace DevOptimal.SystemStateManager.Persistence
{
    internal static class SqliteConnectionFactory
    {
        public static FileInfo DatabaseFile { get; set; } = new FileInfo(
            Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData),
                nameof(SystemStateManager),
                $"{nameof(Persistence)}.db"));

        public static SqliteConnection Create()
        {
            var databaseDirectory = DatabaseFile.Directory;

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

            // In some cases, we need to manually initialize the SQLitePCLRaw bundles before calling into Microsoft.Data.Sqlite
            SQLitePCL.Batteries_V2.Init();

            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = DatabaseFile.FullName,
            }.ToString();

            var connection = new SqliteConnection(connectionString);
            connection.Open();

            // Enable write-ahead logging
            var command = connection.CreateCommand();
            command.CommandText =
            @"
                PRAGMA journal_mode = 'wal'
            ";
            command.ExecuteNonQuery();

            return connection;
        }
    }
}
