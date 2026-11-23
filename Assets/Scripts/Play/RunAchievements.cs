using System.Collections.Generic;

namespace Caravans.Play
{
    public sealed class RunAchievements
    {
        readonly HashSet<string> unlocked = new HashSet<string>(System.StringComparer.Ordinal);
        readonly Dictionary<string, int> fires = new Dictionary<string, int>(System.StringComparer.Ordinal);

        public void Unlock(string id)
        {
            if (string.IsNullOrEmpty(id))
                return;
            int count;
            fires.TryGetValue(id, out count);
            fires[id] = count + 1;
            unlocked.Add(id);
        }

        public bool IsUnlocked(string id)
        {
            return !string.IsNullOrEmpty(id) && unlocked.Contains(id);
        }

        public int UnlockCount(string id)
        {
            int count;
            if (string.IsNullOrEmpty(id) || !fires.TryGetValue(id, out count))
                return 0;
            return count;
        }

        public void Clear()
        {
            unlocked.Clear();
            fires.Clear();
        }
    }
}
