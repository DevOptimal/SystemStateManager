using DevOptimal.SystemStateManager.FileSystem;
using DevOptimal.SystemUtilities.FileSystem;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace DevOptimal.SystemStateManager.Persistence.SQLite.FileSystem
{
    internal class PersistentDirectoryCaretaker : PersistentCaretaker<DirectoryOriginator, DirectoryMemento>
    {
        public PersistentDirectoryCaretaker(string id, DirectoryOriginator originator, SqliteConnection connection)
            : base(id, originator, connection)
        {
            var command = connection.CreateCommand();
            command.CommandText =
            $@"CREATE TABLE IF NOT EXISTS {nameof(PersistentDirectoryCaretaker)} (
                {nameof(ID)} TEXT PRIMARY KEY,
                {nameof(ProcessID)} TEXT,
                {nameof(ProcessStartTime)} INTEGER,
                {nameof(Originator.Path)} TEXT NOT NULL,
                {nameof(Memento.Exists)} INTEGER NOT NULL
            );";
            command.ExecuteNonQuery();
        }

        public PersistentDirectoryCaretaker(string id, int processID, DateTime processStartTime, DirectoryOriginator originator, DirectoryMemento memento)
            : base(id, processID, processStartTime, originator, memento)
        {
        }

        protected override void Persist()
        {
            var command = connection.CreateCommand();
            command.CommandText =
            $@"INSERT INTO {nameof(PersistentDirectoryCaretaker)} (
                {nameof(ID)},
                {nameof(ProcessID)},
                {nameof(ProcessStartTime)},
                {nameof(Originator.Path)},
                {nameof(Memento.Exists)}
            ) VALUES (
                @{nameof(ID)},
                @{nameof(ProcessID)},
                @{nameof(ProcessStartTime)},
                @{nameof(Originator.Path)},
                @{nameof(Memento.Exists)}
            );";
            command.Parameters.AddWithValue($"@{nameof(ID)}", ID);
            command.Parameters.AddWithValue($"@{nameof(ProcessID)}", ProcessID);
            command.Parameters.AddWithValue($"@{nameof(ProcessStartTime)}", ProcessStartTime.Ticks);
            command.Parameters.AddWithValue($"@{nameof(Originator.Path)}", Originator.Path);
            command.Parameters.AddWithValue($"@{nameof(Memento.Exists)}", Memento.Exists);
            command.ExecuteNonQuery();
        }

        protected override void Unpersist()
        {
            var command = connection.CreateCommand();
            command.CommandText = $@"DELETE FROM {nameof(PersistentDirectoryCaretaker)} WHERE {nameof(ID)} = @{nameof(ID)};";
            command.Parameters.AddWithValue($"@{nameof(ID)}", ID);
            command.ExecuteNonQuery();
        }

        public static IEnumerable<IPersistentSnapshot> GetCaretakers(SqliteConnection connection, IFileSystem fileSystem)
        {
            var caretakers = new List<PersistentDirectoryCaretaker>();
            var command = connection.CreateCommand();
            command.CommandText = $@"SELECT * FROM {nameof(PersistentDirectoryCaretaker)};";
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var caretaker = new PersistentDirectoryCaretaker(
                        id: reader.GetString(reader.GetOrdinal(nameof(ID))),
                        processID: reader.GetInt32(reader.GetOrdinal(nameof(ProcessID))),
                        processStartTime: new DateTime(reader.GetInt64(reader.GetOrdinal(nameof(ProcessStartTime)))),
                        originator: new DirectoryOriginator(
                            path: reader.GetString(reader.GetOrdinal(nameof(DirectoryOriginator.Path))),
                            fileSystem: fileSystem
                        ),
                        memento: new DirectoryMemento
                        {
                            Exists = reader.GetBoolean(reader.GetOrdinal(nameof(DirectoryMemento.Exists)))
                        }
                    );
                    caretakers.Add(caretaker);
                }
            }

            return caretakers;
        }
    }
}
