using Caravans.FrontDoor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Caravans.Play
{
    public static class PlayBootstrap
    {
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
            if (Object.FindAnyObjectByType<PlayPresenter>() != null)
                return;
            var go = new GameObject("Play");
            go.AddComponent<PlayPresenter>();
        }
    }
}
