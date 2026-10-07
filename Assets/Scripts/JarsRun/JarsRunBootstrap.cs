using UnityEngine;

namespace Caravans.JarsRun
{
    public static class JarsRunBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Start()
        {
            if (Object.FindAnyObjectByType<JarsRunPresenter>() != null)
                return;
            var go = new GameObject("JarsRun");
            go.AddComponent<JarsRunPresenter>();
        }
    }
}
