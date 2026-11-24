using System.Collections.Generic;
using Caravans.Steam;

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
            try
            {
                SteamAchievements.Report(id);
            }
            catch (System.Exception)
            {
                // The local flag is already set. A missing Steam client must not throw.
            }
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

        internal void Export(out string[] ids, out int[] counts)
        {
            var list = new System.Collections.Generic.List<string>(unlocked);
            list.Sort(System.StringComparer.Ordinal);
            ids = list.ToArray();
            counts = new int[ids.Length];
            for (int i = 0; i < ids.Length; i++)
                counts[i] = UnlockCount(ids[i]);
        }

        internal void Import(string[] ids, int[] counts)
        {
            Clear();
            if (ids == null || counts == null || ids.Length != counts.Length)
                return;
            for (int i = 0; i < ids.Length; i++)
            {
                if (string.IsNullOrEmpty(ids[i]) || counts[i] < 1)
                    continue;
                unlocked.Add(ids[i]);
                fires[ids[i]] = counts[i];
            }
        }
    }
}
