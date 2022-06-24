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
            var previousValue = global::System.Environment.GetEnvironmentVariable(name);

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