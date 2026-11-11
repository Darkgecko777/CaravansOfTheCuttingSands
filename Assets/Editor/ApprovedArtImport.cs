using System.IO;
using Caravans.FrontDoor;
using UnityEditor;
using UnityEngine;

public class ApprovedArtImport : AssetPostprocessor
{
    const string Root = "Assets/Art/Approved/";

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Root) || !assetPath.EndsWith(".png"))
            return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.alphaIsTransparency = true;
        importer.isReadable = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 2048;
        importer.spritePixelsPerUnit = PixelsPerUnit(assetPath);
        importer.spriteBorder = Border(assetPath);

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteMode = (int)SpriteImportMode.Single;
        settings.spritePixelsPerUnit = importer.spritePixelsPerUnit;
        settings.spriteBorder = importer.spriteBorder;
        settings.filterMode = FilterMode.Bilinear;
        settings.alphaIsTransparency = true;
        importer.SetTextureSettings(settings);

        TextureImporterPlatformSettings platform = importer.GetDefaultPlatformTextureSettings();
        platform.textureCompression = TextureImporterCompression.Uncompressed;
        platform.compressionQuality = 100;
        platform.maxTextureSize = 2048;
        platform.crunchedCompression = false;
        importer.SetPlatformTextureSettings(platform);
    }

    static float PixelsPerUnit(string path)
    {
        string name = Path.GetFileName(path);
        if (name == "ui_hide_bar.png")
            return 187f / 128f;
        if (name == "ui_hide_panel.png")
            return 2f;
        if (name == "ui_meter_frame.png")
            return 319f / 72f;
        return 100f;
    }

    // Unity border is left, bottom, right, top. The manifest lists left, right, top, bottom.
    static Vector4 Border(string path)
    {
        string name = Path.GetFileName(path);
        if (name == "ui_hide_panel.png")
            return new Vector4(150f, 150f, 150f, 150f);
        if (name == "ui_hide_bar.png")
            return new Vector4(145f, 45f, 145f, 45f);
        if (name == "ui_meter_frame.png")
            return new Vector4(175f, 0f, 175f, 0f);
        return Vector4.zero;
    }

    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        for (int i = 0; i < imported.Length; i++)
        {
            if (imported[i].StartsWith(Root) && imported[i].EndsWith(".png"))
            {
                ApprovedArtBind.Bind();
                return;
            }
        }
    }
}

[InitializeOnLoad]
public static class ApprovedArtBind
{
    const string AssetPath = "Assets/Resources/Ui/ApprovedArt.asset";

    static bool binding;

    static ApprovedArtBind()
    {
        EditorApplication.delayCall += Bind;
    }

    public static void Bind()
    {
        if (binding || EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        binding = true;
        try
        {
            Directory.CreateDirectory("Assets/Resources/Ui");
            var catalog = AssetDatabase.LoadAssetAtPath<ApprovedArt>(AssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ApprovedArt>();
                AssetDatabase.CreateAsset(catalog, AssetPath);
            }

            catalog.hidePanel = SpriteAt("ui_hide_panel.png");
            catalog.hideBar = SpriteAt("ui_hide_bar.png");
            catalog.goodsCell = SpriteAt("ui_goods_cell.png");
            catalog.goodsCellSelected = SpriteAt("ui_goods_cell_selected.png");
            catalog.sealBase = SpriteAt("ui_seal_base.png");
            catalog.sealMarket = SpriteAt("ui_seal_market.png");
            catalog.sealMarketHover = SpriteAt("ui_seal_market_hover.png");
            catalog.sealMarketPressed = SpriteAt("ui_seal_market_pressed.png");
            catalog.sealInn = SpriteAt("ui_seal_inn.png");
            catalog.sealInnHover = SpriteAt("ui_seal_inn_hover.png");
            catalog.sealInnPressed = SpriteAt("ui_seal_inn_pressed.png");
            catalog.sealHouse = SpriteAt("ui_seal_house.png");
            catalog.sealHouseHover = SpriteAt("ui_seal_house_hover.png");
            catalog.sealHousePressed = SpriteAt("ui_seal_house_pressed.png");
            catalog.sealGates = SpriteAt("ui_seal_gates.png");
            catalog.sealGatesHover = SpriteAt("ui_seal_gates_hover.png");
            catalog.sealGatesPressed = SpriteAt("ui_seal_gates_pressed.png");
            catalog.sealBuy = SpriteAt("ui_seal_buy.png");
            catalog.sealBuyHover = SpriteAt("ui_seal_buy_hover.png");
            catalog.sealBuyPressed = SpriteAt("ui_seal_buy_pressed.png");
            catalog.sealSell = SpriteAt("ui_seal_sell.png");
            catalog.sealSellHover = SpriteAt("ui_seal_sell_hover.png");
            catalog.sealSellPressed = SpriteAt("ui_seal_sell_pressed.png");
            catalog.iconScrubstone = SpriteAt("ui_icon_scrubstone.png");
            catalog.iconCapacity = SpriteAt("ui_icon_capacity.png");
            catalog.meterFrame = SpriteAt("ui_meter_frame.png");
            catalog.meterFill = SpriteAt("ui_meter_fill.png");
            catalog.settlementGeneric = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Approved/bg_settlement_generic.png");
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            binding = false;
        }
    }

    static Sprite SpriteAt(string file)
    {
        string path = "Assets/Art/Approved/" + file;
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
                return sprite;
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
