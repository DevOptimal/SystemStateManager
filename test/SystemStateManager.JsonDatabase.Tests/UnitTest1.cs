using DevOptimal.SystemStateManager.FileSystem.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;

namespace DevOptimal.SystemStateManager.JsonDatabase.Tests
{
    [TestClass]
    public class UnitTest1 : TestBase
    {
        [TestMethod]
        public void SystemStateManagerCorrectlyDisposes()
        {
            var name = "foo";
            var target = EnvironmentVariableTarget.Machine;
            var expectedValue = "bar";

            environment.SetEnvironmentVariable(name, expectedValue, target);

            using (var systemStateManager = CreateSystemStateManager())
            {

                systemStateManager.SnapshotEnvironmentVariable(name, target);

                environment.SetEnvironmentVariable(name, null, target);
            }

            Assert.AreEqual(expectedValue, environment.GetEnvironmentVariable(name, target));
        }

        [TestMethod]
        public void StreamReaderTests()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("foobar"));
            var reader = new StreamReader(stream);
            char c;
            while ((c = (char)reader.Read()) != 0)
            {
                Console.WriteLine(c);
            }

            Assert.AreEqual('f', c);
        }

        [TestMethod]
        public void ParserTests()
        {
            var json =
@"[
    {
        ""Name"": ""Brad"",
        ""Age"" : 39,
        ""Weight"": 203.57,
        ""Married"": true
    },
    ""Hello, world!""
]";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var parser = new MyJsonDatabase(stream, environment, fileSystem, registry, new LocalFileCache(@"C:\temp\cache", fileSystem));
            var objects = parser.GetObjects();
            foreach (var o in objects)
            {
                Console.WriteLine(o.GetType().FullName);
            }
        }
    }
}