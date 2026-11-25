using Caravans.Play;
using UnityEditor;
using UnityEngine;

// The travel sandbox loads Dune Sea art from Assets/Art. Those files are not in Resources.
[InitializeOnLoad]
public static class TravelDemoArt
{
    static TravelDemoArt()
    {
        TravelDemoSprites.ArtLoader = Load;
    }

    static Sprite Load(string resource)
    {
        if (string.IsNullOrEmpty(resource))
            return null;
        string path = resource.Replace('\\', '/');
        if (!path.StartsWith("Assets/"))
            path = "Assets/" + path;
        if (!path.EndsWith(".png"))
            path += ".png";
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
