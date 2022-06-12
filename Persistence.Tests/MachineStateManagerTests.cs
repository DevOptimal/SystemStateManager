using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MachineStateManager.Persistence.Tests
{
    [TestClass]
    public class MachineStateManagerTests
    {
        [TestMethod]
        public void CanGetAdminProcesses()
        {
            var processes = Process.GetProcesses();

            foreach (var process in processes)
            {
                var startTime = "Unaccessible";
                try
                {
                    startTime = process.StartTime.ToString();
                }
                catch { }
                Console.WriteLine($"Process {process.Id} started at {startTime}");
            }
        }

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
