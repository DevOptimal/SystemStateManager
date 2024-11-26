using DevOptimal.SystemStateManager.Environment;
using DevOptimal.SystemStateManager.FileSystem;
using DevOptimal.SystemStateManager.Registry;
using DevOptimal.SystemUtilities.Environment;
using DevOptimal.SystemUtilities.FileSystem;
using DevOptimal.SystemUtilities.Registry;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DevOptimal.SystemStateManager.JsonDatabase
{
    /// <summary>
    /// Goal of this parser is to be able to save on memory by streaming snapshots instead of loading all of them into memory at once.
    /// To accomplish this, we parse and return yield snapshots as we read them from the stream.
    /// Reference: https://www.json.org/json-en.html
    /// </summary>
    public class MyJsonDatabase : IDatabase
    {
        private const string resourceTypePropertyName = "$resource-type";
        private const string environmentVariableResourceTypeName = "EnvironmentVariable";
        private const string directoryResourceTypeName = "Directory";
        private const string fileResourceTypeName = "File";
        private const string registryKeyResourceTypeName = "RegistryKey";
        private const string registryValueResourceTypeName = "RegistryValue";

        private readonly IEnvironment environment;
        private readonly IFileSystem fileSystem;
        private readonly IRegistry registry;
        private readonly IFileCache fileCache;

        private readonly FileInfo databaseFile = new FileInfo(@"C:\temp\database.json");

        public MyJsonDatabase(IEnvironment environment, IFileSystem fileSystem, IRegistry registry, IFileCache fileCache)
        {
            this.environment = environment;
            this.fileSystem = fileSystem;
            this.registry = registry;
            this.fileCache = fileCache;
        }

        public void AddSnapshot(ISnapshot snapshot)
        {
            WriteSnapshots(ReadSnapshots().Concat(new[] { snapshot }));
        }

        public ISnapshot GetSnapshot(string id)
        {
            return ReadSnapshots().First(s => s.ID == id);
        }

        public IEnumerable<ISnapshot> GetSnapshots()
        {
            return ReadSnapshots();
        }

        public void RemoveSnapshot(ISnapshot snapshot)
        {
            WriteSnapshots(ReadSnapshots().Except(new[] { snapshot }));
        }

        private Stream OpenDatabaseFile(FileAccess access)
        {
            return File.Open(databaseFile.FullName, FileMode.OpenOrCreate, access, FileShare.None);
        }

        #region Snapshot Methods

        private IEnumerable<ISnapshot> ReadSnapshots()
        {
            using (var stream = OpenDatabaseFile(FileAccess.Read))
            using (var reader = new StreamReader(stream))
            {
                foreach (var o in EnumerateArray(reader))
                {
                    if (o is IDictionary<string, object> d)
                    {
                        // Get snapshot fields
                        var id = AsString(d[nameof(ISnapshot.ID)]);
                        var processId = AsInteger(d[nameof(ISnapshot.ProcessID)]);
                        var processStartTime = AsDateTime(d[nameof(ISnapshot.ProcessStartTime)]);

                        switch (d[resourceTypePropertyName])
                        {
                            case environmentVariableResourceTypeName:
                                var environmentVariableName = AsString(d[nameof(EnvironmentVariableOriginator.Name)]);
                                var environmentVariableTarget = AsEnum<EnvironmentVariableTarget>(d[nameof(EnvironmentVariableOriginator.Target)]);
                                var environmentVariableValue = AsString(d[nameof(EnvironmentVariableMemento.Value)]);

                                var environmentVariableOriginator = new EnvironmentVariableOriginator(environmentVariableName, environmentVariableTarget, environment);
                                var environmentVariableMemento = new EnvironmentVariableMemento
                                {
                                    Value = environmentVariableValue
                                };

                                yield return new Caretaker<EnvironmentVariableOriginator, EnvironmentVariableMemento>(id, processId, processStartTime, this, environmentVariableOriginator, environmentVariableMemento);
                                break;
                            case directoryResourceTypeName:
                                var directoryPath = AsString(d[nameof(DirectoryOriginator.Path)]);
                                var directoryExists = AsBoolean(d[nameof(DirectoryMemento.Exists)]);

                                var directoryOriginator = new DirectoryOriginator(directoryPath, fileSystem);
                                var directoryMemento = new DirectoryMemento
                                {
                                    Exists = directoryExists
                                };

                                yield return new Caretaker<DirectoryOriginator, DirectoryMemento>(id, processId, processStartTime, this, directoryOriginator, directoryMemento);
                                break;
                            case fileResourceTypeName:
                                var filePath = AsString(d[nameof(FileOriginator.Path)]);
                                var fileHash = AsString(d[nameof(FileMemento.Hash)]);

                                var fileOriginator = new FileOriginator(filePath, fileCache, fileSystem);
                                var fileMemento = new FileMemento
                                {
                                    Hash = fileHash
                                };

                                yield return new Caretaker<FileOriginator, FileMemento>(id, processId, processStartTime, this, fileOriginator, fileMemento);
                                break;
                            case registryKeyResourceTypeName:
                                var registryKeyHive = AsEnum<RegistryHive>(d[nameof(RegistryKeyOriginator.Hive)]);
                                var registryKeyView = AsEnum<RegistryView>(d[nameof(RegistryKeyOriginator.View)]);
                                var registryKeySubKey = AsString(d[nameof(RegistryKeyOriginator.SubKey)]);
                                var registryKeyExists = AsBoolean(d[nameof(RegistryKeyMemento.Exists)]);

                                var registryKeyOriginator = new RegistryKeyOriginator(registryKeyHive, registryKeyView, registryKeySubKey, registry);
                                var registryKeyMemento = new RegistryKeyMemento
                                {
                                    Exists = registryKeyExists
                                };

                                yield return new Caretaker<RegistryKeyOriginator, RegistryKeyMemento>(id, processId, processStartTime, this, registryKeyOriginator, registryKeyMemento);
                                break;
                            case registryValueResourceTypeName:
                                var registryValueHive = AsEnum<RegistryHive>(d[nameof(RegistryValueOriginator.Hive)]);
                                var registryValueView = AsEnum<RegistryView>(d[nameof(RegistryValueOriginator.View)]);
                                var registryValueSubKey = AsString(d[nameof(RegistryValueOriginator.SubKey)]);
                                var registryValueName = AsString(d[nameof(RegistryValueOriginator.Name)]);
                                var registryValueValue = ConvertToRegistryValue(d[nameof(RegistryValueMemento.Value)]);
                                var registryValueKind = AsEnum<RegistryValueKind>(d[nameof(RegistryValueMemento.Kind)]);

                                var registryValueOriginator = new RegistryValueOriginator(registryValueHive, registryValueView, registryValueSubKey, registryValueName, registry);
                                var registryValueMemento = new RegistryValueMemento
                                {
                                    Value = registryValueValue,
                                    Kind = registryValueKind
                                };

                                yield return new Caretaker<RegistryValueOriginator, RegistryValueMemento>(id, processId, processStartTime, this, registryValueOriginator, registryValueMemento);
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
        }

        private void WriteSnapshots(IEnumerable<ISnapshot> snapshots)
        {
            using (var stream = OpenDatabaseFile(FileAccess.Write))
            using (var writer = new StreamWriter(stream))
            {
                WriteArray(writer, ConvertToDictionary(snapshots));
            }
        }

        private IEnumerable<object> ConvertToDictionary(IEnumerable<ISnapshot> snapshots)
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

        private object ConvertToRegistryValue(object o)
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

        private bool AsBoolean(object o)
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

        private DateTime AsDateTime(object o)
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

        private T AsEnum<T>(object o) where T : struct, IConvertible
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

        private int AsInteger(object o)
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

        private string AsString(object o)
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

        #region Read Methods

        private char PeekChar(StreamReader reader)
        {
            return (char)reader.Peek();
        }

        private char ReadChar(StreamReader reader)
        {
            return (char)reader.Read();
        }

        private void AssertChar(StreamReader reader, params char[] c)
        {
            foreach (var ch in c)
            {
                if (ch != ReadChar(reader))
                {
                    throw new Exception();
                }
            }
        }

        private void ReadWhiteSpace(StreamReader reader)
        {
            var whiteSpaceCharacters = new List<char> { ' ', '\n', '\r', '\t' };
            while (whiteSpaceCharacters.Contains(PeekChar(reader)))
            {
                ReadChar(reader);
            }
        }

        private object ReadNumber(StreamReader reader)
        {
            var digitCharacters = new List<char> { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

            var sb = new StringBuilder();

            if (PeekChar(reader) == '-')
            {
                sb.Append(ReadChar(reader));
            }

            if (PeekChar(reader) == '0')
            {
                sb.Append(ReadChar(reader));
            }
            else if (digitCharacters.Contains(PeekChar(reader)))
            {
                do
                {
                    sb.Append(ReadChar(reader));
                } while (digitCharacters.Contains(PeekChar(reader)));
            }
            else
            {
                throw new Exception();
            }

            if (PeekChar(reader) == '.')
            {
                sb.Append(ReadChar(reader));
                if (!digitCharacters.Contains(PeekChar(reader)))
                {
                    throw new Exception();
                }

                do
                {
                    sb.Append(ReadChar(reader));
                } while (digitCharacters.Contains(PeekChar(reader)));
            }

            var exponentCharacters = new List<char> { 'e', 'E' };
            if (exponentCharacters.Contains(PeekChar(reader)))
            {
                sb.Append(ReadChar(reader));

                var signCharacters = new List<char> { '+', '-' };
                if (signCharacters.Contains(PeekChar(reader)))
                {
                    sb.Append(ReadChar(reader));
                }

                if (!digitCharacters.Contains(PeekChar(reader)))
                {
                    throw new Exception();
                }

                do
                {
                    sb.Append(ReadChar(reader));
                } while (digitCharacters.Contains(PeekChar(reader)));
            }

            if (int.TryParse(sb.ToString(), out var intResult))
            {
                return intResult;
            }
            else if (long.TryParse(sb.ToString(), out var longResult))
            {
                return longResult;
            }
            else if (float.TryParse(sb.ToString(), out var floatResult))
            {
                return floatResult;
            }
            else if (double.TryParse(sb.ToString(), out var doubleResult))
            {
                return doubleResult;
            }
            else if (decimal.TryParse(sb.ToString(), out var decimalResult))
            {
                return decimalResult;
            }
            else
            {
                throw new Exception();
            }
        }

        private string ReadString(StreamReader reader)
        {
            var sb = new StringBuilder();

            AssertChar(reader, '"');

            while (true)
            {
                switch (PeekChar(reader))
                {
                    case '"':
                        ReadChar(reader);
                        return sb.ToString();
                    case '\\':
                        ReadChar(reader);
                        switch (ReadChar(reader))
                        {
                            case '"':
                                sb.Append('"');
                                break;
                            case '\\':
                                sb.Append('\\');
                                break;
                            case '/':
                                sb.Append('/');
                                break;
                            case 'b':
                                sb.Append('\b');
                                break;
                            case 'f':
                                sb.Append('\f');
                                break;
                            case 'n':
                                sb.Append('\n');
                                break;
                            case 'r':
                                sb.Append('\r');
                                break;
                            case 't':
                                sb.Append('\t');
                                break;
                            case 'u':
                                var hexDigitCharacters = new List<char> { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'A', 'b', 'B', 'c', 'C', 'd', 'D', 'e', 'E', 'f', 'F' };
                                var hexStringBuilder = new StringBuilder();
                                for (var i = 0; i < 4; i++)
                                {
                                    if (!hexDigitCharacters.Contains(PeekChar(reader)))
                                    {
                                        throw new Exception();
                                    }

                                    hexStringBuilder.Append(ReadChar(reader));
                                }
                                sb.Append((char)int.Parse(hexStringBuilder.ToString(), System.Globalization.NumberStyles.HexNumber));
                                break;
                            default:
                                throw new Exception();
                        }
                        break;
                    default:
                        sb.Append(ReadChar(reader));
                        break;
                }
            }
        }

        private object ReadValue(StreamReader reader)
        {
            ReadWhiteSpace(reader);

            object result;
            switch (PeekChar(reader))
            {
                case '"':
                    result = ReadString(reader);
                    break;
                case '-':
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    result = ReadNumber(reader);
                    break;
                case '{':
                    result = ReadObject(reader);
                    break;
                case '[':
                    result = ReadArray(reader);
                    break;
                case 't':
                    AssertChar(reader, 't', 'r', 'u', 'e');
                    result = true;
                    break;
                case 'f':
                    AssertChar(reader, 'f', 'a', 'l', 's', 'e');
                    result = false;
                    break;
                case 'n':
                    AssertChar(reader, 'n', 'u', 'l', 'l');
                    result = null;
                    break;
                default: throw new Exception();
            }

            ReadWhiteSpace(reader);

            return result;
        }

        private object[] ReadArray(StreamReader reader)
        {
            return EnumerateArray(reader).ToArray();
        }

        private IEnumerable<object> EnumerateArray(StreamReader reader)
        {
            AssertChar(reader, '[');

            ReadWhiteSpace(reader);

            while (true)
            {
                yield return ReadValue(reader);

                if (PeekChar(reader) == ',')
                {
                    ReadChar(reader);
                }
                else
                {
                    break;
                }
            }

            AssertChar(reader, ']');
        }

        private IDictionary<string, object> ReadObject(StreamReader reader)
        {
            var result = new Dictionary<string, object>();

            AssertChar(reader, '{');

            ReadWhiteSpace(reader);

            while (true)
            {
                ReadWhiteSpace(reader);

                var key = ReadString(reader);

                ReadWhiteSpace(reader);

                AssertChar(reader, ':');

                var value = ReadValue(reader);

                result.Add(key, value);

                if (PeekChar(reader) == ',')
                {
                    ReadChar(reader);
                }
                else
                {
                    break;
                }
            }

            AssertChar(reader, '}');

            return result;
        }

        #endregion

        #region Write Methods

        private void WriteString(StreamWriter writer, string value)
        {
            var escapedValue = value
                .Replace("\"", "\\\"")
                .Replace("\\", "\\\\")
                .Replace("/", "\\/")
                .Replace("\b", "\\b")
                .Replace("\f", "\\f")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
            writer.Write($"\"{escapedValue}\"");
        }

        private void WriteNumber(StreamWriter writer, int value)
        {
            writer.Write(value);
        }

        private void WriteNumber(StreamWriter writer, long value)
        {
            writer.Write(value);
        }

        private void WriteNumber(StreamWriter writer, float value)
        {
            writer.Write(value);
        }

        private void WriteNumber(StreamWriter writer, double value)
        {
            writer.Write(value);
        }

        private void WriteNumber(StreamWriter writer, decimal value)
        {
            writer.Write(value);
        }

        private void WriteValue(StreamWriter writer, object value)
        {
            if (value == null)
            {
                writer.Write("null");
            }
            else
            {
                switch (value)
                {
                    case string s:
                        WriteString(writer, s);
                        break;
                    case int i:
                        WriteNumber(writer, i);
                        break;
                    case long l:
                        WriteNumber(writer, l);
                        break;
                    case float f:
                        WriteNumber(writer, f);
                        break;
                    case double d:
                        WriteNumber(writer, d);
                        break;
                    case decimal e:
                        WriteNumber(writer, e);
                        break;
                    case IDictionary<string, object> o:
                        WriteObject(writer, o);
                        break;
                    case IEnumerable<object> a:
                        WriteArray(writer, a);
                        break;
                    case bool b:
                        writer.Write(b ? "true" : "false");
                        break;
                    default: throw new Exception();
                }
            }
        }

        private void WriteArray(StreamWriter writer, IEnumerable<object> value)
        {
            writer.Write("[");

            var firstItem = true;
            foreach (var item in value)
            {
                if (firstItem)
                {
                    firstItem = false;
                }
                else
                {
                    writer.Write(",");
                }

                writer.Write(" ");

                WriteValue(writer, item);
            }

            writer.Write(" ]");
        }

        private void WriteObject(StreamWriter writer, IDictionary<string, object> value)
        {
            writer.Write("{");

            var firstItem = true;
            foreach (var item in value)
            {
                if (firstItem)
                {
                    firstItem = false;
                }
                else
                {
                    writer.Write(",");
                }

                writer.Write(" ");
                WriteString(writer, item.Key);
                writer.Write(": ");

                WriteValue(writer, item.Value);
            }

            writer.Write(" }");
        }

        #endregion
    }
}
