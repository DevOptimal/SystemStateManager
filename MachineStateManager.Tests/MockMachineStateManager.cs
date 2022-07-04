using bradselw.System.Resources.Environment;
using bradselw.System.Resources.FileSystem;
using bradselw.System.Resources.Registry;

namespace bradselw.MachineStateManager.Tests
{
    public class MockMachineStateManager : MachineStateManager
    {
        public MockMachineStateManager(MockEnvironmentProxy environmentProxy)
            : this(environmentProxy, new MockFileSystemProxy(), new MockRegistryProxy())
        {
        }

        public MockMachineStateManager(MockFileSystemProxy fileSystemProxy)
            : this(new MockEnvironmentProxy(), fileSystemProxy, new MockRegistryProxy())
        {
        }

        public MockMachineStateManager(MockRegistryProxy registryProxy)
            : this(new MockEnvironmentProxy(), new MockFileSystemProxy(), registryProxy)
        {
        }

        private MockMachineStateManager(
            MockEnvironmentProxy environmentProxy,
            MockFileSystemProxy fileSystemProxy,
            MockRegistryProxy registryProxy)
            : base(environmentProxy, fileSystemProxy, registryProxy)
        {
        }
    }
}
