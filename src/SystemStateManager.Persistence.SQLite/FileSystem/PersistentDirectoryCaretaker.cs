using Dapper;
using DevOptimal.SystemStateManager.FileSystem;
using Microsoft.Data.Sqlite;
using System;

namespace DevOptimal.SystemStateManager.Persistence.SQLite.FileSystem
{
    internal class PersistentDirectoryCaretaker : PersistentCaretaker<DirectoryOriginator, DirectoryMemento>
    {
        public string Path => Originator.Path;

        public bool Exists => Memento.Exists;

        public PersistentDirectoryCaretaker(string id, DirectoryOriginator originator, SqliteConnection connection)
            : base(id, originator, connection)
        {
            connection.Execute($@"CREATE TABLE IF NOT EXISTS {nameof(PersistentDirectoryCaretaker)}s (
                {nameof(ID)} TEXT PRIMARY KEY,
                {nameof(ProcessID)} TEXT NOT NULL,
                {nameof(Path)} TEXT NOT NULL,
                {nameof(Exists)} INTEGER NOT NULL
            );");
        }

        public PersistentDirectoryCaretaker(string id, string processID, string path, long exists)
            : base(id, processID, new DirectoryOriginator(path), new DirectoryMemento { Exists = Convert.ToBoolean(exists) })
        {
        }

        protected override void Persist()
        {
            connection.Execute($@"INSERT INTO {nameof(PersistentDirectoryCaretaker)}s (
                {nameof(ID)},
                {nameof(ProcessID)},
                {nameof(Path)},
                {nameof(Exists)}
            ) VALUES (
                @{nameof(ID)},
                @{nameof(ProcessID)},
                @{nameof(Path)},
                @{nameof(Exists)}
            );", this);
        }

        protected override void Unpersist()
        {
            connection.Execute($@"DELETE FROM {nameof(PersistentDirectoryCaretaker)}s WHERE {nameof(ID)} = @{nameof(ID)};", this);
        }
    }
}
