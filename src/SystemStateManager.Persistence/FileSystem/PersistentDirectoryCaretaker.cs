﻿using DevOptimal.SystemStateManager.FileSystem;
using DevOptimal.SystemUtilities.FileSystem;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace DevOptimal.SystemStateManager.Persistence.FileSystem
{
    internal class PersistentDirectoryCaretaker : PersistentCaretaker<DirectoryOriginator, DirectoryMemento>
    {
        public PersistentDirectoryCaretaker(string id, DirectoryOriginator originator)
            : base(id, originator)
        {
        }

        public PersistentDirectoryCaretaker(string id, int processID, DateTime processStartTime, DirectoryOriginator originator, DirectoryMemento memento)
            : base(id, processID, processStartTime, originator, memento)
        {
        }


        protected override void Initialize(SqliteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText =
            $@"CREATE TABLE IF NOT EXISTS {nameof(PersistentDirectoryCaretaker)} (
                '{nameof(ID)}' TEXT PRIMARY KEY,
                '{nameof(ProcessID)}' INTEGER NOT NULL,
                '{nameof(ProcessStartTime)}' INTEGER NOT NULL,
                '{nameof(Originator.Path)}' TEXT NOT NULL,
                '{nameof(Memento.Exists)}' INTEGER NOT NULL
            );";
            command.ExecuteNonQuery();
        }

        protected override void Persist(SqliteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText =
            $@"INSERT INTO {nameof(PersistentDirectoryCaretaker)} (
                '{nameof(ID)}',
                '{nameof(ProcessID)}',
                '{nameof(ProcessStartTime)}',
                '{nameof(Originator.Path)}',
                '{nameof(Memento.Exists)}'
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

        protected override void Unpersist(SqliteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = $@"DELETE FROM {nameof(PersistentDirectoryCaretaker)} WHERE {nameof(ID)} = @{nameof(ID)};";
            command.Parameters.AddWithValue($"@{nameof(ID)}", ID);
            command.ExecuteNonQuery();
        }

        public static IEnumerable<IPersistentSnapshot> GetCaretakers(SqliteConnection connection, IFileSystem fileSystem)
        {
            var caretakers = new List<PersistentDirectoryCaretaker>();

            if (connection.TableExists(nameof(PersistentDirectoryCaretaker)))
            {
                using (var reader = connection.ExecuteReader($@"SELECT * FROM {nameof(PersistentDirectoryCaretaker)};"))
                {
                    while (reader.Read())
                    {
                        var caretaker = new PersistentDirectoryCaretaker(
                            id: reader.GetString(nameof(ID)),
                            processID: reader.GetInt32(nameof(ProcessID)),
                            processStartTime: new DateTime(reader.GetInt64(nameof(ProcessStartTime))),
                            originator: new DirectoryOriginator(
                                path: reader.GetString(nameof(DirectoryOriginator.Path)),
                                fileSystem: fileSystem
                            ),
                            memento: new DirectoryMemento
                            {
                                Exists = reader.GetBoolean(nameof(DirectoryMemento.Exists))
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
