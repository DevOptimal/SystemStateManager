using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DevOptimal.SystemStateManager.Persistence.SQLite.Tests
{
    public static class SqliteConnectionExtensions
    {
        public static int CreateTable<T>(this SqliteConnection connection)
            where T : class
        {
            var type = typeof(T);
            var typeName = type.Name;
            var sqlStringBuilder = new StringBuilder($@"CREATE TABLE IF NOT EXISTS {typeName} (");

            var columnDefinitions = new List<string>();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                var columnDefinition = property.Name;

                // See https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/types
                var propertyType = property.PropertyType;
                if (propertyType == typeof(bool))
                {
                    columnDefinition += " INTEGER"; // 0 or 1
                }
                else if (propertyType == typeof(byte))
                {
                    columnDefinition += " INTEGER";
                }
                else if (propertyType == typeof(byte[]))
                {
                    columnDefinition += " BLOB";
                }
                else if (propertyType == typeof(char))
                {
                    columnDefinition += " TEXT"; // UTF-8
                }
                else if (propertyType == typeof(DateOnly))
                {
                    columnDefinition += " TEXT"; // yyyy-MM-dd
                }
                else if (propertyType == typeof(DateTime))
                {
                    columnDefinition += " TEXT"; // yyyy-MM-dd HH:mm:ss.FFFFFFF
                }
                else if (propertyType == typeof(DateTimeOffset))
                {
                    columnDefinition += " TEXT"; // yyyy-MM-dd HH:mm:ss.FFFFFFFzzz
                }
                else if (propertyType == typeof(decimal))
                {
                    columnDefinition += " TEXT"; // 0.0########################### format. REAL would be lossy.
                }
                else if (propertyType == typeof(double))
                {
                    columnDefinition += " REAL";
                }
                else if (propertyType == typeof(Guid))
                {
                    columnDefinition += " TEXT"; // 00000000-0000-0000-0000-000000000000
                }
                else if (propertyType == typeof(short))
                {
                    columnDefinition += " INTEGER";
                }
                else if (propertyType == typeof(int))
                {
                    columnDefinition += " INTEGER";
                }
                else if (propertyType == typeof(long))
                {
                    columnDefinition += " INTEGER";
                }
                else if (propertyType == typeof(sbyte))
                {
                    columnDefinition += " INTEGER";
                }
                else if (propertyType == typeof(float))
                {
                    columnDefinition += " REAL";
                }
                else if (propertyType == typeof(string))
                {
                    columnDefinition += " TEXT"; // UTF-8
                }
                else if (propertyType == typeof(TimeOnly))
                {
                    columnDefinition += " TEXT"; // HH:mm:ss.fffffff
                }
                else if (propertyType == typeof(TimeSpan))
                {
                    columnDefinition += " TEXT"; // d.hh:mm:ss.fffffff
                }
                else if (propertyType == typeof(ushort))
                {
                    columnDefinition += " INTEGER";
                }
                else if (propertyType == typeof(uint))
                {
                    columnDefinition += " INTEGER";
                }
                else if (propertyType == typeof(ulong))
                {
                    columnDefinition += " INTEGER"; // Large values overflow
                }
                else if (propertyType.IsEnum)
                {
                    columnDefinition += " INTEGER";
                }
                else if (propertyType.IsClass)
                {
                    // Just gonna gloss over this for now...
                    continue;
                }
                else
                {
                    throw new NotSupportedException($"The type '{propertyType.Name}' is not supported");
                }

                if (Attribute.IsDefined(property, typeof(PrimaryKeyAttribute)))
                {
                    columnDefinition += " PRIMARY KEY";
                }

                columnDefinitions.Add(columnDefinition);
            }
            sqlStringBuilder.Append(string.Join(", ", columnDefinitions));
            sqlStringBuilder.Append(");");

            var command = connection.CreateCommand();
            command.CommandText = sqlStringBuilder.ToString();
            return command.ExecuteNonQuery();
        }
    }
}
