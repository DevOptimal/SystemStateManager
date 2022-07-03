using bradselw.System.Resources.Environment;
using LiteDB;
using Microsoft.QualityTools.Testing.Fakes;
using System;
using System.Collections.Generic;
using System.Diagnostics.Fakes;
using System.IO;
using System.Threading.Tasks;

namespace bradselw.MachineStateManager.Persistence.Tests
{
    [TestClass]
    public class MachineStateManagerTests
    {
        private MockEnvironmentProxy proxy;

        private MockMachineStateManager machineStateManager;

        private const string name = "foo";

        private const EnvironmentVariableTarget target = EnvironmentVariableTarget.Machine;

        private const string expectedValue = "bar";

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext testContext)
        {
            PersistentMachineStateManager.PersistenceURI = new Uri(Path.Combine(testContext.DeploymentDirectory, "persistence.litedb"));
        }

        [TestInitialize]
        public void TestInitializeAttribute()
        {
            proxy = new MockEnvironmentProxy();

            proxy.SetEnvironmentVariable(name, expectedValue, target);

            machineStateManager = new MockMachineStateManager(proxy);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            machineStateManager.Dispose();
        }

        [TestMethod]
        public void MachineStateManagerCorrectlyDisposes()
        {
            using var caretaker = machineStateManager.SnapshotEnvironmentVariable(name, target);

            proxy.SetEnvironmentVariable(name, null, target);

            machineStateManager.Dispose();

            Assert.AreEqual(expectedValue, proxy.GetEnvironmentVariable(name, target));
        }

        [TestMethod]
        public void CaretakerCorrectlyDisposes()
        {
            using var caretaker = machineStateManager.SnapshotEnvironmentVariable(name, target);

            proxy.SetEnvironmentVariable(name, null, target);

            caretaker.Dispose();

            Assert.AreEqual(expectedValue, proxy.GetEnvironmentVariable(name, target));
        }

        [TestMethod]
        public void DoesNotRestoreSnapshotsFromCurrentProcess()
        {
            machineStateManager.SnapshotEnvironmentVariable(name, target);

            proxy.SetEnvironmentVariable(name, null, target);

            MockMachineStateManager.RestoreAbandonedSnapshots();
            Assert.AreNotEqual(expectedValue, proxy.GetEnvironmentVariable(name, target));
        }

        [TestMethod]
        public void RestoresAbandonedSnapshots()
        {
            var fakeProcessID = global::System.Environment.ProcessId + 1;
            var fakeProcessStartTime = DateTime.Now;

            using (ShimsContext.Create())
            {
                ShimProcess.AllInstances.IdGet = p => fakeProcessID;
                ShimProcess.AllInstances.StartTimeGet = p => fakeProcessStartTime;

                var snapshot = machineStateManager.SnapshotEnvironmentVariable(name, target);
            }

            proxy.SetEnvironmentVariable(name, null, target);

            BsonMapper.Global.RegisterType<IEnvironmentProxy>(
                serialize: value => new BsonValue(value),
                deserialize: bson => proxy);
            MockMachineStateManager.RestoreAbandonedSnapshots();
            Assert.AreEqual(expectedValue, proxy.GetEnvironmentVariable(name, target));
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
                    using (machineStateManager.SnapshotEnvironmentVariable(name, target))
                    {
                        proxy.SetEnvironmentVariable(name, expectedValue, target);
                        Assert.AreEqual(expectedValue, proxy.GetEnvironmentVariable(name, target));
                    }
                    Assert.AreEqual(null, proxy.GetEnvironmentVariable(name, target));
                }));
            }

            Task.WaitAll(tasks.ToArray());
        }

        [TestMethod]
        public void ReuseExistingCaretaker()
        {
            using (machineStateManager.SnapshotEnvironmentVariable(name, target))
            using (machineStateManager.SnapshotEnvironmentVariable(name, target))
            {
                proxy.SetEnvironmentVariable(name, null, target);
            }

            Assert.AreEqual(expectedValue, proxy.GetEnvironmentVariable(name, target));
        }
    }
}
