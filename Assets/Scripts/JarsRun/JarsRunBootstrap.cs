using Caravans.FrontDoor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Caravans.JarsRun
{
    public static class JarsRunBootstrap
    {
        // AfterSceneLoad runs for the first scene only. Play starts on the title,
        // so the city has to arm itself when Confirm loads SampleScene later.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Start()
        {
            TryStart();
        }

        public static void StopListening()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!Application.isPlaying)
                return;
            TryStart();
        }

        static void TryStart()
        {
            if (!Gate.RunConfirmed)
                return;
            if (SceneManager.GetActiveScene().name != Gate.PlayScene)
                return;
            if (Object.FindAnyObjectByType<JarsRunPresenter>() != null)
                return;
            var go = new GameObject("JarsRun");
            go.AddComponent<JarsRunPresenter>();
        }
    }
}
