using bradselw.System.Resources.Registry;
using Microsoft.Win32;
using System.Runtime.Versioning;

namespace bradselw.MachineStateManager.Persistence.Tests.Registry
{
    [TestClass]
    [SupportedOSPlatform("windows")]
    public class RegistryKeyTests
    {
        private MockRegistryProxy proxy;

        [TestInitialize]
        public void TestInitializeAttribute()
        {
            proxy = new MockRegistryProxy();
        }

        [TestMethod]
        public void RestoresDeletedRegistryKey()
        {
            var hive = RegistryHive.LocalMachine;
            var view = RegistryView.Default;
            var subKey = @"SOTFWARE\Microsoft\Windows";

            proxy.CreateRegistryKey(hive, view, subKey);

            var machineStateManager = new MockMachineStateManager(proxy);

            using (machineStateManager.SnapshotRegistryKey(hive, view, subKey))
            {
                proxy.DeleteRegistryKey(hive, view, subKey, recursive: true);
                Assert.IsFalse(proxy.RegistryKeyExists(hive, view, subKey));
            }

            Assert.IsTrue(proxy.RegistryKeyExists(hive, view, subKey));
        }
    }
}
