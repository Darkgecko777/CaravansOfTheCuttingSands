using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

// Build Player packs Assets only. The session reads the economy sheets,
// data/config, and the encounter charts from the folder above Application.dataPath.
// In a player that folder is the one that contains the exe.
public static class ShipPlayData
{
    [PostProcessBuild(0)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        string built = Path.GetDirectoryName(pathToBuiltProject);
        if (string.IsNullOrEmpty(built))
            throw new IOException("Player build has no output folder.");
        CopyTo(built);
        Debug.Log("Play data copied beside the player: " + built);
    }

    public static void CopyTo(string destinationRoot)
    {
        string project = Path.GetDirectoryName(Application.dataPath);
        CopyFile(project, destinationRoot, "Encounter_IDs.md");
        CopyFile(project, destinationRoot, "Route_Tags.md");
        CopyFile(project, destinationRoot, Path.Combine("Docs", "Economy_Weight_Price.csv"));
        CopyFile(project, destinationRoot, Path.Combine("Docs", "Economy_Eat_Per_Day.csv"));
        CopyConfig(Path.Combine(project, "data", "config"), Path.Combine(destinationRoot, "data", "config"));
    }

    static void CopyFile(string project, string destinationRoot, string relative)
    {
        string from = Path.Combine(project, relative);
        if (!File.Exists(from))
            throw new FileNotFoundException("Play data was not found.", from);
        string to = Path.Combine(destinationRoot, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(to));
        File.Copy(from, to, true);
    }

    static void CopyConfig(string from, string to)
    {
        if (!Directory.Exists(from))
            throw new DirectoryNotFoundException(from);
        string[] files = Directory.GetFiles(from, "*.json");
        if (files.Length == 0)
            throw new FileNotFoundException("data/config has no json.", from);
        Directory.CreateDirectory(to);
        for (int i = 0; i < files.Length; i++)
            File.Copy(files[i], Path.Combine(to, Path.GetFileName(files[i])), true);
    }
}
