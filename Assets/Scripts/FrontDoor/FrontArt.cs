using TMPro;
using UnityEngine;

namespace Caravans.FrontDoor
{
    public static class FrontArt
    {
        static Sprite woodPanel;
        static Sprite hoverStrip;
        static Sprite boneTick;
        static Sprite parchment;
        static Sprite hidePanel;
        static Sprite banner;
        static Sprite hideHeader;
        static Sprite boneFrame;
        static Sprite resinSeal;
        static Sprite resinSealHover;
        static Sprite resinSealSelected;
        static Sprite resinSealGate;
        static Sprite resinSealGateHover;
        static Sprite resinSealGateSelected;
        static Texture2D title;
        static Texture2D settlement;
        static Texture2D grain;
        static Material sand;
        static TMP_FontAsset bold;
        static TMP_FontAsset regular;
        static TMP_FontAsset display;
        static AudioClip titleTheme;

        public static Sprite WoodPanel()
        {
            if (woodPanel == null)
                woodPanel = Resources.Load<Sprite>("FrontDoor/ui_wood_panel");
            return woodPanel;
        }

        public static Sprite HoverStrip()
        {
            if (hoverStrip == null)
                hoverStrip = Resources.Load<Sprite>("FrontDoor/ui_hover_strip");
            return hoverStrip;
        }

        public static Sprite BoneTick()
        {
            if (boneTick == null)
                boneTick = Resources.Load<Sprite>("FrontDoor/ui_bone_tick");
            return boneTick;
        }

        public static Sprite Parchment()
        {
            // Plain document sheet. The torn edge stays in the slice border.
            return parchment = parchment != null
                ? parchment
                : Slice("Ui/parchment-plain", new Vector4(72f, 72f, 72f, 72f), 220f);
        }

        public static Sprite HidePanel()
        {
            ApprovedArt art = ApprovedArt.Load();
            if (art != null && art.hidePanel != null)
                return art.hidePanel;
            // Even rim stays in the slice border. The grain in the centre stretches.
            return hidePanel = hidePanel != null
                ? hidePanel
                : Slice("Ui/duzhu-hide-panel", new Vector4(48f, 48f, 48f, 48f), 128f);
        }

        public static Sprite Banner()
        {
            // Knobs stay in the side caps. The crystal centre stretches.
            return banner = banner != null
                ? banner
                : Slice("Ui/banner", new Vector4(200f, 10f, 200f, 10f), 140f);
        }

        public static Sprite HideHeader()
        {
            // Curls stay in the side caps. The blank centre stretches.
            return hideHeader = hideHeader != null
                ? hideHeader
                : Slice("Ui/hide-header", new Vector4(170f, 36f, 170f, 36f), 300f);
        }

        public static Sprite BoneFrame()
        {
            // Borders sit outside the sinew at each corner. 230 pixels per unit keeps a joint near 96 screen pixels.
            return boneFrame = boneFrame != null
                ? boneFrame
                : Slice("Ui/bone-frame", new Vector4(220f, 210f, 210f, 220f), 230f);
        }

        public static Sprite ResinSeal()
        {
            return resinSeal = resinSeal != null ? resinSeal : Slice("Ui/resin-seal", Vector4.zero);
        }

        public static Sprite ResinSealHover()
        {
            return resinSealHover = resinSealHover != null ? resinSealHover : Slice("Ui/resin-seal-hover", Vector4.zero);
        }

        public static Sprite ResinSealSelected()
        {
            return resinSealSelected = resinSealSelected != null ? resinSealSelected : Slice("Ui/resin-seal-selected", Vector4.zero);
        }

        public static Sprite ResinSealGate()
        {
            return resinSealGate = resinSealGate != null ? resinSealGate : Slice("Ui/resin-seal-gate", Vector4.zero);
        }

        public static Sprite ResinSealGateHover()
        {
            return resinSealGateHover = resinSealGateHover != null ? resinSealGateHover : Slice("Ui/resin-seal-gate-hover", Vector4.zero);
        }

