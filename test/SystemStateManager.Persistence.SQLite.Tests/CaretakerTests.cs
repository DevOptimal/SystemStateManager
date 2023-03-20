using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;

namespace DevOptimal.SystemStateManager.Persistence.SQLite.Tests
{
    [TestClass]
    public class CaretakerTests : TestBase
    {
        private interface IMemento
        {
        }

        private class EnvironmentVariableMemento : IMemento
        {
            public string Value { get; set; }

            public EnvironmentVariableMemento(string value)
            {
                Value = value;
            }
        }

        private interface IOriginator<TMemento>
            where TMemento : IMemento
        {
        }

        private class EnvironmentVariableOriginator : IOriginator<EnvironmentVariableMemento>
        {
            public string Name { get; }

            public EnvironmentVariableTarget Target { get; }

            public EnvironmentVariableOriginator(string name, EnvironmentVariableTarget target)
            {
                Name = name;
                Target = target;
            }
        }

        private interface ISnapshot
        {
            string ID { get; }
        }

        private class Caretaker<TOriginator, TMemento> : ISnapshot
            where TOriginator : IOriginator<TMemento>
            where TMemento : IMemento
        {
            [PrimaryKey]
            public string ID { get; }

            public TOriginator Originator { get; }

            public TMemento Memento { get; }

            public Caretaker(string id, TOriginator originator, TMemento memento)
            {
                ID = id;
                Originator = originator;
                Memento = memento;
            }
        }

        private interface IPersistentSnapshot : ISnapshot
        {
            string ProcessID { get; }
        }

        private abstract class PersistentCaretaker<TOriginator, TMemento> : Caretaker<TOriginator, TMemento>, IPersistentSnapshot
            where TOriginator : IOriginator<TMemento>
            where TMemento : IMemento
        {
            public string ProcessID { get; }

            protected PersistentCaretaker(string id, string processID, TOriginator originator, TMemento memento)
                : base(id, originator, memento)
            {
                ProcessID = processID;
            }
        }

        private class PersistentEnvironmentVariableCaretaker : PersistentCaretaker<EnvironmentVariableOriginator, EnvironmentVariableMemento>
        {
            public string Name => Originator.Name;

            public EnvironmentVariableTarget Target => Originator.Target;

            public string Value => Memento.Value;

            public PersistentEnvironmentVariableCaretaker(string id, string processID, EnvironmentVariableOriginator originator, EnvironmentVariableMemento memento)
                : base(id, processID, originator, memento)
            {
            }

            public PersistentEnvironmentVariableCaretaker(string id, string processID, string name, long target, string value)
                : base(id, processID, new EnvironmentVariableOriginator(name, (EnvironmentVariableTarget)target), new EnvironmentVariableMemento(value))
            {
            }
        }

        [TestMethod]
        public void PersistCaretaker()
        {
            var caretaker = new PersistentEnvironmentVariableCaretaker(
                id: $@"[EnvironmentVariable]Process\Foo",
                processID: $"{12345}/{(new DateTime(2023, 3, 15, 21, 17, 35, 678)).Ticks}",
                originator: new EnvironmentVariableOriginator("Foo", EnvironmentVariableTarget.Process),
                memento: new EnvironmentVariableMemento("Bar"));

            //var command1 = connection.CreateCommand();
            //command1.CommandText =
            //$@"CREATE TABLE IF NOT EXISTS {nameof(PersistentEnvironmentVariableCaretaker)}s (
            //    {nameof(caretaker.ID)} TEXT PRIMARY KEY,
            //    {nameof(caretaker.ProcessID)} TEXT,
            //    {nameof(caretaker.Name)} TEXT,
            //    {nameof(caretaker.Target)} INTEGER,
            //    {nameof(caretaker.Value)} TEXT
            //);";
            //command1.ExecuteNonQuery();
            connection.CreateTable<PersistentEnvironmentVariableCaretaker>();

            var command2 = connection.CreateCommand();
            command2.CommandText =
            $@"INSERT INTO {nameof(PersistentEnvironmentVariableCaretaker)} (
                {nameof(caretaker.ID)},
                {nameof(caretaker.ProcessID)},
                {nameof(caretaker.Name)},
                {nameof(caretaker.Target)},
                {nameof(caretaker.Value)}
            ) VALUES (
                @{nameof(caretaker.ID)},
                @{nameof(caretaker.ProcessID)},
                @{nameof(caretaker.Name)},
                @{nameof(caretaker.Target)},
                @{nameof(caretaker.Value)}
            );";
            command2.Parameters.AddWithValue($"@{nameof(caretaker.ID)}", caretaker.ID);
            command2.Parameters.AddWithValue($"@{nameof(caretaker.ProcessID)}", caretaker.ProcessID);
            command2.Parameters.AddWithValue($"@{nameof(caretaker.Name)}", caretaker.Name);
            command2.Parameters.AddWithValue($"@{nameof(caretaker.Target)}", caretaker.Target);
            command2.Parameters.AddWithValue($"@{nameof(caretaker.Value)}", caretaker.Value);
            command2.ExecuteNonQuery();

            var caretakers = new List<PersistentEnvironmentVariableCaretaker>();
            var command3 = connection.CreateCommand();
            command3.CommandText = $@"SELECT * FROM {nameof(PersistentEnvironmentVariableCaretaker)}";
            using (var reader = command3.ExecuteReader())
            {
                while (reader.Read())
                {
                    var id = reader.GetString(nameof(caretaker.ID));
                    var processID = reader.GetString(nameof(caretaker.ProcessID));
                    var name = reader.GetString(nameof(caretaker.Name));
                    var target = (EnvironmentVariableTarget)reader.GetInt32(nameof(caretaker.Target));
                    var value = reader.GetString(nameof(caretaker.Value));
                    caretakers.Add(new PersistentEnvironmentVariableCaretaker(id, processID, new EnvironmentVariableOriginator(name, target), new EnvironmentVariableMemento(value)));
                }
            }

            var command4 = connection.CreateCommand();
            command4.CommandText = $@"DELETE FROM {nameof(PersistentEnvironmentVariableCaretaker)} WHERE {nameof(caretaker.ID)} = @{nameof(caretaker.ID)};";
            command4.Parameters.AddWithValue($"@{nameof(caretaker.ID)}", caretaker.ID);
            command4.ExecuteNonQuery();
        }
    }
}
