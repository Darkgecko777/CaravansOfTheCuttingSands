// Adapted from the Steamworks.NET SteamManager, which is released into the public domain.
// App ID 4765690. A missing Steam client logs a warning and leaves play running.

using System;
using Steamworks;
using UnityEngine;

namespace Caravans.Steam
{
    [DisallowMultipleComponent]
    public sealed class SteamManager : MonoBehaviour
    {
        public const uint AppId = 4765690;

        static SteamManager instance;
        static bool initialized;
        static bool bootstrapped;

        Callback<UserStatsReceived_t> statsReceived;
        int statsWait;

        public static bool Initialized
        {
            get { return initialized; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            instance = null;
            initialized = false;
            bootstrapped = false;
            SteamAchievements.Reset();
        }

#if UNITY_EDITOR
        internal static bool EditorSteamOff()
        {
            return Environment.GetEnvironmentVariable("CARAVANS_EDITOR_NO_STEAM") == "1";
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
#if UNITY_EDITOR
            if (EditorSteamOff())
                return;
#endif
            if (!Application.isPlaying || bootstrapped)
                return;
            bootstrapped = true;
            var host = new GameObject("SteamManager");
            host.AddComponent<SteamManager>();
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            TryInit();
        }

        void TryInit()
        {
            if (!Packsize.Test())
                Debug.LogWarning("[Steamworks.NET] Packsize test failed. Play continues without Steam.");
            if (!DllCheck.Test())
                Debug.LogWarning("[Steamworks.NET] DllCheck failed. Play continues without Steam.");

            try
            {
                // A shipped exe may bounce through Steam. The editor must not relaunch the library copy.
                if (!Application.isEditor && SteamAPI.RestartAppIfNecessary(new AppId_t(AppId)))
                {
                    Application.Quit();
                    return;
                }
            }
            catch (DllNotFoundException error)
            {
                Debug.LogWarning("[Steamworks.NET] Steam is not available. Play continues. " + error.Message);
                return;
            }
            catch (Exception error)
            {
                Debug.LogWarning("[Steamworks.NET] Steam restart check failed. Play continues. " + error.Message);
            }

            try
            {
                initialized = SteamAPI.Init();
            }
            catch (DllNotFoundException error)
            {
                initialized = false;
                Debug.LogWarning("[Steamworks.NET] Steam is not available. Play continues. " + error.Message);
                return;
            }
            catch (Exception error)
            {
                initialized = false;
                Debug.LogWarning("[Steamworks.NET] SteamAPI.Init failed. Play continues. " + error.Message);
                return;
            }

            if (!initialized)
            {
                Debug.LogWarning("[Steamworks.NET] SteamAPI.Init failed. Play continues without Steam.");
                return;
            }

            // SDK 1.65 removed RequestCurrentStats. The client syncs stats on launch.
            // RequestUserStats for this user still posts UserStatsReceived_t.
            statsReceived = Callback<UserStatsReceived_t>.Create(OnStatsReceived);
            SteamUserStats.RequestUserStats(SteamUser.GetSteamID());
        }

        void OnStatsReceived(UserStatsReceived_t data)
        {
            if (!initialized)
                return;
            if (data.m_eResult != EResult.k_EResultOK)
            {
                Debug.LogWarning("Steam stats were not received (" + data.m_eResult + "). Play continues.");
                return;
            }

            AppId_t app = SteamUtils.GetAppID();
            if ((data.m_nGameID & 0xFFFFFFFF) != app.m_AppId)
                return;
            SteamAchievements.MarkStatsReady();
        }

        void Update()
        {
            if (!initialized)
                return;
            SteamAPI.RunCallbacks();
            if (SteamAchievements.Ready || statsWait >= 30)
                return;
            statsWait++;
            if (statsWait < 30)
                return;
            Debug.LogWarning("Steam stats callback did not arrive. Storing achievements with the stats the client already synced.");
            SteamAchievements.MarkStatsReady();
        }

        void OnDestroy()
        {
            if (instance != this)
                return;
            instance = null;
            statsReceived = null;
            if (!initialized)
                return;
            initialized = false;
            SteamAPI.Shutdown();
        }
    }
}
