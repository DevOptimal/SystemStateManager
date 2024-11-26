using DevOptimal.SystemStateManager.JsonDatabase.Converters;
using DevOptimal.SystemStateManager.JsonDatabase.Resolvers;
using DevOptimal.SystemUtilities.Environment;
using DevOptimal.SystemUtilities.FileSystem;
using DevOptimal.SystemUtilities.FileSystem.Extensions;
using DevOptimal.SystemUtilities.Registry;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DevOptimal.SystemStateManager.JsonDatabase
{
    public class JsonDatabase : IDatabase
    {
        private readonly FileInfo jsonFile;

        private readonly JsonSerializerOptions jsonSerializerOptions;

        public JsonDatabase(IEnvironment environment, IFileSystem fileSystem, IRegistry registry)
        {
            jsonSerializerOptions = new JsonSerializerOptions
            {
                TypeInfoResolver = new PolymorphicTypeResolver(),
            };
            jsonSerializerOptions.Converters.Add(new EnvironmentJsonConverter(environment));
            jsonSerializerOptions.Converters.Add(new FileSystemJsonConverter(fileSystem));
            jsonSerializerOptions.Converters.Add(new RegistryJsonConverter(registry));
            jsonSerializerOptions.Converters.Add(new DatabaseJsonConverter(this));
            jsonFile = new FileInfo(@"C:\temp\database.json");
            if (!jsonFile.Exists)
            {
                jsonFile.WriteAllText("[]");
            }
        }

        public void AddSnapshot(ISnapshot snapshot)
        {
            using (var stream = jsonFile.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var snapshots = JsonSerializer.Deserialize<List<ISnapshot>>(stream, jsonSerializerOptions);
                snapshots.Add(snapshot);
                stream.Position = 0;
                stream.SetLength(0);
                JsonSerializer.Serialize(stream, snapshots, jsonSerializerOptions);
            }
        }

        public ISnapshot GetSnapshot(string id)
        {
            using (var stream = jsonFile.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var snapshots = JsonSerializer.Deserialize<List<ISnapshot>>(stream, jsonSerializerOptions);
                return snapshots.FirstOrDefault(s => s.ID == id);
            }
        }

        public IEnumerable<ISnapshot> GetSnapshots()
        {
            using (var stream = jsonFile.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                return JsonSerializer.Deserialize<List<ISnapshot>>(stream, jsonSerializerOptions);
            }
        }

        public void RemoveSnapshot(ISnapshot snapshot)
        {
            using (var stream = jsonFile.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var snapshots = JsonSerializer.Deserialize<List<ISnapshot>>(stream, jsonSerializerOptions);
                if (snapshots.Remove(snapshots?.SingleOrDefault(s => s.ID == snapshot.ID)))
                {
                    stream.Position = 0;
                    stream.SetLength(0);
                    JsonSerializer.Serialize(stream, snapshots, jsonSerializerOptions);
                }
            }
        }
    }
}
