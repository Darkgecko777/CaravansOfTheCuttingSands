using System.Collections.Generic;
using Caravans.FrontDoor;
using Caravans.JarsRun;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Builds the title and house scenes inside the open Editor so they are real
// scenes the storm can outlive. SampleScene stays the run.
static class FrontDoorSceneSetup
{
    const string TitlePath = "Assets/Scenes/TitleScreen.unity";
    const string HousePath = "Assets/Scenes/HouseSelect.unity";
    const string MaterialPath = "Assets/Resources/FrontDoor/SandGrain.mat";

    [InitializeOnLoadMethod]
    static void Schedule()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged -= OnPlayMode;
        EditorApplication.playModeStateChanged += OnPlayMode;
    }

    static void OnPlayMode(PlayModeStateChange state)
    {
        // Drop the play-mode scene hook before the editor restores the open scene.
        if (state == PlayModeStateChange.ExitingPlayMode)
            JarsRunBootstrap.StopListening();
    }

    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        EditorApplication.update -= Tick;
        Ensure();
    }

    static void Ensure()
    {

        EnsureScene(TitlePath, "TitleScreen", new Color(0.45f, 0.32f, 0.18f, 1f), typeof(TitleScreen));
        EnsureScene(HousePath, "HouseSelect", new Color(0.12f, 0.08f, 0.05f, 1f), typeof(HouseSelectScreen));
        EnsureMaterial();
        EnsureBuildSettings();
        EnsurePlayStartsAtTitle();
    }

    static void EnsurePlayStartsAtTitle()
    {
        var title = AssetDatabase.LoadAssetAtPath<SceneAsset>(TitlePath);
        if (title == null || EditorSceneManager.playModeStartScene == title)
            return;
        EditorSceneManager.playModeStartScene = title;
        Debug.Log("Play mode starts at TitleScreen.");
    }

    static void EnsureScene(string path, string hostName, Color sky, System.Type screen)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.4f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = sky;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 100f;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        cameraObject.AddComponent<AudioListener>();
        var extra = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        extra.renderShadows = false;
        extra.renderPostProcessing = false;

        var host = new GameObject(hostName);
        host.AddComponent(screen);
        EditorSceneManager.SaveScene(scene, path);
        EditorSceneManager.CloseScene(scene, true);
    }

    static void EnsureMaterial()
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) != null)
            return;
        var shader = Shader.Find("Caravans/SandGrain");
        if (shader == null)
            return;
        var material = new Material(shader);
        AssetDatabase.CreateAsset(material, MaterialPath);
    }

    static int buildAttempts;

    static void EnsureBuildSettings()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TitlePath) == null
            || AssetDatabase.LoadAssetAtPath<SceneAsset>(HousePath) == null)
        {
            if (buildAttempts++ < 8)
                EditorApplication.delayCall += EnsureBuildSettings;
            return;
        }

        var current = EditorBuildSettings.scenes;
        var kept = new List<EditorBuildSettingsScene>();
        for (int i = 0; i < current.Length; i++)
        {
            if (current[i].path == TitlePath || current[i].path == HousePath)
                continue;
            kept.Add(current[i]);
        }

        var ordered = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(TitlePath, true),
            new EditorBuildSettingsScene(HousePath, true),
        };
        ordered.AddRange(kept);

        bool same = current.Length == ordered.Count;
        for (int i = 0; same && i < current.Length; i++)
        {
            if (current[i].path != ordered[i].path || current[i].enabled != ordered[i].enabled)
                same = false;
        }

        if (!same)
        {
            EditorBuildSettings.scenes = ordered.ToArray();
            Debug.Log("Front door scenes are first in the build.");
        }
    }
}
