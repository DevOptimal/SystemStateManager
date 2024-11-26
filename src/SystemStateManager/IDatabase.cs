using System.Collections.Generic;

namespace DevOptimal.SystemStateManager
{
    public interface IDatabase
    {
        /// <summary>
        /// Adds a snapshot to the database.
        /// </summary>
        /// <param name="snapshot">The snapshot to add.</param>
        void AddSnapshot(ISnapshot snapshot);

        /// <summary>
        /// Gets a snapshot from the database by its ID.
        /// </summary>
        /// <param name="id">The ID of the snapshot to get.</param>
        /// <returns>The snapshot with the given ID or null if not found.</returns>
        ISnapshot GetSnapshot(string id);

        /// <summary>
        /// Gets all snapshots from the database.
        /// </summary>
        /// <returns>An enumeration of all snapshots in the database.</returns>
        IEnumerable<ISnapshot> GetSnapshots();

        /// <summary>
        /// Removes a snapshot from the database.
        /// </summary>
        /// <param name="snapshot">The snapshot to remove.</param>
        void RemoveSnapshot(ISnapshot snapshot);
    }
}
