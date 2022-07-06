using DevOptimal.SystemUtilities.Environment;
using LiteDB;
using Microsoft.QualityTools.Testing.Fakes;
using System;
using System.Collections.Generic;
using System.Diagnostics.Fakes;
using System.IO;
using System.Threading.Tasks;

namespace DevOptimal.SystemStateManager.Persistence.Tests
{
    [TestClass]
    public class PersistentSystemStateManagerTests
    {
        private MockEnvironmentProxy proxy;

        private MockPersistentSystemStateManager systemStateManager;

        private const string name = "foo";

        private const EnvironmentVariableTarget target = EnvironmentVariableTarget.Machine;

        private const string expectedValue = "bar";

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext testContext)
        {
            PersistentSystemStateManager.PersistenceURI = new Uri(Path.Combine(testContext.ResultsDirectory, "persistence.litedb"));
        }

        [TestInitialize]
        public void TestInitializeAttribute()
        {
            proxy = new MockEnvironmentProxy();

            proxy.SetEnvironmentVariable(name, expectedValue, target);

            systemStateManager = new MockPersistentSystemStateManager(proxy);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            systemStateManager.Dispose();
        }

        [TestMethod]
        [TestCategory("OmitFromCI")] // Fakes require VS Enterprise, but agent machines only have Community installed.
        public void RestoresAbandonedSnapshots()
        {
            var fakeProcessID = global::System.Environment.ProcessId + 1;
            var fakeProcessStartTime = DateTime.Now;

            using (ShimsContext.Create())
            {
                ShimProcess.AllInstances.IdGet = p => fakeProcessID;
                ShimProcess.AllInstances.StartTimeGet = p => fakeProcessStartTime;

                var snapshot = systemStateManager.SnapshotEnvironmentVariable(name, target);
            }

            proxy.SetEnvironmentVariable(name, null, target);

            BsonMapper.Global.RegisterType<IEnvironmentProxy>(
                serialize: value => new BsonValue(value),
                deserialize: bson => proxy);
            MockPersistentSystemStateManager.RestoreAbandonedSnapshots();
            Assert.AreEqual(expectedValue, proxy.GetEnvironmentVariable(name, target));
        }

        [TestMethod]
        public void DoesNotRestoreSnapshotsFromCurrentProcess()
        {
            systemStateManager.SnapshotEnvironmentVariable(name, target);

            proxy.SetEnvironmentVariable(name, null, target);

            MockPersistentSystemStateManager.RestoreAbandonedSnapshots();
            Assert.AreNotEqual(expectedValue, proxy.GetEnvironmentVariable(name, target));
        }

        [TestMethod]
        public void RevertsSnapshotsConcurrently()
        {
            var tasks = new List<Task>();

            for (var i = 0; i < 100; i++)
            {
                tasks.Add(Task.Factory.StartNew(() =>
                {
                    var name = Guid.NewGuid().ToString();
                    using (systemStateManager.SnapshotEnvironmentVariable(name, target))
                    {
                        proxy.SetEnvironmentVariable(name, expectedValue, target);
                        Assert.AreEqual(expectedValue, proxy.GetEnvironmentVariable(name, target));
                    }
                    Assert.AreEqual(null, proxy.GetEnvironmentVariable(name, target));
                }));
            }

            Task.WaitAll(tasks.ToArray());
        }
    }
}
