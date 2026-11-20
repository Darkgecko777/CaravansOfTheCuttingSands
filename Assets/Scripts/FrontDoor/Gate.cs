using UnityEngine;
using UnityEngine.SceneManagement;

namespace Caravans.FrontDoor
{
    public static class Gate
    {
        public const string TitleScene = "TitleScreen";
        public const string PlayScene = "SampleScene";

        public static bool RunConfirmed { get; private set; }

        // Enter Play Mode keeps statics when domain reload is off. This still runs.
        static bool sessionRouted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            ClearRun();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ResetBeforeScene()
        {
            ClearRun();
        }

        static void ClearRun()
        {
            RunConfirmed = false;
            sessionRouted = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (sessionRouted)
                return;
            sessionRouted = true;
            if (RunConfirmed)
                return;
            if (Object.FindAnyObjectByType<TitleScreen>() != null)
                return;
            ShowTitle();
        }

        public static void ShowTitle()
        {
            if (Application.CanStreamedLevelBeLoaded(TitleScene))
            {
                SceneManager.LoadScene(TitleScene);
                return;
            }

            TitleScreen.Spawn();
        }

        public static void ReturnToTitle()
        {
            RunConfirmed = false;
            ShowTitle();
        }

        public static void ConfirmRun()
        {
            RunConfirmed = true;
            SceneManager.LoadScene(PlayScene);
        }
    }
}
