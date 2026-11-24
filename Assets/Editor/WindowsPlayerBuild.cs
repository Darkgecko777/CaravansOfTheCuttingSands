using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.WindowsStandalone;
using UnityEngine;

// Caravans/Build Windows writes the upload player. It does not copy steam_appid.txt.
public static class WindowsPlayerBuild
{
    const string Company = "Uncanny Mercantile";
    const string Product = "Caravans of the Cutting Sands";

    [MenuItem("Caravans/Build Windows")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            Fail("Leave Play mode before building.");
            return;
        }

        if (PlayerSettings.productName != Product)
        {
            Fail("productName must stay " + Product + ".");
            return;
        }

        if (PlayerSettings.companyName != Company)
        {
            PlayerSettings.companyName = Company;
            AssetDatabase.SaveAssets();
        }

        string[] scenes = EnabledScenes();
        if (scenes.Length != 2
            || !scenes[0].EndsWith("TitleScreen.unity")
            || !scenes[1].EndsWith("SampleScene.unity"))
        {
            Fail("Build settings must enable TitleScreen, then SampleScene.");
            return;
        }

        string project = Path.GetDirectoryName(Application.dataPath);
        string folder = Path.Combine(project, "Builds", "Windows");
        Directory.CreateDirectory(folder);
        string exe = Path.Combine(folder, Product + ".exe");
        bool previousPdb = UserBuildSettings.copyPDBFiles;
        UserBuildSettings.copyPDBFiles = false;
        BuildReport report = null;
        try
        {
            report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = exe,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            });
        }
        finally
        {
            UserBuildSettings.copyPDBFiles = previousPdb;
            RemoveAppId(folder);
        }

        if (report == null || report.summary.result != BuildResult.Succeeded)
        {
            string result = report == null ? "no report" : report.summary.result.ToString();
            Fail("Windows build failed (" + result + ").");
            return;
        }

        if (File.Exists(Path.Combine(folder, "steam_appid.txt")))
        {
            Fail("steam_appid.txt is still in the player folder.");
            return;
        }

        Debug.Log("Windows player written to " + exe);
    }

    static string[] EnabledScenes()
    {
        var scenes = new System.Collections.Generic.List<string>();
        EditorBuildSettingsScene[] listed = EditorBuildSettings.scenes;
        for (int i = 0; i < listed.Length; i++)
        {
            if (listed[i].enabled)
                scenes.Add(listed[i].path);
        }

        return scenes.ToArray();
    }

    static void RemoveAppId(string folder)
    {
        if (!Directory.Exists(folder))
            return;
        string[] found = Directory.GetFiles(folder, "steam_appid.txt", SearchOption.AllDirectories);
        for (int i = 0; i < found.Length; i++)
        {
            Debug.LogWarning("Removing steam_appid.txt from the player folder: " + found[i]);
            File.Delete(found[i]);
        }
    }

    static void Fail(string message)
    {
        Debug.LogError(message);
        EditorUtility.DisplayDialog("Caravans/Build Windows", message, "OK");
    }
}
