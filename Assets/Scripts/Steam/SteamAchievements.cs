using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;

namespace Caravans.Steam
{
    // Local unlocks stay in RunAchievements. This only reports the same ids to Steam.
    public static class SteamAchievements
    {
        static readonly HashSet<string> pending = new HashSet<string>(StringComparer.Ordinal);
        static bool ready;

        public static bool Ready
        {
            get { return ready; }
        }

        public static event Action StatsReceived;

        internal static void Reset()
        {
            pending.Clear();
            ready = false;
            StatsReceived = null;
        }

        public static void MarkStatsReady()
        {
            ready = true;
            Flush();
            Action handler = StatsReceived;
            if (handler != null)
                handler();
        }

        public static void Report(string apiName)
        {
            if (string.IsNullOrEmpty(apiName) || !SteamManager.Initialized)
                return;
            if (!ready)
            {
                pending.Add(apiName);
                return;
            }

            Store(apiName);
        }

        public static void Push(string[] apiNames)
        {
            if (apiNames == null)
                return;
            for (int i = 0; i < apiNames.Length; i++)
                Report(apiNames[i]);
        }

        static void Flush()
        {
            if (!SteamManager.Initialized || !ready || pending.Count == 0)
                return;
            var copy = new List<string>(pending);
            pending.Clear();
            for (int i = 0; i < copy.Count; i++)
                Store(copy[i]);
        }

        static void Store(string apiName)
        {
            try
            {
                bool already;
                if (SteamUserStats.GetAchievement(apiName, out already) && already)
                    return;
                if (!SteamUserStats.SetAchievement(apiName) || !SteamUserStats.StoreStats())
                    pending.Add(apiName);
            }
            catch (Exception error)
            {
                pending.Add(apiName);
                Debug.LogWarning("Steam achievement was not stored (" + apiName + "). " + error.Message);
            }
        }
    }
}
