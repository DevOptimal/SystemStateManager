using DevOptimal.SystemStateManager.FileSystem;
using DevOptimal.SystemUtilities.FileSystem;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace DevOptimal.SystemStateManager.Persistence.FileSystem
{
    internal class PersistentFileCaretaker : PersistentCaretaker<FileOriginator, FileMemento>
    {
        public PersistentFileCaretaker(string id, FileOriginator originator)
            : base(id, originator)
        {
        }

        public PersistentFileCaretaker(string id, int processID, DateTime processStartTime, FileOriginator originator, FileMemento memento)
            : base(id, processID, processStartTime, originator, memento)
        {
        }


        protected override void Initialize(SqliteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText =
            $@"CREATE TABLE IF NOT EXISTS {nameof(PersistentFileCaretaker)} (
                '{nameof(ID)}' TEXT PRIMARY KEY,
                '{nameof(ProcessID)}' INTEGER NOT NULL,
                '{nameof(ProcessStartTime)}' INTEGER NOT NULL,
                '{nameof(Originator.Path)}' TEXT NOT NULL,
                '{nameof(Memento.Hash)}' TEXT
            );";
            command.ExecuteNonQuery();
        }

        protected override void Persist(SqliteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText =
            $@"INSERT INTO {nameof(PersistentFileCaretaker)} (
                '{nameof(ID)}',
                '{nameof(ProcessID)}',
                '{nameof(ProcessStartTime)}',
                '{nameof(Originator.Path)}',
                '{nameof(Memento.Hash)}'
            ) VALUES (
                @{nameof(ID)},
                @{nameof(ProcessID)},
                @{nameof(ProcessStartTime)},
                @{nameof(Originator.Path)},
                @{nameof(Memento.Hash)}
            );";
            command.Parameters.AddWithValue($"@{nameof(ID)}", ID);
            command.Parameters.AddWithValue($"@{nameof(ProcessID)}", ProcessID);
            command.Parameters.AddWithValue($"@{nameof(ProcessStartTime)}", ProcessStartTime.Ticks);
            command.Parameters.AddWithValue($"@{nameof(Originator.Path)}", Originator.Path);
            command.Parameters.AddWithNullableValue($"@{nameof(Memento.Hash)}", Memento.Hash);
            command.ExecuteNonQuery();
        }

        protected override void Unpersist(SqliteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = $@"DELETE FROM {nameof(PersistentFileCaretaker)} WHERE {nameof(ID)} = @{nameof(ID)};";
            command.Parameters.AddWithValue($"@{nameof(ID)}", ID);
            command.ExecuteNonQuery();
        }

        public static IEnumerable<IPersistentSnapshot> GetCaretakers(SqliteConnection connection, IFileSystem fileSystem, IFileCache fileCache)
        {
            var caretakers = new List<PersistentFileCaretaker>();

            if (connection.TableExists(nameof(PersistentFileCaretaker)))
            {
                using (var reader = connection.ExecuteReader($@"SELECT * FROM {nameof(PersistentFileCaretaker)};"))
                {
                    while (reader.Read())
                    {
                        var caretaker = new PersistentFileCaretaker(
                            id: reader.GetString(nameof(ID)),
                            processID: reader.GetInt32(nameof(ProcessID)),
                            processStartTime: new DateTime(reader.GetInt64(nameof(ProcessStartTime))),
                            originator: new FileOriginator(
                                path: reader.GetString(nameof(FileOriginator.Path)),
                                fileCache: fileCache,
                                fileSystem: fileSystem
                            ),
                            memento: new FileMemento
                            {
                                Hash = reader.GetNullableString(nameof(FileMemento.Hash))
                            }
                        );
                        caretakers.Add(caretaker);
                    }
                }
            }

            return caretakers;
        }
    }
}
