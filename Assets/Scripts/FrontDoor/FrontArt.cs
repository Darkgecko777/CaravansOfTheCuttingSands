using TMPro;
using UnityEngine;

namespace Caravans.FrontDoor
{
    public static class FrontArt
    {
        static Sprite buttonPanel;
        static Sprite menuFrame;
        static Sprite plateQuiet;
        static Sprite plateHot;
        static Sprite platePressed;
        static Texture2D title;
        static Texture2D grain;
        static Material sand;
        static TMP_FontAsset bold;
        static TMP_FontAsset regular;
        static AudioClip wind;

        public static Sprite ButtonPanel()
        {
            return buttonPanel = buttonPanel != null ? buttonPanel : Slice("FrontDoor/button_panel", new Vector4(30f, 30f, 30f, 30f));
        }

        public static Sprite MenuFrame()
        {
            return menuFrame = menuFrame != null ? menuFrame : Slice("FrontDoor/menu_frame", Vector4.zero);
        }

        public static Sprite PlateQuiet()
        {
            return plateQuiet = plateQuiet != null ? plateQuiet : Slice("FrontDoor/plate_quiet", new Vector4(22f, 22f, 22f, 22f));
        }

        public static Sprite PlateHot()
        {
            return plateHot = plateHot != null ? plateHot : Slice("FrontDoor/plate_hot", new Vector4(22f, 22f, 22f, 22f));
        }

        public static Sprite PlatePressed()
        {
            return platePressed = platePressed != null ? platePressed : Slice("FrontDoor/plate_pressed", new Vector4(22f, 22f, 22f, 22f));
        }

        public static Texture2D TitleTexture()
        {
            if (title == null)
                title = Resources.Load<Texture2D>("FrontDoor/title_screen");
            return title;
        }

        public static AudioClip Wind()
        {
            if (wind == null)
                wind = Resources.Load<AudioClip>("FrontDoor/wind_sound");
            return wind;
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

        static Sprite Slice(string path, Vector4 border)
        {
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null || !texture.isReadable)
                return null;
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                border,
                false);
        }

        static TMP_FontAsset MakeFont(string path)
        {
            var source = Resources.Load<Font>(path);
            if (source == null)
                source = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return TMP_FontAsset.CreateFontAsset(source);
        }
    }
}
