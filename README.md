# System State Manager

## What does it do?

The System State Manager creates snapshots of the state of supported system resources at a given point in time which can be used to restore the state of those system resources at a later point in time.

## Why do I need it?

This is particularly useful in automation that needs to make temporary changes to the state of the system it runs on, but is expected to leave the system in the same state it was in before the automation ran.

## How about an example?

For example, during automated regression testing, sometimes a test needs to configure the product under test by changing system resources (such as an environment variable, configuration file, or registry key). Other times, the product will change system resources itself. In either case, the altered state of those system resources can interfere with subsequent tests, leading to failures that are difficult to debug.

This is where the System State Manager comes in: Prior to executing the test, the System State Manager can be used to snapshot the state of any system resources that the test or product might alter. During execution of the test, those system resources can be altered however needed. At the completion of the test, the System State Manager can be used to restore those system resources to the state they were in when snapshotted at the beginning of the test.

## Usage

To begin using the System State Manager, all you need to do is create a new `SystemStateManager` instance:
```csharp
var systemStateManager = new SystemStateManager();
```
Once you have a System State Manager instance, you can use it to snapshot system resources:
```csharp
var caretaker = systemStateManager.SnapshotFile(@"C:\foo\bar.txt");
```
When you snapshot a system resource, you get back a "caretaker" object. This caretaker can be used to restore the state of the system resource later by calling its `Dispose` method:
```csharp
caretaker.Dispose();
```

# Persistence

It is possible for a snapshot to not get restored by the time its process terminates. This can happen when the `Dispose` method on the snapshot doesn't get called, such as when the process is killed prematurely.

This is potentially devastating because system state could be lost. For example, consider an application that uses the System State Manager to snapshot a file, then overwrites the file, and finally uses the System State Manager to restore the original contents of the file. If the process crashes after it overwrites the file but before the System State Manager can restore its contents, then data loss has occurred.

For this reason, the System State Manager has a persistence layer that saves the state of system resources to disk when they are snapshotted. This "persistent" System State Manager has a static method `RestoreAbandonedCaretakers` which can be called to restore any "abandoned caretakers" left behind by old processes.

It is good practice to call `RestoreAbandonedCaretakers` periodically (such as at the beginning of your application) to ensure that state from previous processes gets cleaned up.

## Usage

To use the System State Manager's persistence layer, simply create a new instance of `PersistentSystemStateManager`:
```csharp
var systemStateManager = new PersistentSystemStateManager();
```
You can snapshot and restore system resources in the same manner previously described.

To restore any abandoned caretakers left behind by old processes, simply call the static `RestoreAbandonedCaretakers` method:
```csharp
PersistentSystemStateManager.RestoreAbandonedCaretakers();
```
