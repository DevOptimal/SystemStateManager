﻿using Microsoft.Win32;
using System;
using System.IO;
using System.Text;

namespace DevOptimal.SystemStateManager.Persistence.Tests
{
    [TestClass]
    [TestCategory("OmitFromCI")] // Fakes require VS Enterprise, but agent machines only have Community installed.
    public class RestoreAbandonedSnapshotsTests : TestBase
    {
        [TestMethod]
        public void RestoresAbandonedEnvironmentVariableSnapshots()
        {
            var environmentVariableName = "foo";
            var environmentVariableTarget = EnvironmentVariableTarget.Machine;
            var expectedEnvironmentVariableValue = "bar";
            environment.SetEnvironmentVariable(environmentVariableName, expectedEnvironmentVariableValue, environmentVariableTarget);

            using (CreateShimsContext())
            {
                var systemStateManager = new PersistentSystemStateManager(environment, fileSystem, registry);
                systemStateManager.SnapshotEnvironmentVariable(environmentVariableName, environmentVariableTarget);
            }

            environment.SetEnvironmentVariable(environmentVariableName, null, environmentVariableTarget);

            PersistentSystemStateManager.RestoreAbandonedSnapshots();

            Assert.AreEqual(expectedEnvironmentVariableValue, environment.GetEnvironmentVariable(environmentVariableName, environmentVariableTarget));
        }

        [TestMethod]
        public void RestoresAbandonedDirectorySnapshots()
        {
            var directoryPath = @"C:\foo";
            fileSystem.CreateDirectory(directoryPath);

            using (CreateShimsContext())
            {
                var systemStateManager = new PersistentSystemStateManager(environment, fileSystem, registry);
                systemStateManager.SnapshotDirectory(directoryPath);
            }

            fileSystem.DeleteDirectory(directoryPath, recursive: true);

            PersistentSystemStateManager.RestoreAbandonedSnapshots();

            Assert.IsTrue(fileSystem.DirectoryExists(directoryPath));
        }

