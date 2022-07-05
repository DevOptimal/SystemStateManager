using DevOptimal.System.Resources.Environment;
using DevOptimal.System.Resources.FileSystem;
using DevOptimal.System.Resources.Registry;

namespace DevOptimal.SystemStateManager.Tests
{
    public class MockSystemStateManager : SystemStateManager
    {
        public MockSystemStateManager(MockEnvironmentProxy environmentProxy)
            : this(environmentProxy, new MockFileSystemProxy(), new MockRegistryProxy())
        {
        }

        public MockSystemStateManager(MockFileSystemProxy fileSystemProxy)
            : this(new MockEnvironmentProxy(), fileSystemProxy, new MockRegistryProxy())
        {
        }

        public MockSystemStateManager(MockRegistryProxy registryProxy)
            : this(new MockEnvironmentProxy(), new MockFileSystemProxy(), registryProxy)
        {
        }

        private MockSystemStateManager(
            MockEnvironmentProxy environmentProxy,
            MockFileSystemProxy fileSystemProxy,
            MockRegistryProxy registryProxy)
            : base(environmentProxy, fileSystemProxy, registryProxy)
        {
        }
    }
}
