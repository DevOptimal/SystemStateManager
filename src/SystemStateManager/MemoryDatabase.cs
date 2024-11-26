using System.Collections.Generic;
using System.Linq;

namespace DevOptimal.SystemStateManager
{
    public class MemoryDatabase : IDatabase
    {
        private static readonly IList<ISnapshot> snapshots;

        static MemoryDatabase() => snapshots = new List<ISnapshot>();

        public void AddSnapshot(ISnapshot snapshot) => snapshots.Add(snapshot);

        public ISnapshot GetSnapshot(string id)
        {
            lock (snapshots)
            {
                return snapshots.FirstOrDefault(s => s.ID == id);
            }
        }

        public IEnumerable<ISnapshot> GetSnapshots() => snapshots;

        public void RemoveSnapshot(ISnapshot snapshot) => snapshots.Remove(snapshot);
    }
}
