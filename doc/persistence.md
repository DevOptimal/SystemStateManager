# Persistence

It is possible for a snapshot to not be restored before its process terminates. This happens when the `Dispose` method on the snapshot doesn't get called by the time the process exits, such as when a process is killed prematurely.

This can be devastating because system state could be lost. For example, consider an application that uses the System State Manager to snapshot a file, then overwrites the file, and finally uses the System State Manager to restore the original contents of the file. If the process crashes _after_ the application overwrites the file but _before_ the System State Manager can restore its contents, then data loss has occurred.

For this reason, the System State Manager has a persistence layer that saves the state of system resources to disk when they are snapshotted. This "persistent" System State Manager has a static method `RestoreAbandonedSnapshots` which can be called to restore any "abandoned snapshots" left behind by old processes.

It is good practice to call `RestoreAbandonedSnapshots` periodically (such as at the beginning of your application) to ensure that state from previous processes gets cleaned up.

