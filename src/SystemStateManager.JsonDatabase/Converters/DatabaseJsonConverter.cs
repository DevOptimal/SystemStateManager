using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevOptimal.SystemStateManager.JsonDatabase.Converters
{
    public class DatabaseJsonConverter : JsonConverter<IDatabase>
    {
        private readonly IDatabase database;

        public DatabaseJsonConverter(IDatabase database)
        {
            this.database = database;
        }

        public override IDatabase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            reader.Skip();
            return database;
        }

        public override void Write(Utf8JsonWriter writer, IDatabase value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }
    }
}
