using DevOptimal.SystemStateManager.Environment;
using DevOptimal.SystemStateManager.FileSystem;
using DevOptimal.SystemStateManager.Registry;
using DevOptimal.SystemUtilities.Environment.Abstractions;
using DevOptimal.SystemUtilities.FileSystem.Abstractions;
using DevOptimal.SystemUtilities.Registry.Abstractions;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DevOptimal.SystemStateManager.JsonDatabase.Serialization
{
    /// <summary>
    /// Goal of this serializer is efficient resource usage by streaming snapshots instead of loading all of them into memory at once.
    /// To accomplish this, we parse and yield return snapshots as we read them from the JSON stream.
    /// </summary>
    internal static class SnapshotSerializer
    {
        private const string resourceTypePropertyName = "$resource-type";
        private const string environmentVariableResourceTypeName = "EnvironmentVariable";
        private const string directoryResourceTypeName = "Directory";
        private const string fileResourceTypeName = "File";
        private const string registryKeyResourceTypeName = "RegistryKey";
        private const string registryValueResourceTypeName = "RegistryValue";

        #region Serializer Methods

        public static IEnumerable<ISnapshot> ReadSnapshots(this JsonReader reader, IDatabase database, IEnvironment environment, IFileSystem fileSystem, IRegistry registry, IFileCache fileCache)
        {
            foreach (var item in reader.EnumerateArray())
            {
                if (item is IDictionary<string, object> dictionary)
                {
                    // Get snapshot fields
                    var id = AsString(dictionary[nameof(ISnapshot.ID)]);
                    var processId = AsInteger(dictionary[nameof(ISnapshot.ProcessID)]);
                    var processStartTime = AsDateTime(dictionary[nameof(ISnapshot.ProcessStartTime)]);

                    switch (dictionary[resourceTypePropertyName])
                    {
                        case environmentVariableResourceTypeName:
                            var environmentVariableName = AsString(dictionary[nameof(EnvironmentVariableOriginator.Name)]);
                            var environmentVariableTarget = AsEnum<EnvironmentVariableTarget>(dictionary[nameof(EnvironmentVariableOriginator.Target)]);
                            var environmentVariableValue = AsString(dictionary[nameof(EnvironmentVariableMemento.Value)]);

                            var environmentVariableOriginator = new EnvironmentVariableOriginator(environmentVariableName, environmentVariableTarget, environment);
                            var environmentVariableMemento = new EnvironmentVariableMemento
                            {
                                Value = environmentVariableValue
                            };

                            yield return new Caretaker<EnvironmentVariableOriginator, EnvironmentVariableMemento>(id, processId, processStartTime, database, environmentVariableOriginator, environmentVariableMemento);
                            break;
                        case directoryResourceTypeName:
                            var directoryPath = AsString(dictionary[nameof(DirectoryOriginator.Path)]);
                            var directoryExists = AsBoolean(dictionary[nameof(DirectoryMemento.Exists)]);

                            var directoryOriginator = new DirectoryOriginator(directoryPath, fileSystem);
                            var directoryMemento = new DirectoryMemento
                            {
                                Exists = directoryExists
                            };

                            yield return new Caretaker<DirectoryOriginator, DirectoryMemento>(id, processId, processStartTime, database, directoryOriginator, directoryMemento);
                            break;
                        case fileResourceTypeName:
                            var filePath = AsString(dictionary[nameof(FileOriginator.Path)]);
                            var fileHash = AsString(dictionary[nameof(FileMemento.Hash)]);

                            var fileOriginator = new FileOriginator(filePath, fileCache, fileSystem);
                            var fileMemento = new FileMemento
                            {
                                Hash = fileHash
                            };

                            yield return new Caretaker<FileOriginator, FileMemento>(id, processId, processStartTime, database, fileOriginator, fileMemento);
                            break;
                        case registryKeyResourceTypeName:
                            var registryKeyHive = AsEnum<RegistryHive>(dictionary[nameof(RegistryKeyOriginator.Hive)]);
                            var registryKeyView = AsEnum<RegistryView>(dictionary[nameof(RegistryKeyOriginator.View)]);
                            var registryKeySubKey = AsString(dictionary[nameof(RegistryKeyOriginator.SubKey)]);
                            var registryKeyExists = AsBoolean(dictionary[nameof(RegistryKeyMemento.Exists)]);

                            var registryKeyOriginator = new RegistryKeyOriginator(registryKeyHive, registryKeyView, registryKeySubKey, registry);
                            var registryKeyMemento = new RegistryKeyMemento
                            {
                                Exists = registryKeyExists
                            };

                            yield return new Caretaker<RegistryKeyOriginator, RegistryKeyMemento>(id, processId, processStartTime, database, registryKeyOriginator, registryKeyMemento);
                            break;
                        case registryValueResourceTypeName:
                            var registryValueHive = AsEnum<RegistryHive>(dictionary[nameof(RegistryValueOriginator.Hive)]);
                            var registryValueView = AsEnum<RegistryView>(dictionary[nameof(RegistryValueOriginator.View)]);
                            var registryValueSubKey = AsString(dictionary[nameof(RegistryValueOriginator.SubKey)]);
                            var registryValueName = AsString(dictionary[nameof(RegistryValueOriginator.Name)]);
                            var registryValueValue = ConvertToRegistryValue(dictionary[nameof(RegistryValueMemento.Value)]);
                            var registryValueKind = AsEnum<RegistryValueKind>(dictionary[nameof(RegistryValueMemento.Kind)]);

                            var registryValueOriginator = new RegistryValueOriginator(registryValueHive, registryValueView, registryValueSubKey, registryValueName, registry);
                            var registryValueMemento = new RegistryValueMemento
                            {
                                Value = registryValueValue,
                                Kind = registryValueKind
                            };

                            yield return new Caretaker<RegistryValueOriginator, RegistryValueMemento>(id, processId, processStartTime, database, registryValueOriginator, registryValueMemento);
                            break;
                        default: throw new Exception();
                    }
                }
                else
                {
                    throw new Exception();
                }
            }
        }

        public static void WriteSnapshots(this JsonWriter writer, IEnumerable<ISnapshot> snapshots)
        {
            writer.WriteArray(ConvertSnapshotToDictionary(snapshots));
        }

        private static IEnumerable<IDictionary<string, object>> ConvertSnapshotToDictionary(IEnumerable<ISnapshot> snapshots)
        {
            foreach (var snapshot in snapshots)
            {
                var result = new Dictionary<string, object>
                {
                    [nameof(ISnapshot.ID)] = snapshot.ID,
                    [nameof(ISnapshot.ProcessID)] = snapshot.ProcessID,
                    [nameof(ISnapshot.ProcessStartTime)] = snapshot.ProcessStartTime.Ticks
                };

                switch (snapshot)
                {
                    case Caretaker<EnvironmentVariableOriginator, EnvironmentVariableMemento> environmentVariableCaretaker:
                        result[resourceTypePropertyName] = environmentVariableResourceTypeName;
                        result[nameof(EnvironmentVariableOriginator.Name)] = environmentVariableCaretaker.Originator.Name;
                        result[nameof(EnvironmentVariableOriginator.Target)] = environmentVariableCaretaker.Originator.Target.ToString();
                        result[nameof(EnvironmentVariableMemento.Value)] = environmentVariableCaretaker.Memento.Value;
                        break;
                    case Caretaker<DirectoryOriginator, DirectoryMemento> directoryCaretaker:
                        result[resourceTypePropertyName] = directoryResourceTypeName;
                        result[nameof(DirectoryOriginator.Path)] = directoryCaretaker.Originator.Path;
                        result[nameof(DirectoryMemento.Exists)] = directoryCaretaker.Memento.Exists;
                        break;
                    case Caretaker<FileOriginator, FileMemento> fileCaretaker:
                        result[resourceTypePropertyName] = fileResourceTypeName;
                        result[nameof(FileOriginator.Path)] = fileCaretaker.Originator.Path;
                        result[nameof(FileMemento.Hash)] = fileCaretaker.Memento.Hash;
                        break;
                    case Caretaker<RegistryKeyOriginator, RegistryKeyMemento> registryKeyCaretaker:
                        result[resourceTypePropertyName] = registryKeyResourceTypeName;
                        result[nameof(RegistryKeyOriginator.Hive)] = registryKeyCaretaker.Originator.Hive.ToString();
                        result[nameof(RegistryKeyOriginator.View)] = registryKeyCaretaker.Originator.View.ToString();
                        result[nameof(RegistryKeyOriginator.SubKey)] = registryKeyCaretaker.Originator.SubKey;
                        result[nameof(RegistryKeyMemento.Exists)] = registryKeyCaretaker.Memento.Exists;
                        break;
                    case Caretaker<RegistryValueOriginator, RegistryValueMemento> registryValueCaretaker:
                        result[resourceTypePropertyName] = registryValueResourceTypeName;
                        result[nameof(RegistryValueOriginator.Hive)] = registryValueCaretaker.Originator.Hive.ToString();
                        result[nameof(RegistryValueOriginator.View)] = registryValueCaretaker.Originator.View.ToString();
                        result[nameof(RegistryValueOriginator.SubKey)] = registryValueCaretaker.Originator.SubKey;
                        result[nameof(RegistryValueOriginator.Name)] = registryValueCaretaker.Originator.Name;
                        result[nameof(RegistryValueMemento.Value)] = ConvertFromRegistryValue(registryValueCaretaker.Memento.Value);
                        result[nameof(RegistryValueMemento.Kind)] = registryValueCaretaker.Memento.Kind.ToString();
                        break;
                }

                yield return result;
            }
        }

        #endregion

        #region Conversion Methods

        private static string ConvertFromRegistryValue(object value)
        {
            if (value == null)
            {
                return null;
            }

            var result = new List<byte>();
            switch (value)
            {
                case byte[] byteValue:
                    result.Add(0x0);
                    result.AddRange(byteValue);
                    break;
                case int intValue:
                    result.Add(0x1);
                    result.AddRange(BitConverter.GetBytes(intValue));
                    break;
                case long longValue:
                    result.Add(0x2);
                    result.AddRange(BitConverter.GetBytes(longValue));
                    break;
                case string stringValue:
                    result.Add(0x3);
                    result.AddRange(Encoding.ASCII.GetBytes(stringValue));
                    break;
                case string[] stringArrayValue:
                    result.Add(0x4);
                    foreach (var stringValue in stringArrayValue)
                    {
                        result.AddRange(Encoding.ASCII.GetBytes(stringValue));
                        result.Add(0x0);
                    }
                    break;
                default:
                    throw new NotSupportedException($"{value.GetType().Name} is not a supported registry type.");
            }
            return Convert.ToBase64String(result.ToArray());
        }

        private static object ConvertToRegistryValue(object o)
        {
            if (o == null)
            {
                return null;
            }

            if (o is string s)
            {
                var bytes = Convert.FromBase64String(s);

                switch (bytes[0])
                {
                    case 0x0:
                        return bytes.Skip(1).ToArray();
                    case 0x1:
                        return BitConverter.ToInt32(bytes, 1);
                    case 0x2:
                        return BitConverter.ToInt64(bytes, 1);
                    case 0x3:
                        return Encoding.ASCII.GetString(bytes, 1, bytes.Length - 1);
                    case 0x4:
                        var result = new List<string>();
                        var currentStartIndex = 1;
                        for (var i = currentStartIndex; i < bytes.Length; i++)
                        {
                            if (bytes[i] == 0x0)
                            {
                                result.Add(Encoding.ASCII.GetString(bytes, currentStartIndex, i - currentStartIndex));
                                currentStartIndex = i + 1;
                            }
                        }
                        return result.ToArray();
                    default:
                        throw new NotSupportedException($"Unknown type byte.");
                }
            }
            else
            {
                throw new Exception();
            }
        }

        #endregion

        #region Cast Methods

        private static bool AsBoolean(object o)
        {
            if (o is bool b)
            {
                return b;
            }
            else
            {
                throw new Exception();
            }
        }

        private static DateTime AsDateTime(object o)
        {
            if (o is DateTime d)
            {
                return d;
            }
            else if (o is string s)
            {
                return DateTime.Parse(s);
            }
            else if (o is long l)
            {
                return new DateTime(l);
            }
            else
            {
                throw new Exception();
            }
        }

        private static T AsEnum<T>(object o) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException($"{typeof(T).Name} is not an enumerated type.");
            }

            if (o is T t)
            {
                return t;
            }
            else if (o is int i)
            {
                return (T)(object)i;
            }
            else if (o is string s)
            {
                return (T)Enum.Parse(typeof(T), s);
            }
            else
            {
                throw new Exception();
            }
        }

        private static int AsInteger(object o)
        {
            if (o is int i)
            {
                return i;
            }
            else
            {
                throw new Exception();
            }
        }

        private static string AsString(object o)
        {
            if (o is string s)
            {
                return s;
            }
            else
            {
                throw new Exception();
            }
        }

        #endregion
    }
}
