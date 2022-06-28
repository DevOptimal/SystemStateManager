using bradselw.System.Resources.FileSystem;
using System.IO;

namespace bradselw.MachineStateManager.Persistence.Tests.FileSystem
{
    [TestClass]
    public class DirectoryTests
    {
        private MockFileSystemProxy proxy;

        private MockMachineStateManager machineStateManager;

        private const string path = @"C:\temp\foo\bar";

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
        public void RevertsDirectoryCreate()
        {
            using (machineStateManager.SnapshotDirectory(path))
            {
                proxy.CreateDirectory(path);

                Assert.IsTrue(proxy.DirectoryExists(path));
            }

            Assert.IsFalse(proxy.DirectoryExists(path));
        }

        [TestMethod]
        public void RevertsDirectoryDelete()
        {
            proxy.CreateDirectory(path);

            using (machineStateManager.SnapshotDirectory(path))
            {
                proxy.DeleteDirectory(path, recursive: true);

                Assert.IsFalse(proxy.DirectoryExists(path));
            }

            Assert.IsTrue(proxy.DirectoryExists(path));
        }

        [TestMethod]
        public void RevertsDirectoryCreateWithChildren()
        {
            using (machineStateManager.SnapshotDirectory(path))
            {
                proxy.CreateDirectory(path);
                proxy.CreateDirectory(Path.Combine(path, "blah"));
                proxy.CreateFile(Path.Combine(path, "log.txt"));

                Assert.IsTrue(proxy.DirectoryExists(path));
            }

            Assert.IsFalse(proxy.DirectoryExists(path));
        }
    }
}
