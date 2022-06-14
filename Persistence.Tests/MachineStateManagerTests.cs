namespace MachineStateManager.Persistence.Tests
{
    [TestClass]
    public class MachineStateManagerTests
    {
        [TestMethod]
        public void RestoreAbandonedCaretakers()
        {
            PersistentMachineStateManager.RestoreAbandonedCaretakers();
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
                        System.Environment.SetEnvironmentVariable(name, "test");
                        Assert.AreEqual("test", System.Environment.GetEnvironmentVariable(name));
                    }
                    Assert.AreEqual(null, System.Environment.GetEnvironmentVariable(name));
                }));
            }

            Task.WaitAll(tasks.ToArray());
        }
    }
}
