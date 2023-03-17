using Dapper;
using DevOptimal.SystemStateManager.Environment;
using Microsoft.Data.Sqlite;
using System;

namespace DevOptimal.SystemStateManager.Persistence.SQLite.Environment
{
    internal class PersistentEnvironmentVariableCaretaker : PersistentCaretaker<EnvironmentVariableOriginator, EnvironmentVariableMemento>
    {
        public string Name => Originator.Name;

        public EnvironmentVariableTarget Target => Originator.Target;

        public string Value => Memento.Value;

        public PersistentEnvironmentVariableCaretaker(string id, EnvironmentVariableOriginator originator, SqliteConnection connection)
            : base(id, originator, connection)
        {
            connection.Execute($@"CREATE TABLE IF NOT EXISTS {nameof(PersistentEnvironmentVariableCaretaker)}s (
                {nameof(ID)} TEXT PRIMARY KEY,
                {nameof(ProcessID)} TEXT NOT NULL,
                {nameof(Name)} TEXT NOT NULL,
                {nameof(Target)} INTEGER NOT NULL,
                {nameof(Value)} TEXT
            );");
        }

        public PersistentEnvironmentVariableCaretaker(string id, string processID, string name, long target, string value)
            : base(id, processID, new EnvironmentVariableOriginator(name, (EnvironmentVariableTarget)target), new EnvironmentVariableMemento { Value = value })
        {
        }

        protected override void Persist()
        {
            connection.Execute($@"INSERT INTO {nameof(PersistentEnvironmentVariableCaretaker)}s (
                {nameof(ID)},
                {nameof(ProcessID)},
                {nameof(Name)},
                {nameof(Target)},
                {nameof(Value)}
            ) VALUES (
                @{nameof(ID)},
                @{nameof(ProcessID)},
                @{nameof(Name)},
                @{nameof(Target)},
                @{nameof(Value)}
            );", this);
        }

        protected override void Unpersist()
        {
            connection.Execute($@"DELETE FROM {nameof(PersistentEnvironmentVariableCaretaker)}s WHERE {nameof(ID)} = @{nameof(ID)};", this);
        }
    }
}
