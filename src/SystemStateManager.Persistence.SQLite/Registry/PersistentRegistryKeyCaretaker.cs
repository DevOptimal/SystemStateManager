using DevOptimal.SystemStateManager.Registry;
using DevOptimal.SystemUtilities.Registry;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using System;
using System.Collections.Generic;

namespace DevOptimal.SystemStateManager.Persistence.SQLite.Registry
{
    internal class PersistentRegistryKeyCaretaker : PersistentCaretaker<RegistryKeyOriginator, RegistryKeyMemento>
    {
        public PersistentRegistryKeyCaretaker(string id, RegistryKeyOriginator originator, SqliteConnection connection)
            : base(id, originator, connection)
        {
            var command = connection.CreateCommand();
            command.CommandText =
            $@"CREATE TABLE IF NOT EXISTS {nameof(PersistentRegistryKeyCaretaker)} (
                {nameof(ID)} TEXT PRIMARY KEY,
                {nameof(ProcessID)} TEXT,
                {nameof(ProcessStartTime)} INTEGER,
                {nameof(Originator.Hive)} INTEGER NOT NULL,
                {nameof(Originator.View)} INTEGER NOT NULL,
                {nameof(Originator.SubKey)} TEXT NOT NULL,
                {nameof(Memento.Exists)} INTEGER NOT NULL
            );";
            command.ExecuteNonQuery();
        }

        public PersistentRegistryKeyCaretaker(string id, int processID, DateTime processStartTime, RegistryKeyOriginator originator, RegistryKeyMemento memento)
            : base(id, processID, processStartTime, originator, memento)
        {
        }

        protected override void Persist()
        {
            var command = connection.CreateCommand();
            command.CommandText =
            $@"INSERT INTO {nameof(PersistentRegistryKeyCaretaker)} (
                {nameof(ID)},
                {nameof(ProcessID)},
                {nameof(ProcessStartTime)},
                {nameof(Originator.Hive)},
                {nameof(Originator.View)},
                {nameof(Originator.SubKey)},
                {nameof(Memento.Exists)}
            ) VALUES (
                @{nameof(ID)},
                @{nameof(ProcessID)},
                @{nameof(ProcessStartTime)},
                @{nameof(Originator.Hive)},
                @{nameof(Originator.View)},
                @{nameof(Originator.SubKey)},
                @{nameof(Memento.Exists)}
            );";
            command.Parameters.AddWithValue($"@{nameof(ID)}", ID);
            command.Parameters.AddWithValue($"@{nameof(ProcessID)}", ProcessID);
            command.Parameters.AddWithValue($"@{nameof(ProcessStartTime)}", ProcessStartTime.Ticks);
            command.Parameters.AddWithValue($"@{nameof(Originator.Hive)}", Originator.Hive);
            command.Parameters.AddWithValue($"@{nameof(Originator.View)}", Originator.View);
            command.Parameters.AddWithValue($"@{nameof(Originator.SubKey)}", Originator.SubKey);
            command.Parameters.AddWithValue($"@{nameof(Memento.Exists)}", Memento.Exists);
            command.ExecuteNonQuery();
        }

        protected override void Unpersist()
        {
            var command = connection.CreateCommand();
            command.CommandText = $@"DELETE FROM {nameof(PersistentRegistryKeyCaretaker)} WHERE {nameof(ID)} = @{nameof(ID)};";
            command.Parameters.AddWithValue($"@{nameof(ID)}", ID);
            command.ExecuteNonQuery();
        }

        public static IEnumerable<IPersistentSnapshot> GetCaretakers(SqliteConnection connection, IRegistry registry)
        {
            var caretakers = new List<PersistentRegistryKeyCaretaker>();
            var command = connection.CreateCommand();
            command.CommandText = $@"SELECT * FROM {nameof(PersistentRegistryKeyCaretaker)};";
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var caretaker = new PersistentRegistryKeyCaretaker(
                        id: reader.GetString(reader.GetOrdinal(nameof(ID))),
                        processID: reader.GetInt32(reader.GetOrdinal(nameof(ProcessID))),
                        processStartTime: new DateTime(reader.GetInt64(reader.GetOrdinal(nameof(ProcessStartTime)))),
                        originator: new RegistryKeyOriginator(
                            hive: (RegistryHive)reader.GetInt32(reader.GetOrdinal(nameof(RegistryKeyOriginator.Hive))),
                            view: (RegistryView)reader.GetInt32(reader.GetOrdinal(nameof(RegistryKeyOriginator.View))),
                            subKey: reader.GetString(reader.GetOrdinal(nameof(RegistryKeyOriginator.SubKey))),
                            registry: registry
                        ),
                        memento: new RegistryKeyMemento
                        {
                            Exists = reader.GetBoolean(reader.GetOrdinal(nameof(RegistryKeyMemento.Exists)))
                        }
                    );
                    caretakers.Add(caretaker);
                }
            }

            return caretakers;
        }
    }
}
