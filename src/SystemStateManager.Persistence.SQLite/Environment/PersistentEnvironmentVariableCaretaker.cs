using DevOptimal.SystemStateManager.Environment;
using DevOptimal.SystemUtilities.Environment;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace DevOptimal.SystemStateManager.Persistence.SQLite.Environment
{
    internal class PersistentEnvironmentVariableCaretaker : PersistentCaretaker<EnvironmentVariableOriginator, EnvironmentVariableMemento>
    {
        public PersistentEnvironmentVariableCaretaker(string id, EnvironmentVariableOriginator originator, SqliteConnection connection)
            : base(id, originator, connection)
        {
        }

        public PersistentEnvironmentVariableCaretaker(string id, long processID, DateTime processStartTime, EnvironmentVariableOriginator originator, EnvironmentVariableMemento memento)
            : base(id, (int)processID, processStartTime, originator, memento)
        {
        }

        protected override void Initialize()
        {
            var command = connection.CreateCommand();
            command.CommandText =
            $@"CREATE TABLE IF NOT EXISTS {nameof(PersistentEnvironmentVariableCaretaker)} (
                {nameof(ID)} TEXT PRIMARY KEY,
                {nameof(ProcessID)} TEXT,
                {nameof(ProcessStartTime)} INTEGER,
                {nameof(Originator.Name)} TEXT,
                {nameof(Originator.Target)} INTEGER,
                {nameof(Memento.Value)} TEXT
            );";
            command.ExecuteNonQuery();
        }

        protected override void Persist()
        {
            var command = connection.CreateCommand();
            command.CommandText =
            $@"INSERT INTO {nameof(PersistentEnvironmentVariableCaretaker)} (
                {nameof(ID)},
                {nameof(ProcessID)},
                {nameof(ProcessStartTime)},
                {nameof(Originator.Name)},
                {nameof(Originator.Target)},
                {nameof(Memento.Value)}
            ) VALUES (
                @{nameof(ID)},
                @{nameof(ProcessID)},
                @{nameof(ProcessStartTime)},
                @{nameof(Originator.Name)},
                @{nameof(Originator.Target)},
                @{nameof(Memento.Value)}
            );";
            command.Parameters.AddWithValue($"@{nameof(ID)}", ID);
            command.Parameters.AddWithValue($"@{nameof(ProcessID)}", ProcessID);
            command.Parameters.AddWithValue($"@{nameof(ProcessStartTime)}", ProcessStartTime.Ticks);
            command.Parameters.AddWithValue($"@{nameof(Originator.Name)}", Originator.Name);
            command.Parameters.AddWithValue($"@{nameof(Originator.Target)}", Originator.Target);
            command.Parameters.AddWithValue($"@{nameof(Memento.Value)}", Memento.Value);
            command.ExecuteNonQuery();
        }

        protected override void Unpersist()
        {
            var command = connection.CreateCommand();
            command.CommandText = $@"DELETE FROM {nameof(PersistentEnvironmentVariableCaretaker)} WHERE {nameof(ID)} = @{nameof(ID)};";
            command.Parameters.AddWithValue($"@{nameof(ID)}", ID);
            command.ExecuteNonQuery();
        }

        public static IEnumerable<IPersistentSnapshot> GetCaretakers(SqliteConnection connection, IEnvironment environment)
        {
            var caretakers = new List<PersistentEnvironmentVariableCaretaker>();
            var command = connection.CreateCommand();
            command.CommandText = $@"SELECT * FROM {nameof(PersistentEnvironmentVariableCaretaker)};";
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var caretaker = new PersistentEnvironmentVariableCaretaker(
                        id: reader.GetString(reader.GetOrdinal(nameof(ID))),
                        processID: reader.GetInt32(reader.GetOrdinal(nameof(ProcessID))),
                        processStartTime: new DateTime(reader.GetInt64(reader.GetOrdinal(nameof(ProcessStartTime)))),
                        originator: new EnvironmentVariableOriginator(
                            name: reader.GetString(reader.GetOrdinal(nameof(EnvironmentVariableOriginator.Name))),
                            target: (EnvironmentVariableTarget)reader.GetInt32(reader.GetOrdinal(nameof(EnvironmentVariableOriginator.Target))),
                            environment: environment
                        ),
                        memento: new EnvironmentVariableMemento
                        {
                            Value = reader.GetString(reader.GetOrdinal(nameof(EnvironmentVariableMemento.Value)))
                        }
                    );
                    caretakers.Add(caretaker);
                }
            }

            return caretakers;
        }
    }
}