        public static Sprite ResinSealGateSelected()
        {
            return resinSealGateSelected = resinSealGateSelected != null ? resinSealGateSelected : Slice("Ui/resin-seal-gate-selected", Vector4.zero);
        }

        public static Texture2D TitleTexture()
        {
            if (title == null)
                title = Resources.Load<Texture2D>("FrontDoor/title_screen");
            return title;
        }

        public static Texture2D SettlementDesert()
        {
            if (settlement == null)
                settlement = Resources.Load<Texture2D>("FrontDoor/settlement-desert");
            return settlement;
        }

        public static AudioClip TitleTheme()
        {
            if (titleTheme == null)
                titleTheme = Resources.Load<AudioClip>("Audio/Music/TitleTheme");
            return titleTheme;
        }

        public static TMP_FontAsset Bold()
        {
            if (bold == null)
                bold = MakeFont("FrontDoor/Fonts/BonaNovaSC-Bold");
            return bold;
        }

        public static TMP_FontAsset Regular()
        {
            if (regular == null)
                regular = MakeFont("FrontDoor/Fonts/BonaNovaSC-Regular");
            return regular;
        }

        public static TMP_FontAsset Display()
        {
            if (display == null)
            {
                display = MakeFont("FrontDoor/Fonts/CuttingSands-Regular");
                TMP_FontAsset body = Regular();
                if (display != null && body != null)
                {
                    if (display.fallbackFontAssetTable == null)
                        display.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
                    display.fallbackFontAssetTable.Add(body);
                }
            }

            return display;
        }

        public static Material SandMaterial()
        {
            if (sand != null)
                return sand;
            var loaded = Resources.Load<Material>("FrontDoor/SandGrain");
            var shader = Shader.Find("Caravans/SandGrain");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            sand = loaded != null ? new Material(loaded) : new Material(shader);
            sand.mainTexture = GrainTexture();
            return sand;
        }

        public static Texture2D GrainTexture()
        {
            if (grain != null)
                return grain;
            const int size = 22;
            grain = new Texture2D(size, size, TextureFormat.RGBA32, false);
            grain.name = "SandGrain";
            grain.wrapMode = TextureWrapMode.Clamp;
            grain.filterMode = FilterMode.Bilinear;
            float center = (size - 1) * 0.5f;
            float maxRadius = center * 0.92f;
            const float softness = 0.58f;
            const float irregularity = 0.30f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / maxRadius;
                    float dy = (y - center) / maxRadius;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);
                    float n = SmoothNoise.Sample(Mathf.Cos(angle) * 3f * 0.22f, Mathf.Sin(angle) * 3f * 0.22f, 4);
                    float radiusMod = Mathf.Max(0.55f, 1f + n * irregularity * 0.45f);
                    float effective = dist / radiusMod;
                    float edge = Mathf.Lerp(0.55f, 0.15f, softness);
                    float alpha = 0f;
                    if (effective < 1f)
                    {
                        float t = Mathf.Clamp01((1f - effective) / Mathf.Max(1f - edge, 0.05f));
                        alpha = t * t * (3f - 2f * t);
                        alpha = Mathf.Pow(alpha, Mathf.Lerp(0.7f, 1.6f, softness));
                    }

                    float warm = 0.78f + SmoothNoise.Sample(x * 0.4f * 0.22f, y * 0.4f * 0.22f, 8) * 0.10f;
                    grain.SetPixel(x, y, new Color(0.95f, warm, warm * 0.55f, alpha));
                }
            }

            grain.Apply(false, true);
            return grain;
        }

        static Sprite Slice(string path, Vector4 border, float pixelsPerUnit = 100f)
        {
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null || !texture.isReadable)
                return null;
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                border,
                false);
        }

        static TMP_FontAsset MakeFont(string path)
        {
            var source = Resources.Load<Font>(path);
            if (source == null)
                return null;
            return TMP_FontAsset.CreateFontAsset(source);
        }
    }
}
