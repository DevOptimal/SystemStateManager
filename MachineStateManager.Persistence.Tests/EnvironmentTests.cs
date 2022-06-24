using System;

namespace bradselw.MachineStateManager.Persistence.Tests
{
    [TestClass]
    public class EnvironmentTests
    {
        [TestMethod]
        public void TestEnvironmentVariableCaretaker()
        {
            var machineStateManager = new PersistentMachineStateManager();
            var name = "foo";
            var previousValue = global::System.Environment.GetEnvironmentVariable(name);//"bar";//
            //System.Environment.SetEnvironmentVariable(name, previousValue);

            using (machineStateManager.SnapshotEnvironmentVariable(name))
            {
                var newValue = Guid.NewGuid().ToString();
                global::System.Environment.SetEnvironmentVariable(name, newValue);
                Assert.AreEqual(newValue, global::System.Environment.GetEnvironmentVariable(name));
            }

            Assert.AreEqual(previousValue, global::System.Environment.GetEnvironmentVariable(name));
        }

        [TestMethod]
        public void TwoMachineStateManagers()
        {
            var machineStateManager1 = new PersistentMachineStateManager();
            var machineStateManager2 = new PersistentMachineStateManager();
            var name = "foo";
            var previousValue = global::System.Environment.GetEnvironmentVariable(name);

            using (machineStateManager1.SnapshotEnvironmentVariable(name))
            {
                var newValue = Guid.NewGuid().ToString();
                global::System.Environment.SetEnvironmentVariable(name, newValue);
                Assert.AreEqual(newValue, global::System.Environment.GetEnvironmentVariable(name));
            }

            Assert.AreEqual(previousValue, global::System.Environment.GetEnvironmentVariable(name));
        }
    }
}