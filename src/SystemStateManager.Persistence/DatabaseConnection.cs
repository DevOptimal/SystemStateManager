using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DevOptimal.SystemStateManager.Persistence
{
    internal class DatabaseConnection
    {
        private readonly FileInfo databaseFile;

        private readonly JsonSerializerOptions serializerOptions;

        public DatabaseConnection(FileInfo databaseFile)
        {
            this.databaseFile = databaseFile;
            serializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
            };
        }

        public void Persist<T>(T snapshot)
            where T : IPersistentSnapshot
        {
            using (var stream = new DatabaseFileStream(databaseFile))
            {
                var database = JsonSerializer.Deserialize<List<IPersistentSnapshot>>(stream, serializerOptions);

                stream.SetLength(0);
                stream.Flush();

                if (database.SingleOrDefault(s => s.ID == snapshot.ID) != null)
                {
                    throw new ResourceLockedException($"The resource '{snapshot.ID}' is locked by another instance.");
                }
                database.Add(snapshot);

                JsonSerializer.Serialize(stream, database, serializerOptions);
            }
        }

        public void Unpersist<T>(T snapshot)
            where T : IPersistentSnapshot
        {
            using (var stream = new DatabaseFileStream(databaseFile))
            {
                var database = JsonSerializer.Deserialize<List<IPersistentSnapshot>>(stream, serializerOptions);

                stream.SetLength(0);
                stream.Flush();

                var existingSnapshot = database.SingleOrDefault(s => s.ID ==  snapshot.ID) ?? throw new KeyNotFoundException($"A snapshot with the ID '{snapshot.ID}' was not found in the database.");
                database.Remove(existingSnapshot);

                JsonSerializer.Serialize(stream, database, serializerOptions);
            }
        }
    }
}
