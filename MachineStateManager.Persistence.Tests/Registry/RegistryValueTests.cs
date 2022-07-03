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

        private MockMachineStateManager machineStateManager;

        private const RegistryHive hive = RegistryHive.LocalMachine;
        private const RegistryView view = RegistryView.Default;
        private const string subKey = @"SOFTWARE\Microsoft\Windows";
        private const string name = "foo";

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
        public void RevertsRegistryValueAlteration()
        {
            proxy.CreateRegistryKey(hive, view, subKey);

            var value = "bar";
            var kind = RegistryValueKind.String;
            proxy.SetRegistryValue(hive, view, subKey, name, value, kind);

            using (machineStateManager.SnapshotRegistryValue(hive, view, subKey, name))
            {
                proxy.SetRegistryValue(hive, view, subKey, name, 10, RegistryValueKind.DWord);
            }

            Assert.IsTrue(proxy.RegistryValueExists(hive, view, subKey, name));
            var (actualValue, actualKind) = proxy.GetRegistryValue(hive, view, subKey, name);
            Assert.AreEqual(value, actualValue);
            Assert.AreEqual(kind, actualKind);
        }

        [TestMethod]
        public void RevertsRegistryValueCreation()
        {
            proxy.CreateRegistryKey(hive, view, subKey);

            using (machineStateManager.SnapshotRegistryValue(hive, view, subKey, name))
            {
                proxy.SetRegistryValue(hive, view, subKey, name, "bar", RegistryValueKind.String);
            }

            Assert.IsFalse(proxy.RegistryValueExists(hive, view, subKey, name));
        }

        [TestMethod]
        public void RevertsRegistryValueDeletion()
        {
            proxy.CreateRegistryKey(hive, view, subKey);

            var value = "bar";
            var kind = RegistryValueKind.String;
            proxy.SetRegistryValue(hive, view, subKey, name, value, kind);

            using (machineStateManager.SnapshotRegistryValue(hive, view, subKey, name))
            {
                proxy.DeleteRegistryValue(hive, view, subKey, name);
            }

            Assert.IsTrue(proxy.RegistryValueExists(hive, view, subKey, name));
            var (actualValue, actualKind) = proxy.GetRegistryValue(hive, view, subKey, name);
            Assert.AreEqual(value, actualValue);
            Assert.AreEqual(kind, actualKind);
        }
    }
}
