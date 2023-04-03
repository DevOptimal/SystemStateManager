using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace DevOptimal.SystemStateManager.Persistence.SQLite
{
    internal static class Extensions
    {
        public static SqliteParameter AddWithNullableValue(this SqliteParameterCollection parameters, string parameterName, object value)
        {
            if (value == null)
            {
                value = DBNull.Value;
            }

            return parameters.AddWithValue(parameterName, value);
        }

        public static string GetNullableString(this SqliteDataReader reader, string name) => reader.GetNullableString(reader.GetOrdinal(name));

        public static string GetNullableString(this SqliteDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            return reader.GetString(ordinal);
        }

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
