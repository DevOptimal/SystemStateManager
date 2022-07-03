using bradselw.System.Resources.Registry;
using Microsoft.Win32;
using System.IO;
using System.Runtime.Versioning;

namespace bradselw.MachineStateManager.Persistence.Tests.Registry
{
    [TestClass]
    [SupportedOSPlatform("windows")]
    public class RegistryKeyTests
    {
        private MockRegistryProxy proxy;

        private MockMachineStateManager machineStateManager;

        private const RegistryHive hive = RegistryHive.LocalMachine;

        private const RegistryView view = RegistryView.Default;

        private const string subKey = @"SOTFWARE\Microsoft\Windows";

        [TestInitialize]
        public void TestInitializeAttribute()
        {
            proxy = new MockRegistryProxy();

            machineStateManager = new MockMachineStateManager(proxy);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            machineStateManager.Dispose();
        }

        [TestMethod]
        public void RevertsRegistryKeyCreation()
        {
            using (machineStateManager.SnapshotRegistryKey(hive, view, subKey))
            {
                proxy.CreateRegistryKey(hive, view, subKey);
            }

            Assert.IsFalse(proxy.RegistryKeyExists(hive, view, subKey));
        }

        [TestMethod]
        public void RevertsRegistryKeyCreationWithChildren()
        {
            using (machineStateManager.SnapshotRegistryKey(hive, view, subKey))
            {
                proxy.CreateRegistryKey(hive, view, subKey);
                proxy.CreateRegistryKey(hive, view, Path.Combine(subKey, "foo"));
                proxy.SetRegistryValue(hive, view, subKey, "bar", "Hello, world!", RegistryValueKind.String);
            }

            Assert.IsFalse(proxy.RegistryKeyExists(hive, view, subKey));
        }

        [TestMethod]
        public void RevertsRegistryKeyDeletion()
        {
            proxy.CreateRegistryKey(hive, view, subKey);

            using (machineStateManager.SnapshotRegistryKey(hive, view, subKey))
            {
                proxy.DeleteRegistryKey(hive, view, subKey, recursive: true);
            }

            Assert.IsTrue(proxy.RegistryKeyExists(hive, view, subKey));
        }
    }
}
