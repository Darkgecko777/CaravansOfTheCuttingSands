using System;
using System.Collections.Generic;
using System.IO;
using Caravans.Play;
using UnityEditor;
using UnityEngine;

// UI and goods art is drawn much smaller than the source files. Mipmaps and
// uncompressed RGBA keep that reduction from turning into speckles.
public class UiTextureImporter : AssetPostprocessor
{
    public override int GetPostprocessOrder()
    {
        return 0;
    }

    static readonly string[] Roots =
    {
        "Assets/Art/Approved/",
        "Assets/Resources/Ui/",
        "Assets/Resources/FrontDoor/",
        "Assets/Resources/Goods/",
        "Assets/Resources/MapMarkers/",
        "Assets/Resources/Travel/",
        "Assets/Art/Travel/",
    };

    void OnPreprocessTexture()
    {
        if (!InScope(assetPath))
            return;
        Apply((TextureImporter)assetImporter);
    }

    [InitializeOnLoadMethod]
    static void CatchUpOnLoad()
    {
        EditorApplication.delayCall += CatchUp;
    }

    public static void CatchUp()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            return;

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[]
        {
            "Assets/Art/Approved",
            "Assets/Resources/Ui",
            "Assets/Resources/FrontDoor",
            "Assets/Resources/Goods",
            "Assets/Resources/MapMarkers",
            "Assets/Art/Travel",
        });
        var dirty = new List<string>();
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!InScope(path))
                continue;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || Matches(importer))
                continue;
            dirty.Add(path);
        }

        if (dirty.Count == 0)
            return;

        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < dirty.Count; i++)
                AssetDatabase.ImportAsset(dirty[i], ImportAssetOptions.ForceUpdate);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
    }

    static bool InScope(string path)
    {
        if (string.IsNullOrEmpty(path) || !path.EndsWith(".png"))
            return false;
        if (path.IndexOf("/_Source/", System.StringComparison.Ordinal) >= 0)
            return false;
        if (path.IndexOf("/Reference/", System.StringComparison.Ordinal) >= 0)
            return false;
        for (int i = 0; i < Roots.Length; i++)
        {
            if (path.StartsWith(Roots[i]))
                return true;
        }

        return false;
    }

    static bool NeedsReadable(string path)
    {
        string name = Path.GetFileName(path);
        if (name == "parchment-plain.png" || name == "duzhu-hide-panel.png"
            || name == "banner.png" || name == "hide-header.png" || name == "bone-frame.png")
            return true;
        if (name.StartsWith("resin-seal"))
            return true;
        return TravelProfile(path);
    }

    // Crest profiles read near, mid, and far once. Props, sky, and the back plain stay packed.
    static bool TravelProfile(string path)
    {
        string slash = string.IsNullOrEmpty(path) ? "" : path.Replace('\\', '/');
        if (!slash.Contains("/Art/Travel/"))
            return false;
        if (slash.Contains("/_reference/") || slash.Contains("/_Source/"))
            return false;
        return slash.Contains("/near/") || slash.Contains("/mid/") || slash.Contains("/far/");
    }

    static bool MenuBorder(string path, out Vector4 border)
    {
        string name = Path.GetFileName(path);
        if (name == "ui_wood_panel.png")
        {
            border = new Vector4(280f, 280f, 280f, 280f);
            return true;
        }

        if (name == "ui_bone_tick.png")
        {
            border = Vector4.zero;
            return true;
        }

        string slash = path.Replace('\\', '/');
        if (slash.StartsWith("Assets/Resources/Ui/Buttons/") && name.EndsWith(".png"))
        {
            border = new Vector4(24f, 24f, 24f, 24f);
            return true;
        }

        border = Vector4.zero;
        return false;
    }

    static bool TravelArt(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;
        return path.Replace('\\', '/').StartsWith("Assets/Art/Travel/");
    }

    static bool TravelAvatar(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;
        return path.Replace('\\', '/').EndsWith("Assets/Art/Travel/Avatar/avatar_t1_bird_01.png");
    }

    static bool TravelFeet(string path)
    {
        string slash = path.Replace('\\', '/');
        return slash.Contains("/props/") || slash.Contains("/far/") || slash.Contains("/mid/");
    }

    // Prop pixels per unit live in travel_kits.json. Landscape art stays at 100.
    static TravelKitsFile kitFile;
    static System.DateTime kitTime;

    static float TravelPropPpu(string path)
    {
        string slash = string.IsNullOrEmpty(path) ? "" : path.Replace('\\', '/');
        TravelKitsFile file = Kits();
        if (file == null || file.terrains == null)
            return 100f;
        for (int t = 0; t < file.terrains.Length; t++)
        {
            TravelKitsFile.Terrain terrain = file.terrains[t];
            if (terrain == null || terrain.propSpecs == null)
                continue;
            for (int i = 0; i < terrain.propSpecs.Length; i++)
            {
                TravelKitsFile.PropSpec spec = terrain.propSpecs[i];
                if (spec == null || spec.ppu <= 1f || string.IsNullOrEmpty(spec.path))
                    continue;
                string name = Path.GetFileName(spec.path);
                if (!name.EndsWith(".png"))
                    name += ".png";
                if (slash.EndsWith("/" + name))
                    return spec.ppu;
            }
        }

        return 100f;
    }

    static TravelKitsFile Kits()
    {
        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "data", "config", "travel_kits.json"));
        if (!File.Exists(path))
            return null;
        System.DateTime time = File.GetLastWriteTimeUtc(path);
        if (kitFile != null && time == kitTime)
            return kitFile;
        kitTime = time;
        kitFile = JsonUtility.FromJson<TravelKitsFile>(File.ReadAllText(path));
        return kitFile;
    }

    static bool TravelRepeat(string path)
    {
        string slash = path.Replace('\\', '/');
        return slash.Contains("/back/") || slash.Contains("/near/");
    }

    static bool TokenPivot(string path, out Vector2 pivot)
    {
        pivot = new Vector2(0.5f, 16f / 256f);
        if (string.IsNullOrEmpty(path))
            return false;
        string slash = path.Replace('\\', '/');
        return slash.StartsWith("Assets/Resources/MapMarkers/");
    }

    static void Apply(TextureImporter importer)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = true;
        importer.streamingMipmaps = false;
        importer.filterMode = FilterMode.Trilinear;
        importer.anisoLevel = 1;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.alphaIsTransparency = true;
        importer.isReadable = NeedsReadable(importer.assetPath);
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.crunchedCompression = false;
        importer.compressionQuality = 100;
        importer.maxTextureSize = 4096;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.textureType = TextureImporterType.Sprite;
        settings.spriteMode = (int)SpriteImportMode.Single;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.mipmapEnabled = true;
        settings.filterMode = FilterMode.Trilinear;
        settings.wrapMode = TextureWrapMode.Clamp;
        settings.alphaIsTransparency = true;
        settings.readable = importer.isReadable;
        settings.npotScale = TextureImporterNPOTScale.None;
        if (MenuBorder(importer.assetPath, out Vector4 border))
            settings.spriteBorder = border;
        if (TokenPivot(importer.assetPath, out Vector2 tokenPivot))
        {
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = tokenPivot;
        }

        importer.SetTextureSettings(settings);
        if (MenuBorder(importer.assetPath, out border))
            importer.spriteBorder = border;
        if (TokenPivot(importer.assetPath, out tokenPivot))
        {
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = tokenPivot;
            importer.SetTextureSettings(settings);
        }

        if (TravelAvatar(importer.assetPath))
        {
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            settings.mipmapEnabled = false;
            settings.filterMode = FilterMode.Bilinear;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spritePixelsPerUnit = 195f;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(0.468f, 0.02f);
            settings.wrapMode = TextureWrapMode.Clamp;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = 195f;
            importer.SetTextureSettings(settings);
        }
        else if (TravelArt(importer.assetPath))
        {
            settings.spriteMeshType = SpriteMeshType.FullRect;
            float ppu = TravelPropPpu(importer.assetPath);
            settings.spritePixelsPerUnit = ppu;
            settings.wrapMode = TravelRepeat(importer.assetPath) ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            if (TravelFeet(importer.assetPath))
            {
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(0.5f, 0f);
            }

            importer.wrapMode = settings.wrapMode;
            importer.spritePixelsPerUnit = ppu;
            importer.SetTextureSettings(settings);
        }

        SetPlatform(importer.GetDefaultPlatformTextureSettings(), importer);
        var standalone = importer.GetPlatformTextureSettings("Standalone");
        standalone.name = "Standalone";
        standalone.overridden = true;
        SetPlatform(standalone, importer);
    }

    static void SetPlatform(TextureImporterPlatformSettings platform, TextureImporter importer)
    {
        platform.maxTextureSize = 4096;
        platform.textureCompression = TextureImporterCompression.Uncompressed;
        platform.crunchedCompression = false;
        platform.compressionQuality = 100;
        platform.format = TextureImporterFormat.RGBA32;
        importer.SetPlatformTextureSettings(platform);
    }

    static bool Matches(TextureImporter importer)
    {
        if (importer.textureType != TextureImporterType.Sprite)
            return false;
        if (importer.spriteImportMode != SpriteImportMode.Single)
            return false;
        if (!TravelAvatar(importer.assetPath) && (!importer.mipmapEnabled || importer.filterMode != FilterMode.Trilinear))
            return false;
        if (importer.wrapMode != TextureWrapMode.Clamp)
        {
            if (!TravelRepeat(importer.assetPath) || importer.wrapMode != TextureWrapMode.Repeat)
                return false;
        }
        if (importer.textureCompression != TextureImporterCompression.Uncompressed || importer.crunchedCompression)
            return false;
        if (importer.isReadable != NeedsReadable(importer.assetPath))
            return false;
        if (importer.maxTextureSize < 4096)
            return false;
        if (MenuBorder(importer.assetPath, out Vector4 border))
        {
            Vector4 have = importer.spriteBorder;
            if (Mathf.Abs(have.x - border.x) > 0.5f
                || Mathf.Abs(have.y - border.y) > 0.5f
                || Mathf.Abs(have.z - border.z) > 0.5f
                || Mathf.Abs(have.w - border.w) > 0.5f)
                return false;
        }

        if (importer.assetPath.EndsWith("ui_meter_frame.png"))
        {
            float meter = importer.spriteBorder.x;
            if (meter < 78f || meter > 80f)
                return false;
        }

        if (TokenPivot(importer.assetPath, out Vector2 tokenPivot))
        {
            var have = new TextureImporterSettings();
            importer.ReadTextureSettings(have);
            if (have.spriteAlignment != (int)SpriteAlignment.Custom)
                return false;
            if (Mathf.Abs(have.spritePivot.x - tokenPivot.x) > 0.0001f
                || Mathf.Abs(have.spritePivot.y - tokenPivot.y) > 0.0001f)
                return false;
        }

        if (TravelAvatar(importer.assetPath))
        {
            if (importer.mipmapEnabled || importer.filterMode != FilterMode.Bilinear)
                return false;
            var avatar = new TextureImporterSettings();
            importer.ReadTextureSettings(avatar);
            if (avatar.mipmapEnabled || avatar.filterMode != FilterMode.Bilinear)
                return false;
            if (Mathf.Abs(avatar.spritePixelsPerUnit - 195f) > 0.01f)
                return false;
            if (avatar.spriteAlignment != (int)SpriteAlignment.Custom)
                return false;
            if (Mathf.Abs(avatar.spritePivot.x - 0.468f) > 0.0001f || Mathf.Abs(avatar.spritePivot.y - 0.02f) > 0.0001f)
                return false;
        }
        else if (TravelArt(importer.assetPath))
        {
            var have = new TextureImporterSettings();
            importer.ReadTextureSettings(have);
            if (Mathf.Abs(have.spritePixelsPerUnit - TravelPropPpu(importer.assetPath)) > 0.01f)
                return false;
            TextureWrapMode wrap = TravelRepeat(importer.assetPath) ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            if (importer.wrapMode != wrap || have.wrapMode != wrap)
                return false;
            if (TravelFeet(importer.assetPath))
            {
                if (have.spriteAlignment != (int)SpriteAlignment.Custom)
                    return false;
                if (Mathf.Abs(have.spritePivot.x - 0.5f) > 0.0001f || Mathf.Abs(have.spritePivot.y) > 0.0001f)
                    return false;
            }
        }

        TextureImporterPlatformSettings standalone = importer.GetPlatformTextureSettings("Standalone");
        return standalone.overridden
            && standalone.textureCompression == TextureImporterCompression.Uncompressed
            && !standalone.crunchedCompression
            && standalone.maxTextureSize >= 4096;
    }
}
