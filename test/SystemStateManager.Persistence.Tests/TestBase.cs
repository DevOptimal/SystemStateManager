﻿using DevOptimal.SystemUtilities.Environment;
using DevOptimal.SystemUtilities.FileSystem;
using DevOptimal.SystemUtilities.Registry;
using LiteDB;
using System;
using System.IO;

namespace DevOptimal.SystemStateManager.Persistence.Tests
{
    [TestClass]
    public abstract class TestBase
    {
        protected IEnvironment environment;
        protected IFileSystem fileSystem;
        protected IRegistry registry;

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext testContext)
        {
            PersistentSystemStateManager.PersistenceURI = new Uri(Path.Combine(testContext.ResultsDirectory, "persistence.litedb"));
        }

        [TestInitialize]
        public void TestInitialize()
        {
            environment = new MockEnvironment();
            LiteDatabaseFactory.Mapper.RegisterType(
                serialize: value => BsonMapper.Global.ToDocument(value),//new BsonValue(value),
                deserialize: bson => environment);

            fileSystem = new MockFileSystem();
            LiteDatabaseFactory.Mapper.RegisterType(
                serialize: value => BsonMapper.Global.ToDocument(value),//new BsonValue(value),
                deserialize: bson => fileSystem);

            registry = new MockRegistry();
            LiteDatabaseFactory.Mapper.RegisterType(
                serialize: value => BsonMapper.Global.ToDocument(value),//new BsonValue(value),
                deserialize: bson => registry);
        }
    }
}
