# Usage

To begin using the System State Manager, all you need to do is create a new `SystemStateManager` instance:
```csharp
using DevOptimal.SystemStateManager;

var systemStateManager = new SystemStateManager();
```

To use the System State Manager's [persistence layer](persistence.md), simply create a new instance of `PersistentSystemStateManager`:
```csharp
using DevOptimal.SystemStateManager.Persistence;

var systemStateManager = new PersistentSystemStateManager();
```

Once you have a System State Manager instance, you can use it to snapshot system resources:
```csharp
var snapshot = systemStateManager.SnapshotFile(@"C:\foo\bar.txt");
```

When you snapshot a system resource, you get back an `ISnapshot` object. This object can be used to restore the state of the system resource later by calling its `Dispose` method:
```csharp
snapshot.Dispose();
```

If you are using the persistence layer, you can restore any abandoned snapshots left behind by old processes by calling the static `RestoreAbandonedSnapshots` method:
```csharp
PersistentSystemStateManager.RestoreAbandonedSnapshots();
```
