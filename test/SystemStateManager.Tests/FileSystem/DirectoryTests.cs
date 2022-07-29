using System.IO;

namespace DevOptimal.SystemStateManager.Tests.FileSystem
{
    [TestClass]
    public class DirectoryTests : TestBase
    {
        [TestMethod]
        public void RevertsDirectoryCreation()
        {
            var path = @"C:\foo\bar";

            using var systemStateManager = CreateSystemStateManager();
            using (systemStateManager.SnapshotDirectory(path))
            {
                fileSystem.CreateDirectory(path);

                Assert.IsTrue(fileSystem.DirectoryExists(path));
            }

            Assert.IsFalse(fileSystem.DirectoryExists(path));
        }

        [TestMethod]
        public void RevertsDirectoryCreationWithChildren()
        {
            var path = @"C:\foo\bar";

            using var systemStateManager = CreateSystemStateManager();
            using (systemStateManager.SnapshotDirectory(path))
            {
                fileSystem.CreateDirectory(path);
                fileSystem.CreateDirectory(Path.Combine(path, "blah"));
                fileSystem.CreateFile(Path.Combine(path, "log.txt"));
            }

            Assert.IsFalse(fileSystem.DirectoryExists(path));
        }

        [TestMethod]
        public void RevertsDirectoryDeletion()
        {
            var path = @"C:\foo\bar";
            fileSystem.CreateDirectory(path);

            using var systemStateManager = CreateSystemStateManager();
            using (systemStateManager.SnapshotDirectory(path))
            {
                fileSystem.DeleteDirectory(path, recursive: true);

                Assert.IsFalse(fileSystem.DirectoryExists(path));
            }

            Assert.IsTrue(fileSystem.DirectoryExists(path));
        }
    }
}
