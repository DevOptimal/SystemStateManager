using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace bradselw.MachineStateManager.Persistence.Tests
{
    [TestClass]
    public class MachineStateManagerTests
    {
        [TestMethod]
        public void RestoreAbandonedCaretakers()
        {
            PersistentMachineStateManager.RestoreAbandonedSnapshots();
        }

        [TestMethod]
        public void RestoreAbandonedCaretakersDoesNotRestoreCaretakersFromCurrentProcess()
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
