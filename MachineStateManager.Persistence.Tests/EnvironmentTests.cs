namespace MachineStateManager.Persistence.Tests
{
    [TestClass]
    public class EnvironmentTests
    {
        [TestMethod]
        public void TestEnvironmentVariableCaretaker()
        {
            var machineStateManager = new PersistedMachineStateManager();
            var name = "foo";
            var previousValue = System.Environment.GetEnvironmentVariable(name);//"bar";//
            //System.Environment.SetEnvironmentVariable(name, previousValue);

            using (machineStateManager.SnapshotEnvironmentVariable(name))
            {
                var newValue = Guid.NewGuid().ToString();
                System.Environment.SetEnvironmentVariable(name, newValue);
                Assert.AreEqual(newValue, System.Environment.GetEnvironmentVariable(name));
            }

            Assert.AreEqual(previousValue, System.Environment.GetEnvironmentVariable(name));
        }

        [TestMethod]
        public void TwoMachineStateManagers()
        {
            var machineStateManager1 = new PersistedMachineStateManager();
            var machineStateManager2 = new PersistedMachineStateManager();
            var name = "foo";
            var previousValue = System.Environment.GetEnvironmentVariable(name);

            using (machineStateManager1.SnapshotEnvironmentVariable(name))
            {
                var newValue = Guid.NewGuid().ToString();
                System.Environment.SetEnvironmentVariable(name, newValue);
                Assert.AreEqual(newValue, System.Environment.GetEnvironmentVariable(name));
            }

            Assert.AreEqual(previousValue, System.Environment.GetEnvironmentVariable(name));
        }
    }
}