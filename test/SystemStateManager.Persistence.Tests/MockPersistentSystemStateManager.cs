using DevOptimal.SystemUtilities.Environment;
using DevOptimal.SystemUtilities.FileSystem;
using DevOptimal.SystemUtilities.Registry;

namespace DevOptimal.SystemStateManager.Persistence.Tests
{
    public class MockPersistentSystemStateManager : PersistentSystemStateManager
    {
        public MockPersistentSystemStateManager(MockEnvironment environment)
            : this(environment, new MockFileSystem(), new MockRegistry())
        {
        }

        public MockPersistentSystemStateManager(MockFileSystem fileSystem)
            : this(new MockEnvironment(), fileSystem, new MockRegistry())
        {
        }

        public MockPersistentSystemStateManager(MockRegistry registry)
            : this(new MockEnvironment(), new MockFileSystem(), registry)
        {
        }

        private MockPersistentSystemStateManager(
            MockEnvironment environment,
            MockFileSystem fileSystem,
            MockRegistry registry)
            : base(environment, fileSystem, registry)
        {
        }
    }
}
