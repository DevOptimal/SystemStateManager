using DevOptimal.SystemUtilities.Environment;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevOptimal.SystemStateManager.JsonDatabase.Converters
{
    public class EnvironmentJsonConverter : JsonConverter<IEnvironment>
    {
        private readonly IEnvironment environment;

        public EnvironmentJsonConverter(IEnvironment environment)
        {
            this.environment = environment;
        }

        public override IEnvironment Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            reader.Skip();
            return environment;
        }

        public override void Write(Utf8JsonWriter writer, IEnvironment value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }
    }
}
