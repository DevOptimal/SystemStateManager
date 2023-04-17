using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace DevOptimal.SystemStateManager.Persistence
{
    internal static class Extensions
    {
        public static SqliteParameter AddWithNullableValue(this SqliteParameterCollection parameters, string parameterName, object value)
        {
            return parameters.AddWithValue(parameterName, value ?? DBNull.Value);
        }

        public static bool GetBoolean(this SqliteDataReader reader, string name) => reader.GetBoolean(reader.GetOrdinal(name));

        public static int GetInt32(this SqliteDataReader reader, string name) => reader.GetInt32(reader.GetOrdinal(name));

        public static long GetInt64(this SqliteDataReader reader, string name) => reader.GetInt64(reader.GetOrdinal(name));

        public static string GetString(this SqliteDataReader reader, string name) => reader.GetString(reader.GetOrdinal(name));

        public static string GetNullableString(this SqliteDataReader reader, string name) => reader.GetNullableString(reader.GetOrdinal(name));

        public static string GetNullableString(this SqliteDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            return reader.GetString(ordinal);
        }

        public static Stream GetStream(this SqliteDataReader reader, string name) => reader.GetStream(reader.GetOrdinal(name));

        public static Stream GetNullableStream(this SqliteDataReader reader, string name) => reader.GetNullableStream(reader.GetOrdinal(name));

        public static Stream GetNullableStream(this SqliteDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            return reader.GetStream(ordinal);
        }

        public static SqliteDataReader ExecuteReader(this SqliteConnection connection, string commandText)
        {
            var command = connection?.CreateCommand() ?? throw new ArgumentNullException(nameof(connection));
            command.CommandText = commandText ?? throw new ArgumentNullException(nameof(commandText));
            return command.ExecuteReader();
        }

        public static bool TableExists(this SqliteConnection connection, string tableName)
        {
            using (var reader = connection.ExecuteReader($"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}'"))
            {
                return reader.HasRows;
            }
        }
    }
}