        [TestMethod]
        public void RestoresAbandonedFileSnapshots()
        {
            var filePath = @"C:\foo\bar.txt";
            var expectedFileBytes = Encoding.UTF8.GetBytes("Hello, world!");
            using (var stream = fileSystem.OpenFile(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(expectedFileBytes);
            }

            using (CreateShimsContext())
            {
                var systemStateManager = new PersistentSystemStateManager(environment, fileSystem, registry);
                systemStateManager.SnapshotFile(filePath);
            }

            fileSystem.DeleteFile(filePath);

            PersistentSystemStateManager.RestoreAbandonedSnapshots();

            var actualFileBytes = new byte[expectedFileBytes.Length];
            using (var stream = fileSystem.OpenFile(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                stream.Read(actualFileBytes, 0, expectedFileBytes.Length);
            }
            CollectionAssert.AreEqual(expectedFileBytes, actualFileBytes);
        }

        [TestMethod]
        public void RestoresAbandonedRegistryKeySnapshots()
        {
            var registryHive = RegistryHive.LocalMachine;
            var registryView = RegistryView.Default;
            var registrySubKey = @"SOFTWARE\Microsoft\StrongName\Verification";
            registry.CreateRegistryKey(registryHive, registryView, registrySubKey);

            using (CreateShimsContext())
            {
                var systemStateManager = new PersistentSystemStateManager(environment, fileSystem, registry);
                systemStateManager.SnapshotRegistryKey(registryHive, registryView, registrySubKey);
            }

            registry.DeleteRegistryKey(registryHive, registryView, registrySubKey, recursive: true);

            PersistentSystemStateManager.RestoreAbandonedSnapshots();

            Assert.IsTrue(registry.RegistryKeyExists(registryHive, registryView, registrySubKey));
        }

        [TestMethod]
        public void RestoresAbandonedRegistryValueSnapshots()
        {
            var registryHive = RegistryHive.LocalMachine;
            var registryView = RegistryView.Default;
            var registrySubKey = @"SOFTWARE\Microsoft\StrongName\Verification";
            registry.CreateRegistryKey(registryHive, registryView, registrySubKey);

            var stringRegistryValueName = RegistryValueKind.String.ToString();
            var stringRegistryValueExpectedValue = "foo";
            var stringRegistryValueExpectedKind = RegistryValueKind.String;
            registry.SetRegistryValue(registryHive, registryView, registrySubKey, stringRegistryValueName, stringRegistryValueExpectedValue, stringRegistryValueExpectedKind);
            var expandStringRegistryValueName = RegistryValueKind.ExpandString.ToString();
            var expandStringRegistryValueExpectedValue = "bar";
            var expandStringRegistryValueExpectedKind = RegistryValueKind.ExpandString;
            registry.SetRegistryValue(registryHive, registryView, registrySubKey, expandStringRegistryValueName, expandStringRegistryValueExpectedValue, expandStringRegistryValueExpectedKind);
            var binaryRegistryValueName = RegistryValueKind.Binary.ToString();
            var binaryRegistryValueExpectedValue = Encoding.UTF8.GetBytes("Hello, world!");
            var binaryRegistryValueExpectedKind = RegistryValueKind.Binary;
            registry.SetRegistryValue(registryHive, registryView, registrySubKey, binaryRegistryValueName, binaryRegistryValueExpectedValue, binaryRegistryValueExpectedKind);
            var dwordRegistryValueName = RegistryValueKind.DWord.ToString();
            var dwordRegistryValueExpectedValue = (int)3;
            var dwordRegistryValueExpectedKind = RegistryValueKind.DWord;
            registry.SetRegistryValue(registryHive, registryView, registrySubKey, dwordRegistryValueName, dwordRegistryValueExpectedValue, dwordRegistryValueExpectedKind);
            var qwordRegistryValueName = RegistryValueKind.QWord.ToString();
            var qwordRegistryValueExpectedValue = (long)3;
            var qwordRegistryValueExpectedKind = RegistryValueKind.QWord;
            registry.SetRegistryValue(registryHive, registryView, registrySubKey, qwordRegistryValueName, qwordRegistryValueExpectedValue, qwordRegistryValueExpectedKind);
            var multiStringRegistryValueName = RegistryValueKind.MultiString.ToString();
            var multiStringRegistryValueExpectedValue = new[] { "hello", "world" };
            var multiStringRegistryValueExpectedKind = RegistryValueKind.MultiString;
            registry.SetRegistryValue(registryHive, registryView, registrySubKey, multiStringRegistryValueName, multiStringRegistryValueExpectedValue, multiStringRegistryValueExpectedKind);

            using (CreateShimsContext())
            {
                var systemStateManager = new PersistentSystemStateManager(environment, fileSystem, registry);
                systemStateManager.SnapshotRegistryValue(registryHive, registryView, registrySubKey, stringRegistryValueName);
                systemStateManager.SnapshotRegistryValue(registryHive, registryView, registrySubKey, expandStringRegistryValueName);
                systemStateManager.SnapshotRegistryValue(registryHive, registryView, registrySubKey, binaryRegistryValueName);
                systemStateManager.SnapshotRegistryValue(registryHive, registryView, registrySubKey, dwordRegistryValueName);
                systemStateManager.SnapshotRegistryValue(registryHive, registryView, registrySubKey, qwordRegistryValueName);
                systemStateManager.SnapshotRegistryValue(registryHive, registryView, registrySubKey, multiStringRegistryValueName);
            }

            registry.DeleteRegistryValue(registryHive, registryView, registrySubKey, stringRegistryValueName);
            registry.DeleteRegistryValue(registryHive, registryView, registrySubKey, expandStringRegistryValueName);
            registry.DeleteRegistryValue(registryHive, registryView, registrySubKey, binaryRegistryValueName);
            registry.DeleteRegistryValue(registryHive, registryView, registrySubKey, dwordRegistryValueName);
            registry.DeleteRegistryValue(registryHive, registryView, registrySubKey, qwordRegistryValueName);
            registry.DeleteRegistryValue(registryHive, registryView, registrySubKey, multiStringRegistryValueName);

            PersistentSystemStateManager.RestoreAbandonedSnapshots();

            var (stringRegistryValueActualValue, stringRegistryValueActualKind) = registry.GetRegistryValue(registryHive, registryView, registrySubKey, stringRegistryValueName);
            Assert.AreEqual(stringRegistryValueExpectedValue, stringRegistryValueActualValue);
            Assert.AreEqual(stringRegistryValueExpectedKind, stringRegistryValueActualKind);

            var (expandStringRegistryValueActualValue, expandStringRegistryValueActualKind) = registry.GetRegistryValue(registryHive, registryView, registrySubKey, expandStringRegistryValueName);
            Assert.AreEqual(expandStringRegistryValueExpectedValue, expandStringRegistryValueActualValue);
            Assert.AreEqual(expandStringRegistryValueExpectedKind, expandStringRegistryValueActualKind);

            var (binaryRegistryValueActualValue, binaryRegistryValueActualKind) = registry.GetRegistryValue(registryHive, registryView, registrySubKey, binaryRegistryValueName);
            CollectionAssert.AreEqual(binaryRegistryValueExpectedValue, (byte[])binaryRegistryValueActualValue);
            Assert.AreEqual(binaryRegistryValueExpectedKind, binaryRegistryValueActualKind);

            var (dwordRegistryValueActualValue, dwordRegistryValueActualKind) = registry.GetRegistryValue(registryHive, registryView, registrySubKey, dwordRegistryValueName);
            Assert.AreEqual(dwordRegistryValueExpectedValue, dwordRegistryValueActualValue);
            Assert.AreEqual(dwordRegistryValueExpectedKind, dwordRegistryValueActualKind);

            var (qwordRegistryValueActualValue, qwordRegistryValueActualKind) = registry.GetRegistryValue(registryHive, registryView, registrySubKey, qwordRegistryValueName);
            Assert.AreEqual(qwordRegistryValueExpectedValue, qwordRegistryValueActualValue);
            Assert.AreEqual(qwordRegistryValueExpectedKind, qwordRegistryValueActualKind);

            var (multiStringRegistryValueActualValue, multiStringRegistryValueActualKind) = registry.GetRegistryValue(registryHive, registryView, registrySubKey, multiStringRegistryValueName);
            CollectionAssert.AreEqual(multiStringRegistryValueExpectedValue, (object[])multiStringRegistryValueActualValue);
            Assert.AreEqual(multiStringRegistryValueExpectedKind, multiStringRegistryValueActualKind);
        }
    }
}
