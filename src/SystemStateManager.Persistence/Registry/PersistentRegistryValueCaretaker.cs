using DevOptimal.SystemStateManager.Registry;
using DevOptimal.SystemUtilities.Registry;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DevOptimal.SystemStateManager.Persistence.Registry
{
    internal class PersistentRegistryValueCaretaker : PersistentCaretaker<RegistryValueOriginator, RegistryValueMemento>
    {
        public PersistentRegistryValueCaretaker(string id, RegistryValueOriginator originator, SqliteConnection connection)
            : base(id, originator, connection)
        {
        }

        public PersistentRegistryValueCaretaker(string id, int processID, DateTime processStartTime, RegistryValueOriginator originator, RegistryValueMemento memento, SqliteConnection connection)
            : base(id, processID, processStartTime, originator, memento, connection)
        {
        }


        protected override void Initialize()
        {
            var command = connection.CreateCommand();
            command.CommandText =
            $@"CREATE TABLE IF NOT EXISTS {nameof(PersistentRegistryValueCaretaker)} (
                '{nameof(ID)}' TEXT PRIMARY KEY,
                '{nameof(ProcessID)}' INTEGER NOT NULL,
                '{nameof(ProcessStartTime)}' INTEGER NOT NULL,
                '{nameof(Originator.Hive)}' INTEGER NOT NULL,
                '{nameof(Originator.View)}' INTEGER NOT NULL,
                '{nameof(Originator.SubKey)}' TEXT NOT NULL,
                '{nameof(Originator.Name)}' TEXT,
                '{nameof(Memento.Value)}' BLOB,
                '{nameof(Memento.Kind)}' INTEGER NOT NULL
            );";
            command.ExecuteNonQuery();
        }

        protected override void Persist()
        {
            var command = connection.CreateCommand();
            command.CommandText =
            $@"INSERT INTO {nameof(PersistentRegistryValueCaretaker)} (
                '{nameof(ID)}',
                '{nameof(ProcessID)}',
                '{nameof(ProcessStartTime)}',
                '{nameof(Originator.Hive)}',
                '{nameof(Originator.View)}',
                '{nameof(Originator.SubKey)}',
                '{nameof(Originator.Name)}',
                '{nameof(Memento.Value)}',
                '{nameof(Memento.Kind)}'
            ) VALUES (
                @{nameof(ID)},
                @{nameof(ProcessID)},
                @{nameof(ProcessStartTime)},
                @{nameof(Originator.Hive)},
                @{nameof(Originator.View)},
                @{nameof(Originator.SubKey)},
                @{nameof(Originator.Name)},
                @{nameof(Memento.Value)},
                @{nameof(Memento.Kind)}
            );";
            command.Parameters.AddWithValue($"@{nameof(ID)}", ID);
            command.Parameters.AddWithValue($"@{nameof(ProcessID)}", ProcessID);
            command.Parameters.AddWithValue($"@{nameof(ProcessStartTime)}", ProcessStartTime.Ticks);
            command.Parameters.AddWithValue($"@{nameof(Originator.Hive)}", Originator.Hive);
            command.Parameters.AddWithValue($"@{nameof(Originator.View)}", Originator.View);
            command.Parameters.AddWithValue($"@{nameof(Originator.SubKey)}", Originator.SubKey);
            command.Parameters.AddWithNullableValue($"@{nameof(Originator.Name)}", Originator.Name);
            command.Parameters.AddWithNullableValue($"@{nameof(Memento.Value)}", ConvertValueToBytes(Memento.Value));
            command.Parameters.AddWithValue($"@{nameof(Memento.Kind)}", Memento.Kind);
            command.ExecuteNonQuery();
        }

        protected override void Unpersist()
        {
            var command = connection.CreateCommand();
            command.CommandText = $@"DELETE FROM {nameof(PersistentRegistryValueCaretaker)} WHERE {nameof(ID)} = @{nameof(ID)};";
            command.Parameters.AddWithValue($"@{nameof(ID)}", ID);
            command.ExecuteNonQuery();
        }

        public static IEnumerable<IPersistentSnapshot> GetCaretakers(SqliteConnection connection, IRegistry registry)
        {
            var caretakers = new List<PersistentRegistryValueCaretaker>();

            if (connection.TableExists(nameof(PersistentRegistryValueCaretaker)))
            {
                using (var reader = connection.ExecuteReader($@"SELECT * FROM {nameof(PersistentRegistryValueCaretaker)};"))
                {
                    while (reader.Read())
                    {
                        byte[] valueBytes;
                        using (var valueStream = reader.GetNullableStream(nameof(RegistryValueMemento.Value)))
                        {
                            if (valueStream == null)
                            {
                                valueBytes = null;
                            }
                            else
                            {
                                using (var memoryStream = new MemoryStream())
                                {
                                    valueStream.CopyTo(memoryStream);
                                    valueBytes = memoryStream.ToArray();
                                }
                            }
                        }
                        var caretaker = new PersistentRegistryValueCaretaker(
                            id: reader.GetString(nameof(ID)),
                            processID: reader.GetInt32(nameof(ProcessID)),
                            processStartTime: new DateTime(reader.GetInt64(nameof(ProcessStartTime))),
                            originator: new RegistryValueOriginator(
                                hive: (RegistryHive)reader.GetInt32(nameof(RegistryValueOriginator.Hive)),
                                view: (RegistryView)reader.GetInt32(nameof(RegistryValueOriginator.View)),
                                subKey: reader.GetString(nameof(RegistryValueOriginator.SubKey)),
                                name: reader.GetNullableString(nameof(RegistryValueOriginator.Name)),
                                registry: registry
                            ),
                            memento: new RegistryValueMemento
                            {
                                Value = ConvertBytesToValue(valueBytes),
                                Kind = (RegistryValueKind)reader.GetInt32(nameof(RegistryValueMemento.Kind))
                            },
                            connection: connection
                        );
                        caretakers.Add(caretaker);
                    }
                }
            }

            return caretakers;
        }

        private static byte[] ConvertValueToBytes(object value)
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
            return result.ToArray();
        }

        private static object ConvertBytesToValue(byte[] bytes)
        {
            if (bytes == null)
            {
                return null;
            }

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
    }
}
