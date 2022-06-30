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
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext testContext)
        {
            PersistentMachineStateManager.PersistenceURI = new Uri(Path.Combine(testContext.DeploymentDirectory, "persistence.litedb"));
        }

        [TestMethod]
        public void RestoreAbandonedSnapshots()
        {
            PersistentMachineStateManager.RestoreAbandonedSnapshots();
        }

        [TestMethod]
        public void DoesNotRestoreSnapshotsFromCurrentProcess()
        {
            var machineStateManager = new PersistentMachineStateManager();
            var name = "foo";
            var previousValue = global::System.Environment.GetEnvironmentVariable(name);//"bar";//
            //System.Environment.SetEnvironmentVariable(name, previousValue);

            using (machineStateManager.SnapshotEnvironmentVariable(name))
            {
                var newValue = Guid.NewGuid().ToString();
                global::System.Environment.SetEnvironmentVariable(name, newValue);
                PersistentMachineStateManager.RestoreAbandonedSnapshots();
                Assert.AreEqual(newValue, global::System.Environment.GetEnvironmentVariable(name));
            }

            Assert.AreEqual(previousValue, global::System.Environment.GetEnvironmentVariable(name));
        }

        [TestMethod]
        public void RestoresAbandonedSnapshots()
        {
            var name = "foo";
            var target = EnvironmentVariableTarget.Machine;
            var value = "bar";

            var environment = new MockEnvironmentProxy();
            environment.SetEnvironmentVariable(name, value, target);

            var fakeProcessID = global::System.Environment.ProcessId + 1;
            var fakeProcessStartTime = DateTime.Now;

            using (ShimsContext.Create())
            {
                ShimProcess.AllInstances.IdGet = p => fakeProcessID;
                ShimProcess.AllInstances.StartTimeGet = p => fakeProcessStartTime;

                var machineStateManager = new MockMachineStateManager(environment);
                var snapshot = machineStateManager.SnapshotEnvironmentVariable(name, target);

                var newValue = "baz";
                environment.SetEnvironmentVariable(name, newValue, target);
                Assert.AreEqual(newValue, environment.GetEnvironmentVariable(name, target));
            }

            BsonMapper.Global.RegisterType<IEnvironmentProxy>(
                serialize: value => new BsonValue(value),
                deserialize: bson => environment);
            MockMachineStateManager.RestoreAbandonedSnapshots();
            Assert.AreEqual(value, environment.GetEnvironmentVariable(name, target));
        }

        [TestMethod]
        public void Concurrency()
        {
            var tasks = new List<Task>();

            for (var i = 0; i < 100; i++)
            {
                tasks.Add(Task.Factory.StartNew(() =>
                {
                    var machineStateManager = new PersistentMachineStateManager();

                    var name = Guid.NewGuid().ToString();
                    using (machineStateManager.SnapshotEnvironmentVariable(name))
                    {
                        global::System.Environment.SetEnvironmentVariable(name, "test");
                        Assert.AreEqual("test", global::System.Environment.GetEnvironmentVariable(name));
                    }
                    Assert.AreEqual(null, global::System.Environment.GetEnvironmentVariable(name));
                }));
            }

            Task.WaitAll(tasks.ToArray());
        }

        [TestMethod]
        public void ReuseExistingCaretaker()
        {
            var machineStateManager = new PersistentMachineStateManager();
            var name = "foo";
            var previousValue = global::System.Environment.GetEnvironmentVariable(name);

            using (machineStateManager.SnapshotEnvironmentVariable(name))
            using (machineStateManager.SnapshotEnvironmentVariable(name))
            {
                var newValue = Guid.NewGuid().ToString();
                global::System.Environment.SetEnvironmentVariable(name, newValue);
                Assert.AreEqual(newValue, global::System.Environment.GetEnvironmentVariable(name));
            }

            Assert.AreEqual(previousValue, global::System.Environment.GetEnvironmentVariable(name));
        }
    }
}
