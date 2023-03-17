using Dapper;
using DevOptimal.SystemStateManager.Registry;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DevOptimal.SystemStateManager.Persistence.SQLite.Registry
{
    internal class PersistentRegistryValueCaretaker : PersistentCaretaker<RegistryValueOriginator, RegistryValueMemento>
    {
        public RegistryHive Hive => Originator.Hive;

        public RegistryView View => Originator.View;

        public string SubKey => Originator.SubKey;

        public string Name => Originator.Name;

        public byte[] Value => ConvertValueToBytes(Memento.Value);

        public RegistryValueKind Kind => Memento.Kind;

        public PersistentRegistryValueCaretaker(string id, RegistryValueOriginator originator, SqliteConnection connection)
            : base(id, originator, connection)
        {
            connection.Execute($@"CREATE TABLE IF NOT EXISTS {nameof(PersistentRegistryKeyCaretaker)}s (
                {nameof(ID)} TEXT PRIMARY KEY,
                {nameof(ProcessID)} TEXT NOT NULL,
                {nameof(Hive)} INTEGER NOT NULL,
                {nameof(View)} INTEGER NOT NULL,
                {nameof(SubKey)} TEXT NOT NULL,
                {nameof(Name)} TEXT,
                {nameof(Value)} BLOB,
                {nameof(Kind)} INTEGER NOT NULL
            );");
        }

        public PersistentRegistryValueCaretaker(string id, string processID, long hive, long view, string subKey, string name, byte[] value, long kind)
            : base(id, processID, new RegistryValueOriginator((RegistryHive)hive, (RegistryView)view, subKey, name), new RegistryValueMemento { Value = ConvertBytesToValue(value), Kind = (RegistryValueKind)kind })
        {
        }

        protected override void Persist()
        {
            connection.Execute($@"INSERT INTO {nameof(PersistentRegistryValueCaretaker)}s (
                {nameof(ID)},
                {nameof(ProcessID)},
                {nameof(Hive)},
                {nameof(View)},
                {nameof(SubKey)},
                {nameof(Name)},
                {nameof(Value)},
                {nameof(Kind)}
            ) VALUES (
                @{nameof(ID)},
                @{nameof(ProcessID)},
                @{nameof(Hive)},
                @{nameof(View)},
                @{nameof(SubKey)},
                @{nameof(Name)},
                @{nameof(Value)},
                @{nameof(Kind)}
            );", this);
        }

        protected override void Unpersist()
        {
            connection.Execute($@"DELETE FROM {nameof(PersistentRegistryValueCaretaker)}s WHERE {nameof(ID)} = @{nameof(ID)};", this);
        }

        private static byte[] ConvertValueToBytes(object value)
        {
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
                    throw new NotSupportedException($"{value.GetType().Name} is not a supported registry type");
            }
            return result.ToArray();
        }

        private static object ConvertBytesToValue(byte[] bytes)
        {;
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
