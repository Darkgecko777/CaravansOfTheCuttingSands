using System.Collections.Generic;
using UnityEngine;

namespace Caravans.Play
{
    // Placeholder shapes for a terrain kit. A Resources sprite wins when the file exists.
    public static class TravelDemoSprites
    {
        static readonly Dictionary<string, Sprite> loaded = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, Sprite> made = new Dictionary<string, Sprite>();
        static readonly List<Object> owned = new List<Object>();

        // Editor play loads travel art that lives outside Resources. The player has no hook.
        public static System.Func<string, Sprite> ArtLoader;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            Clear();
        }

        public static void Clear()
        {
            for (int i = 0; i < owned.Count; i++)
            {
                if (owned[i] != null)
                    Object.Destroy(owned[i]);
            }

            owned.Clear();
            loaded.Clear();
            made.Clear();
        }

        public static Sprite LoadOrNull(string resource)
        {
            if (string.IsNullOrEmpty(resource))
                return null;
            Sprite sprite;
            if (loaded.TryGetValue(resource, out sprite))
                return sprite;
            sprite = Resources.Load<Sprite>(resource);
            if (sprite == null && ArtLoader != null)
                sprite = ArtLoader(resource);
            loaded[resource] = sprite;
            return sprite;
        }

        public static Sprite Feature(string terrain, int layer, int variant)
        {
            string key = "f:" + terrain + ":" + layer + ":" + (variant % 3);
            return Cached(key, delegate
            {
                var sheet = new Sheet(180, 120);
                Color body = Scale(TerrainColor(terrain), layer == 0 ? 1.15f : layer == 1 ? 1f : 0.78f);
                DrawFeature(sheet, terrain, layer, variant % 3, body);
                sheet.FadeBase(0.28f);
                return sheet.ToSprite(new Vector2(0.5f, 0.04f), 64f, false);
            });
        }

        public static Sprite Ground(string terrain)
        {
            string key = "g:" + terrain;
            return Cached(key, delegate
            {
                // Taller than the ground strip so the vertical grade is not repeated.
                var sheet = new Sheet(128, 160);
                DrawGround(sheet, terrain);
                return sheet.ToSprite(new Vector2(0.5f, 0.5f), 32f, true);
            });
        }

        public static Sprite Prop(string id)
        {
            string key = "p:" + id;
            return Cached(key, delegate
            {
                var sheet = new Sheet(96, 120);
                DrawProp(sheet, id);
                sheet.FadeBase(0.22f);
                return sheet.ToSprite(new Vector2(0.5f, 0.02f), 64f, false);
            });
        }

        public static Sprite Caravan()
        {
            return Cached("caravan", delegate
            {
                var sheet = new Sheet(240, 160);
                DrawCaravan(sheet);
                sheet.FadeBase(0.12f);
                return sheet.ToSprite(new Vector2(0.5f, 0.02f), 64f, false);
            });
        }

        public static Sprite Solid()
        {
            return Cached("solid", delegate
            {
                var sheet = new Sheet(4, 4);
                var white = new Color(1f, 1f, 1f, 1f);
                for (int y = 0; y < sheet.Height; y++)
                {
                    for (int x = 0; x < sheet.Width; x++)
                        sheet.Set(x, y, white);
                }

                return sheet.ToSprite(new Vector2(0.5f, 0.5f), 4f, false);
            });
        }

        public static Sprite Shadow()
        {
            return Cached("shadow", delegate
            {
                var sheet = new Sheet(64, 32);
                float cx = 31.5f;
                float cy = 15.5f;
                for (int y = 0; y < sheet.Height; y++)
                {
                    for (int x = 0; x < sheet.Width; x++)
                    {
                        float nx = (x - cx) / 30f;
                        float ny = (y - cy) / 12f;
                        float fall = 1f - (nx * nx + ny * ny);
                        float alpha = Mathf.Clamp01(fall);
                        alpha = alpha * alpha * 0.5f;
                        sheet.Set(x, y, new Color(0f, 0f, 0f, alpha));
                    }
                }

                return sheet.ToSprite(new Vector2(0.5f, 0.5f), 32f, false);
            });
        }

