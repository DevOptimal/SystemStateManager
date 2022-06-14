namespace MachineStateManager.Persistence.Tests
{
    [TestClass]
    public class FileSystemTests
    {
        public TestContext TestContext { get; set; }

        private FileInfo testFile;

        private string expectedFileContent;

        [TestInitialize]
        public void TestInitializeAttribute()
        {
            expectedFileContent = Guid.NewGuid().ToString();
            testFile = new FileInfo(Path.Combine(TestContext.ResultsDirectory, Path.GetRandomFileName()));
            File.WriteAllText(testFile.FullName, expectedFileContent);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            File.Delete(testFile.FullName);
        }

        [TestMethod]
        public void RestoresChangedFile()
        {
            using (var machineStateManager = new PersistentMachineStateManager())
            using (machineStateManager.SnapshotFile(testFile.FullName))
            {
                var someOtherText = "This is some other text";
                File.WriteAllText(testFile.FullName, someOtherText);
                Assert.AreEqual(someOtherText, File.ReadAllText(testFile.FullName));
            }

            Assert.AreEqual(expectedFileContent, File.ReadAllText(testFile.FullName));
        }

        [TestMethod]
        public void RestoresDeletedFile()
        {
            using (var machineStateManager = new PersistentMachineStateManager())
            using (machineStateManager.SnapshotFile(testFile.FullName))
            {
                File.Delete(testFile.FullName);
            }

            Assert.AreEqual(expectedFileContent, File.ReadAllText(testFile.FullName));
        }

        [TestMethod]
        public void MachineStateManagerCorrectlyDisposes()
        {
            using var machineStateManager = new PersistentMachineStateManager();
            using var caretaker = machineStateManager.SnapshotFile(testFile.FullName);

            File.Delete(testFile.FullName);

            machineStateManager.Dispose();

            Assert.AreEqual(expectedFileContent, File.ReadAllText(testFile.FullName));
        }

        [TestMethod]
        public void CaretakerCorrectlyDisposes()
        {
            using var machineStateManager = new PersistentMachineStateManager();
            using var caretaker = machineStateManager.SnapshotFile(testFile.FullName);

            File.Delete(testFile.FullName);

            caretaker.Dispose();

            Assert.AreEqual(expectedFileContent, File.ReadAllText(testFile.FullName));
        }

        [TestMethod]
        public void CorrectlyRestoresTwoDifferentFilesWithSameContent()
        {
            var testFile2 = new FileInfo(Path.Combine(TestContext.ResultsDirectory, Path.GetRandomFileName()));
            File.WriteAllText(testFile2.FullName, expectedFileContent);

            using var machineStateManager = new PersistentMachineStateManager();

            using (var caretaker = machineStateManager.SnapshotFile(testFile.FullName))
            {
                File.Delete(testFile.FullName);
                Assert.IsFalse(File.Exists(testFile.FullName));

                using (var caretaker2 = machineStateManager.SnapshotFile(testFile2.FullName))
                {
                    File.Delete(testFile2.FullName);
                    Assert.IsFalse(File.Exists(testFile2.FullName));
                }

                Assert.AreEqual(expectedFileContent, File.ReadAllText(testFile2.FullName));
            }

            Assert.AreEqual(expectedFileContent, File.ReadAllText(testFile.FullName));
        }
    }
}
