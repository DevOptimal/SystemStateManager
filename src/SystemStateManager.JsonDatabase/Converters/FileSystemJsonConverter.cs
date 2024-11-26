using DevOptimal.SystemUtilities.FileSystem;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevOptimal.SystemStateManager.JsonDatabase.Converters
{
    public class FileSystemJsonConverter : JsonConverter<IFileSystem>
    {
        private readonly IFileSystem fileSystem;

        public FileSystemJsonConverter(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public override IFileSystem Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            reader.Skip();
            return fileSystem;
        }

        public override void Write(Utf8JsonWriter writer, IFileSystem value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }
    }
}