        public static Sprite Haze()
        {
            return Cached("haze", delegate
            {
                var sheet = new Sheet(64, 64);
                DrawHaze(sheet);
                return sheet.ToSprite(new Vector2(0.5f, 0.5f), 32f, true);
            });
        }

        public static Texture2D SkyTexture()
        {
            var texture = new Texture2D(4, 128, TextureFormat.RGBA32, false);
            texture.name = "TravelSky";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.hideFlags = HideFlags.HideAndDontSave;
            owned.Add(texture);
            return texture;
        }

        public static Sprite Sky(Texture2D texture)
        {
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
            sprite.name = "TravelSky";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            owned.Add(sprite);
            return sprite;
        }

        public static void PaintSky(Texture2D texture, Color horizon, Color zenith, float horizonV)
        {
            int height = texture.height;
            var pixels = new Color[texture.width * height];
            for (int y = 0; y < height; y++)
            {
                float v = y / (float)(height - 1);
                float t = Mathf.InverseLerp(horizonV, 1f, v);
                t = t * t * (3f - 2f * t);
                Color color = Color.Lerp(horizon, zenith, Mathf.Clamp01(t));
                for (int x = 0; x < texture.width; x++)
                    pixels[y * texture.width + x] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply(false);
        }

        public static Color TerrainColor(string terrain)
        {
            switch (terrain)
            {
                case "Dune Sea":
                    return new Color(0.80f, 0.60f, 0.32f, 1f);
                case "Salt Flats":
                    return new Color(0.78f, 0.77f, 0.72f, 1f);
                case "Badlands":
                    return new Color(0.58f, 0.30f, 0.20f, 1f);
                case "Dry Scrub":
                    return new Color(0.46f, 0.44f, 0.24f, 1f);
                case "Highlands":
                    return new Color(0.46f, 0.50f, 0.58f, 1f);
                case "Coast":
                    return new Color(0.70f, 0.56f, 0.36f, 1f);
                default:
                    return new Color(0.62f, 0.50f, 0.34f, 1f);
            }
        }

        static Sprite Cached(string key, System.Func<Sprite> build)
        {
            Sprite sprite;
            if (made.TryGetValue(key, out sprite) && sprite != null)
                return sprite;
            sprite = build();
            made[key] = sprite;
            return sprite;
        }

        static void DrawFeature(Sheet sheet, string terrain, int layer, int variant, Color body)
        {
            int baseline = 8;
            if (terrain == "Highlands")
            {
                sheet.Triangle(20 + variant * 8, baseline, 90, 100 - layer * 8, 160 - variant * 6, baseline, body);
                if (variant != 1)
                    sheet.Triangle(70, baseline, 120, 78, 168, baseline, Scale(body, 0.82f));
                return;
            }

            if (terrain == "Badlands")
            {
                sheet.Rect(30, baseline, 120, 28 + variant * 6, body);
                sheet.Rect(48, baseline + 24, 70, 22, Scale(body, 0.8f));
                sheet.Rect(78, baseline + 40, 28 + variant * 8, 26, Scale(body, 1.08f));
                return;
            }

            if (terrain == "Dry Scrub")
            {
                int n = 3 + variant;
                for (int i = 0; i < n; i++)
                {
                    int x = 28 + i * 36;
                    sheet.Rect(x + 10, baseline, 6, 16, Scale(body, 0.7f));
                    sheet.Ellipse(x + 13, baseline + 22, 16 + (i % 2) * 4, 12, body);
                }

                return;
            }

            if (terrain == "Salt Flats")
            {
                sheet.Ellipse(90, baseline + 10, 78, 14, body);
                sheet.Triangle(70 + variant * 6, baseline + 8, 90, baseline + 48, 112, baseline + 8, Scale(body, 0.92f));
                return;
            }

            if (terrain == "Coast")
            {
                sheet.Ellipse(80, baseline + 16, 70, 18, body);
                Color water = new Color(0.28f, 0.48f, 0.55f, 0.9f);
                sheet.Ellipse(120, baseline + 6, 40, 8, water);
                return;
            }

            sheet.Ellipse(78, baseline + 18, 70 - layer * 6, 28 + variant * 4, body);
            sheet.Ellipse(118, baseline + 16, 36, 20, Scale(body, 0.78f));
            if (variant == 2)
                sheet.Ellipse(46, baseline + 14, 28, 16, Scale(body, 1.05f));
        }

        static void DrawGround(Sheet sheet, string terrain)
        {
            Color body = TerrainColor(terrain);
            Color deep = Scale(body, 0.72f);
            for (int y = 0; y < sheet.Height; y++)
            {
                float t = y / (float)sheet.Height;
                Color row = Color.Lerp(deep, body, t);
                if (terrain == "Coast" && t < 0.42f)
                    row = Color.Lerp(new Color(0.22f, 0.42f, 0.50f, 1f), row, t / 0.42f);
                for (int x = 0; x < sheet.Width; x++)
                {
                    // Periodic in x so a tiled strip has no vertical seam.
                    float u = x / (float)sheet.Width;
                    float wave = Mathf.Sin((u * 3f + y * 0.04f) * Mathf.PI * 2f) * 0.035f;
                    wave += Mathf.Sin((u * 7f + y * 0.015f) * Mathf.PI * 2f) * 0.02f;
                    sheet.Set(x, y, Scale(row, 1f + wave));
                }
            }
        }

        static void DrawProp(Sheet sheet, string id)
        {
            string name = string.IsNullOrEmpty(id) ? "" : id;
            Color hide = new Color(0.55f, 0.40f, 0.24f, 1f);
            Color stone = new Color(0.62f, 0.58f, 0.50f, 1f);
            bool structure = name.Contains("post") || name.Contains("gate") || name.Contains("wall")
                || name.Contains("fort") || name.Contains("city") || name.Contains("house")
                || name.Contains("hut") || name.Contains("embassy") || name.Contains("hall")
                || name.Contains("mine") || name.Contains("pen") || name.Contains("shed")
                || name.Contains("rack") || name.Contains("camp") || name.Contains("tent");
            if (name.Contains("tent") || name.Contains("camp"))
            {
                sheet.Triangle(18, 8, 48, 96, 78, 8, hide);
                return;
            }

            if (structure)
            {
                sheet.Rect(28, 6, 40, 70, stone);
                sheet.Triangle(18, 72, 48, 112, 78, 72, Scale(hide, 0.85f));
                sheet.Rect(40, 28, 12, 18, new Color(0.2f, 0.16f, 0.12f, 1f));
                return;
            }

            Color shell = new Color(0.72f, 0.62f, 0.48f, 1f);
            sheet.Ellipse(48, 22, 30, 16, shell);
            sheet.Ellipse(36, 30, 14, 18, Scale(shell, 0.86f));
            sheet.Ellipse(60, 28, 12, 14, Scale(shell, 1.08f));
        }

        static void DrawCaravan(Sheet sheet)
        {
            Color hide = new Color(0.72f, 0.52f, 0.30f, 1f);
            Color dark = new Color(0.42f, 0.28f, 0.16f, 1f);
            Color cloth = new Color(0.45f, 0.22f, 0.16f, 1f);
            Color pack = new Color(0.62f, 0.48f, 0.28f, 1f);
            sheet.Ellipse(132, 36, 52, 28, hide);
            sheet.Ellipse(78, 58, 16, 28, hide);
            sheet.Ellipse(52, 78, 22, 12, dark);
            sheet.Triangle(24, 74, 46, 84, 48, 68, dark);
            sheet.Rect(118, 8, 8, 28, dark);
            sheet.Rect(156, 8, 7, 24, dark);
            sheet.Ellipse(108, 18, 10, 5, dark);
            sheet.Ellipse(168, 16, 9, 4, dark);
            sheet.Ellipse(150, 62, 18, 22, cloth);
            sheet.Rect(146, 78, 8, 16, new Color(0.85f, 0.72f, 0.52f, 1f));
            sheet.Ellipse(168, 48, 16, 12, pack);
            sheet.Ellipse(186, 44, 12, 10, Scale(pack, 0.85f));
        }

        static void DrawHaze(Sheet sheet)
        {
            for (int i = 0; i < 8; i++)
            {
                int x = 8 + (i * 17) % 52;
                int y = 6 + (i * 13) % 48;
                var puff = new Color(0.86f, 0.74f, 0.52f, 0.22f);
                sheet.Ellipse(x, y, 10, 6, puff);
            }
        }

        static Color Scale(Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r * amount),
                Mathf.Clamp01(color.g * amount),
                Mathf.Clamp01(color.b * amount),
                color.a);
        }

        sealed class Sheet
        {
            readonly Color[] pixels;
            public int Width { get; private set; }
            public int Height { get; private set; }

            public Sheet(int width, int height)
            {
                Width = width;
                Height = height;
                pixels = new Color[width * height];
            }

            public void Set(int x, int y, Color color)
            {
                if (x < 0 || y < 0 || x >= Width || y >= Height)
                    return;
                int i = y * Width + x;
                Color under = pixels[i];
                float a = color.a + under.a * (1f - color.a);
                if (a <= 0.001f)
                    return;
                float inv = 1f / a;
                pixels[i] = new Color(
                    (color.r * color.a + under.r * under.a * (1f - color.a)) * inv,
                    (color.g * color.a + under.g * under.a * (1f - color.a)) * inv,
                    (color.b * color.a + under.b * under.a * (1f - color.a)) * inv,
                    a);
            }

            public void Rect(int x, int y, int w, int h, Color color)
            {
                for (int yy = y; yy < y + h; yy++)
                {
                    for (int xx = x; xx < x + w; xx++)
                        Set(xx, yy, color);
                }
            }

            public void Ellipse(int cx, int cy, int rx, int ry, Color color)
            {
                if (rx < 1 || ry < 1)
                    return;
                float invx = 1f / (rx * rx);
                float invy = 1f / (ry * ry);
                for (int y = cy - ry; y <= cy + ry; y++)
                {
                    for (int x = cx - rx; x <= cx + rx; x++)
                    {
                        float dx = x - cx;
                        float dy = y - cy;
                        float n = dx * dx * invx + dy * dy * invy;
                        if (n > 1f)
                            continue;
                        Color paint = color;
                        if (n > 0.75f)
                            paint.a *= (1f - n) / 0.25f;
                        Set(x, y, paint);
                    }
                }
            }

            public void Triangle(int x0, int y0, int x1, int y1, int x2, int y2, Color color)
            {
                int minX = Mathf.Min(x0, Mathf.Min(x1, x2));
                int maxX = Mathf.Max(x0, Mathf.Max(x1, x2));
                int minY = Mathf.Min(y0, Mathf.Min(y1, y2));
                int maxY = Mathf.Max(y0, Mathf.Max(y1, y2));
                float area = Edge(x0, y0, x1, y1, x2, y2);
                if (Mathf.Abs(area) < 0.01f)
                    return;
                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        float w0 = Edge(x1, y1, x2, y2, x, y) / area;
                        float w1 = Edge(x2, y2, x0, y0, x, y) / area;
                        float w2 = Edge(x0, y0, x1, y1, x, y) / area;
                        if (w0 >= 0f && w1 >= 0f && w2 >= 0f)
                            Set(x, y, color);
                    }
                }
            }

            public void FadeBase(float portion)
            {
                int rows = Mathf.Max(1, Mathf.RoundToInt(Height * portion));
                for (int y = 0; y < rows; y++)
                {
                    float t = y / (float)rows;
                    t = t * t;
                    for (int x = 0; x < Width; x++)
                    {
                        Color pixel = pixels[y * Width + x];
                        pixel.a *= t;
                        pixels[y * Width + x] = pixel;
                    }
                }
            }

            public Sprite ToSprite(Vector2 pivot, float pixelsPerUnit, bool repeat)
            {
                var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
                texture.name = "TravelPlaceholder";
                texture.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                texture.hideFlags = HideFlags.HideAndDontSave;
                texture.SetPixels(pixels);
                texture.Apply(false);
                owned.Add(texture);
                var sprite = Sprite.Create(texture, new Rect(0f, 0f, Width, Height), pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);
                sprite.hideFlags = HideFlags.HideAndDontSave;
                owned.Add(sprite);
                return sprite;
            }

            static float Edge(int ax, int ay, int bx, int by, int cx, int cy)
            {
                return (cx - ax) * (by - ay) - (cy - ay) * (bx - ax);
            }
        }
    }
}
