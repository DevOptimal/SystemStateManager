using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.Versioning;

namespace MachineStateManager.Tests
{
    [TestClass]
    [SupportedOSPlatform("windows")]
    public class RegistryTests
    {
        private const RegistryHive hive = RegistryHive.CurrentUser;

        private const RegistryView view = RegistryView.Default;

        private const string rootSubKey = nameof(MachineStateManager);

        [TestMethod]
        public void RestoresDeletedRegKey()
        {
            var subKey = Path.Combine(rootSubKey, Guid.NewGuid().ToString());
            RegistryKey.OpenBaseKey(hive, view).CreateSubKey(subKey);

            var machineStateManager = new MachineStateManager();

            using (machineStateManager.SnapshotRegistryKey(hive, view, subKey))
            {
                RegistryKey.OpenBaseKey(hive, view).DeleteSubKey(subKey);
                Assert.AreEqual(null, RegistryKey.OpenBaseKey(hive, view).OpenSubKey(subKey));
            }

            Assert.AreNotEqual(null, RegistryKey.OpenBaseKey(hive, view).OpenSubKey(subKey));
        }

        [TestMethod]
        public void RestoresDeletedRegValue()
        {
            var subKey = Path.Combine(rootSubKey, Guid.NewGuid().ToString());
            var regKey = RegistryKey.OpenBaseKey(hive, view).CreateSubKey(subKey);

            var name = "foo";
            regKey.SetValue(name, "bar");

            var machineStateManager = new MachineStateManager();

            using (machineStateManager.SnapshotRegistryValue(hive, view, subKey, name))
            {
                regKey.DeleteValue(name);
                Assert.AreEqual(null, regKey.GetValue(name));
            }

            Assert.AreEqual("bar", (string?)regKey.GetValue(name));
        }
    }
}
