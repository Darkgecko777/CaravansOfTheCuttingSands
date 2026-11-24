using System;
using System.Collections.Generic;
using System.IO;
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
        return name.StartsWith("resin-seal");
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

        border = Vector4.zero;
        return false;
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
        if (!importer.mipmapEnabled || importer.filterMode != FilterMode.Trilinear)
            return false;
        if (importer.wrapMode != TextureWrapMode.Clamp)
            return false;
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

        TextureImporterPlatformSettings standalone = importer.GetPlatformTextureSettings("Standalone");
        return standalone.overridden
            && standalone.textureCompression == TextureImporterCompression.Uncompressed
            && !standalone.crunchedCompression
            && standalone.maxTextureSize >= 4096;
    }
}
