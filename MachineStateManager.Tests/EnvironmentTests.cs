using System;

namespace bradselw.MachineStateManager.Tests
{
    [TestClass]
    public class EnvironmentTests
    {
        [TestMethod]
        public void TestEnvironmentVariableCaretaker()
        {
            var machineStateManager = new MachineStateManager();
            var name = "foo";
            var previousValue = System.Environment.GetEnvironmentVariable(name);

            using (machineStateManager.SnapshotEnvironmentVariable(name))
            {
                var newValue = Guid.NewGuid().ToString();
                System.Environment.SetEnvironmentVariable(name, newValue);
                Assert.AreEqual(newValue, System.Environment.GetEnvironmentVariable(name));
            }

            Assert.AreEqual(previousValue, System.Environment.GetEnvironmentVariable(name));
        }
    }
}