using bradselw.System.Resources.Registry;
using Microsoft.Win32;
using System.Runtime.Versioning;

namespace bradselw.MachineStateManager.Persistence.Tests.Registry
{
    [TestClass]
    [SupportedOSPlatform("windows")]
    public class RegistryValueTests
    {
        private MockRegistryProxy proxy;

        [TestInitialize]
        public void TestInitializeAttribute()
        {
            proxy = new MockRegistryProxy();
        }

        [TestMethod]
        public void RestoresDeletedRegistryValue()
        {
            var hive = RegistryHive.LocalMachine;
            var view = RegistryView.Default;
            var subKey = @"SOTFWARE\Microsoft\Windows";
            proxy.CreateRegistryKey(hive, view, subKey);

            var name = "foo";
            var expectedValue = "bar";
            var expectedKind = RegistryValueKind.String;
            proxy.SetRegistryValue(hive, view, subKey, name, expectedValue, expectedKind);

            var machineStateManager = new MockMachineStateManager(proxy);

            using (machineStateManager.SnapshotRegistryValue(hive, view, subKey, name))
            {
                proxy.DeleteRegistryValue(hive, view, subKey, name);
                Assert.IsFalse(proxy.RegistryValueExists(hive, view, subKey, name));
            }

            Assert.IsTrue(proxy.RegistryValueExists(hive, view, subKey, name));
            var (actualValue, actualKind) = proxy.GetRegistryValue(hive, view, subKey, name);
            Assert.AreEqual(expectedValue, actualValue);
            Assert.AreEqual(expectedKind, actualKind);
        }
    }
}
