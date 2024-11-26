using DevOptimal.SystemUtilities.Registry;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevOptimal.SystemStateManager.JsonDatabase.Converters
{
    public class RegistryJsonConverter : JsonConverter<IRegistry>
    {
        private readonly IRegistry registry;

        public RegistryJsonConverter(IRegistry registry)
        {
            this.registry = registry;
        }

        public override IRegistry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            reader.Skip();
            return registry;
        }

        public override void Write(Utf8JsonWriter writer, IRegistry value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }
    }
}
