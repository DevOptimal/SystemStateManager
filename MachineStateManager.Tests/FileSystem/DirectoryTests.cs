using bradselw.System.Resources.FileSystem;
using System.IO;

namespace bradselw.MachineStateManager.Tests.FileSystem
{
    [TestClass]
    public class DirectoryTests
    {
        private MockFileSystemProxy proxy;

        private MockMachineStateManager machineStateManager;

        private const string path = @"C:\foo\bar";

        [TestInitialize]
        public void TestInitializeAttribute()
        {
            proxy = new MockFileSystemProxy();

            machineStateManager = new MockMachineStateManager(proxy);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            machineStateManager.Dispose();
        }

        [TestMethod]
        public void RevertsDirectoryCreation()
        {
            using (machineStateManager.SnapshotDirectory(path))
            {
                proxy.CreateDirectory(path);

                Assert.IsTrue(proxy.DirectoryExists(path));
            }

            Assert.IsFalse(proxy.DirectoryExists(path));
        }

        [TestMethod]
        public void RevertsDirectoryCreationWithChildren()
        {
            using (machineStateManager.SnapshotDirectory(path))
            {
                proxy.CreateDirectory(path);
                proxy.CreateDirectory(Path.Combine(path, "blah"));
                proxy.CreateFile(Path.Combine(path, "log.txt"));
            }

            Assert.IsFalse(proxy.DirectoryExists(path));
        }

        [TestMethod]
        public void RevertsDirectoryDeletion()
        {
            proxy.CreateDirectory(path);

            using (machineStateManager.SnapshotDirectory(path))
            {
                proxy.DeleteDirectory(path, recursive: true);

                Assert.IsFalse(proxy.DirectoryExists(path));
            }

            Assert.IsTrue(proxy.DirectoryExists(path));
        }
    }
}
