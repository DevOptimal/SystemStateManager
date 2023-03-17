using Dapper;
using DevOptimal.SystemStateManager.FileSystem;
using Microsoft.Data.Sqlite;

namespace DevOptimal.SystemStateManager.Persistence.SQLite.FileSystem
{
    internal class PersistentFileCaretaker : PersistentCaretaker<FileOriginator, FileMemento>
    {
        public string Path => Originator.Path;

        public string Hash => Memento.Hash;

        public PersistentFileCaretaker(string id, FileOriginator originator, SqliteConnection connection)
            : base(id, originator, connection)
        {
            connection.Execute($@"CREATE TABLE IF NOT EXISTS {nameof(PersistentFileCaretaker)}s (
                {nameof(ID)} TEXT PRIMARY KEY,
                {nameof(ProcessID)} TEXT NOT NULL,
                {nameof(Path)} TEXT NOT NULL,
                {nameof(Hash)} TEXT NOT NULL
            );");
        }

        public PersistentFileCaretaker(string id, string processID, string path, string hash)
            : base(id, processID, new FileOriginator(path), new FileMemento { Hash = hash })
        {
        }

        protected override void Persist()
        {
            connection.Execute($@"INSERT INTO {nameof(PersistentFileCaretaker)}s (
                {nameof(ID)},
                {nameof(ProcessID)},
                {nameof(Path)},
                {nameof(Hash)}
            ) VALUES (
                @{nameof(ID)},
                @{nameof(ProcessID)},
                @{nameof(Path)},
                @{nameof(Hash)}
            );", this);
        }

        protected override void Unpersist()
        {
            connection.Execute($@"DELETE FROM {nameof(PersistentFileCaretaker)}s WHERE {nameof(ID)} = @{nameof(ID)};", this);
        }
    }
}
