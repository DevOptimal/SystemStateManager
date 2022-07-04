using DevOptimal.System.Resources.Environment;
using DevOptimal.System.Resources.FileSystem;
using DevOptimal.System.Resources.Registry;

namespace DevOptimal.MachineStateManager.Persistence.Tests
{
    public class MockPersistentMachineStateManager : PersistentMachineStateManager
    {
        public MockPersistentMachineStateManager(MockEnvironmentProxy environmentProxy)
            : this(environmentProxy, new MockFileSystemProxy(), new MockRegistryProxy())
        {
        }

        public MockPersistentMachineStateManager(MockFileSystemProxy fileSystemProxy)
            : this(new MockEnvironmentProxy(), fileSystemProxy, new MockRegistryProxy())
        {
        }

        public MockPersistentMachineStateManager(MockRegistryProxy registryProxy)
            : this(new MockEnvironmentProxy(), new MockFileSystemProxy(), registryProxy)
        {
        }

        private MockPersistentMachineStateManager(
            MockEnvironmentProxy environmentProxy,
            MockFileSystemProxy fileSystemProxy,
            MockRegistryProxy registryProxy)
            : base(environmentProxy, fileSystemProxy, registryProxy)
        {
        }
    }
}
