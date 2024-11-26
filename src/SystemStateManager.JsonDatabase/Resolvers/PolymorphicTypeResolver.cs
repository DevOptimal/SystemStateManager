using DevOptimal.SystemStateManager.Environment;
using DevOptimal.SystemStateManager.FileSystem;
using DevOptimal.SystemStateManager.Registry;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace DevOptimal.SystemStateManager.JsonDatabase.Resolvers
{
    public class PolymorphicTypeResolver : DefaultJsonTypeInfoResolver
    {
        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            JsonTypeInfo jsonTypeInfo = base.GetTypeInfo(type, options);

            if (jsonTypeInfo.Type == typeof(ISnapshot))
            {
                jsonTypeInfo.PolymorphismOptions = new JsonPolymorphismOptions
                {
                    TypeDiscriminatorPropertyName = "$resource-type",
                    IgnoreUnrecognizedTypeDiscriminators = true,
                    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization,
                    DerivedTypes =
                    {
                        new JsonDerivedType(typeof(Caretaker<EnvironmentVariableOriginator, EnvironmentVariableMemento>), "EnvironmentVariable"),
                        new JsonDerivedType(typeof(Caretaker<DirectoryOriginator, DirectoryMemento>), "Directory"),
                        new JsonDerivedType(typeof(Caretaker<FileOriginator, FileMemento>), "File"),
                        new JsonDerivedType(typeof(Caretaker<RegistryKeyOriginator, RegistryKeyMemento>), "RegistryKey"),
                        new JsonDerivedType(typeof(Caretaker<RegistryValueOriginator, RegistryValueMemento>), "RegistryValue"),
                    }
                };
            }

            return jsonTypeInfo;
        }
    }
}
