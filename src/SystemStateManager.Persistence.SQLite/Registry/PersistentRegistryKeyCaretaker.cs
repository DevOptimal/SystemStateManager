using Dapper;
using DevOptimal.SystemStateManager.Registry;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using System;

namespace DevOptimal.SystemStateManager.Persistence.SQLite.Registry
{
    internal class PersistentRegistryKeyCaretaker : PersistentCaretaker<RegistryKeyOriginator, RegistryKeyMemento>
    {
        public RegistryHive Hive => Originator.Hive;

        public RegistryView View => Originator.View;

        public string SubKey => Originator.SubKey;

        public bool Exists => Memento.Exists;

        public PersistentRegistryKeyCaretaker(string id, RegistryKeyOriginator originator, SqliteConnection connection)
            : base(id, originator, connection)
        {
            connection.Execute($@"CREATE TABLE IF NOT EXISTS {nameof(PersistentRegistryKeyCaretaker)}s (
                {nameof(ID)} TEXT PRIMARY KEY,
                {nameof(ProcessID)} TEXT NOT NULL,
                {nameof(Hive)} INTEGER NOT NULL,
                {nameof(View)} INTEGER NOT NULL,
                {nameof(SubKey)} TEXT NOT NULL,
                {nameof(Exists)} INTEGER NOT NULL
            );");
        }

        public PersistentRegistryKeyCaretaker(string id, string processID, long hive, long view, string subKey, long exists)
            : base(id, processID, new RegistryKeyOriginator((RegistryHive)hive, (RegistryView)view, subKey), new RegistryKeyMemento { Exists = Convert.ToBoolean(exists) })
        {
        }

        protected override void Persist()
        {
            connection.Execute($@"INSERT INTO {nameof(PersistentRegistryKeyCaretaker)}s (
                {nameof(ID)},
                {nameof(ProcessID)},
                {nameof(Hive)},
                {nameof(View)},
                {nameof(SubKey)},
                {nameof(Exists)}
            ) VALUES (
                @{nameof(ID)},
                @{nameof(ProcessID)},
                @{nameof(Hive)},
                @{nameof(View)},
                @{nameof(SubKey)},
                @{nameof(Exists)}
            );", this);
        }

        protected override void Unpersist()
        {
            connection.Execute($@"DELETE FROM {nameof(PersistentRegistryKeyCaretaker)}s WHERE {nameof(ID)} = @{nameof(ID)};", this);
        }
    }
}
