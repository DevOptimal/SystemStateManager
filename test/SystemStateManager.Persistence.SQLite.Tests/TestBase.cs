using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace DevOptimal.SystemStateManager.Persistence.SQLite.Tests
{
    public abstract class TestBase
    {
        public TestContext TestContext { get; set; }

        protected SqliteConnection connection => connectionLazy.Value;

        private Lazy<SqliteConnection> connectionLazy;

        public TestBase()
        {
            connectionLazy = new Lazy<SqliteConnection>(() =>
            {
                var connectionString = new SqliteConnectionStringBuilder
                {
                    DataSource = Path.Combine(TestContext.ResultsDirectory, "persistence.db")
                }.ToString();
                var connection = new SqliteConnection(connectionString);

                connection.Open();

                return connection;
            });
        }
    }
}
