using Caravans.FrontDoor;
using UnityEngine;

namespace Caravans.Play
{
    public static class WeatherLook
    {
        public struct Weights
        {
            public float Heat;
            public float Wind;
            public float Sandstorm;
        }

        struct Palette
        {
            public Color SandstormGrade;
            public Color HeatGrade;
            public Color WindGrade;
            public float SandstormGradeStrength;
            public float HeatGradeStrength;
            public float WindGradeStrength;
            public Color SandstormHaze;
            public Color HeatHaze;
            public Color WindHaze;
            public float SandstormHazeAlpha;
            public float HeatHazeAlpha;
            public float WindHazeAlpha;
            public float HazeCenterRatio;
            public Color MassSandstorm;
            public Color MassHeat;
            public Color MassWind;
            public float MassSandstormAlpha;
            public float MassHeatAlpha;
            public float MassWindAlpha;
            public float MassCore;
            public float NoiseScale;
            public float NoiseStrength;
        }

        static bool ready;
        static Palette palette;

        public static void Validate(WeatherLookFile file)
        {
            if (file == null)
                throw new System.InvalidOperationException("weather_look.json did not parse.");
            if (file.schemaVersion != 1)
                throw new System.InvalidOperationException("weather_look.json schemaVersion must be 1.");

            Range(file.fadeSeconds, 0.5f, 1f, "fadeSeconds");
            Range(file.sandstormHazeAlpha, 0f, 0.6f, "sandstormHazeAlpha");
            Range(file.heatHazeAlpha, 0f, 0.6f, "heatHazeAlpha");
            Range(file.windHazeAlpha, 0f, 0.6f, "windHazeAlpha");
            Range(file.massSandstormAlpha, 0f, 0.45f, "massSandstormAlpha");
            Range(file.massHeatAlpha, 0f, 0.45f, "massHeatAlpha");
            Range(file.massWindAlpha, 0f, 0.45f, "massWindAlpha");
            Range(file.sandstormGradeStrength, 0f, 1f, "sandstormGradeStrength");
            Range(file.heatGradeStrength, 0f, 1f, "heatGradeStrength");
            Range(file.windGradeStrength, 0f, 1f, "windGradeStrength");
            Range(file.noiseStrength, 0f, 1f, "noiseStrength");

            float largestHaze = file.sandstormHazeAlpha;
            if (file.heatHazeAlpha > largestHaze)
                largestHaze = file.heatHazeAlpha;
            if (file.windHazeAlpha > largestHaze)
                largestHaze = file.windHazeAlpha;
            if (file.hazeCenterRatio * largestHaze > 0.15f)
                throw new System.InvalidOperationException("weather_look.json hazeCenterRatio exceeds the centre cap.");

            Hex(file.sandstormGrade, "sandstormGrade");
            Hex(file.sandstormHaze, "sandstormHaze");
            Hex(file.heatGrade, "heatGrade");
            Hex(file.heatHaze, "heatHaze");
            Hex(file.windGrade, "windGrade");
            Hex(file.windHaze, "windHaze");
            Hex(file.massSandstorm, "massSandstorm");
            Hex(file.massHeat, "massHeat");
            Hex(file.massWind, "massWind");
        }

        public static WeatherKind Target(bool onRoad, WeatherKind local)
        {
            return onRoad ? local : WeatherKind.Clear;
        }

        public static Weights Snap(WeatherKind target)
        {
            return new Weights
            {
                Heat = target == WeatherKind.Heat ? 1f : 0f,
                Wind = target == WeatherKind.Wind ? 1f : 0f,
                Sandstorm = target == WeatherKind.Sandstorm ? 1f : 0f
            };
        }

        public static Weights Step(Weights current, WeatherKind target, float dt, float fadeSeconds)
        {
            if (fadeSeconds <= 0f)
                return Snap(target);
            float step = dt / fadeSeconds;
            return new Weights
            {
                Heat = Mathf.MoveTowards(current.Heat, target == WeatherKind.Heat ? 1f : 0f, step),
                Wind = Mathf.MoveTowards(current.Wind, target == WeatherKind.Wind ? 1f : 0f, step),
                Sandstorm = Mathf.MoveTowards(current.Sandstorm, target == WeatherKind.Sandstorm ? 1f : 0f, step)
            };
        }

        public static Color MapGrade(Color sun, Weights weights)
        {
            EnsurePalette();
            Color grade = Color.white;
            grade *= Color.Lerp(Color.white, palette.SandstormGrade, palette.SandstormGradeStrength * weights.Sandstorm);
            grade *= Color.Lerp(Color.white, palette.HeatGrade, palette.HeatGradeStrength * weights.Heat);
            grade *= Color.Lerp(Color.white, palette.WindGrade, palette.WindGradeStrength * weights.Wind);
            return sun * grade;
        }

        public static Color HazeColour(Weights weights)
        {
            EnsurePalette();
            float heat = palette.HeatHazeAlpha * weights.Heat;
            float wind = palette.WindHazeAlpha * weights.Wind;
            float storm = palette.SandstormHazeAlpha * weights.Sandstorm;
            float sum = heat + wind + storm;
            if (sum <= 0f)
                return new Color(1f, 1f, 1f, 0f);
            Color rgb = (palette.HeatHaze * heat + palette.WindHaze * wind + palette.SandstormHaze * storm) / sum;
            rgb.a = sum > 0.6f ? 0.6f : sum;
            return rgb;
        }

        public static float HazeCenterAlpha(Weights weights)
        {
            EnsurePalette();
            return palette.HazeCenterRatio * HazeColour(weights).a;
        }

        public static Color MassColour(WeatherKind kind)
        {
            EnsurePalette();
            switch (kind)
            {
                case WeatherKind.Heat:
                    return WithAlpha(palette.MassHeat, palette.MassHeatAlpha);
                case WeatherKind.Wind:
                    return WithAlpha(palette.MassWind, palette.MassWindAlpha);
                case WeatherKind.Sandstorm:
                    return WithAlpha(palette.MassSandstorm, palette.MassSandstormAlpha);
                default:
                    return new Color(1f, 1f, 1f, 0f);
            }
        }

        public static float MassDiameter(float radius, float sheetHeight)
        {
            return 2f * radius * sheetHeight;
        }

        public static Texture2D BuildHazeTexture(int seed)
        {
            EnsurePalette();
            const int width = 256;
            const int height = 144;
            var texture = NewTexture(width, height);
            var pixels = new Color32[width * height];
            float halfHeight = height * 0.5f;
            float centreX = width * 0.5f;
            float centreY = height * 0.5f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    // d = 1 is the vision circle: distance from the centre in half-heights, x scaled by the aspect.
                    float dx = (px - centreX) / halfHeight;
                    float dy = (py - centreY) / halfHeight;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float v = Mathf.SmoothStep(0.45f, 1.5f, d);
                    float n = Noise(px / width, py / height, seed);
                    float alpha = Mathf.Lerp(palette.HazeCenterRatio, 1f, v) * n;
                    pixels[y * width + x] = White(alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        public static Texture2D BuildMassTexture(int seed)
        {
            EnsurePalette();
            const int size = 128;
            var texture = NewTexture(size, size);
            var pixels = new Color32[size * size];
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) - radius;
                    float dy = (y + 0.5f) - radius;
                    float t = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                    float radial = 1f - Mathf.SmoothStep(palette.MassCore, 1f, t);
                    float n = Noise((x + 0.5f) / size, (y + 0.5f) / size, seed + 1);
                    pixels[y * size + x] = White(radial * n);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        internal static void Forget()
        {
            ready = false;
        }

        static void EnsurePalette()
        {
            if (ready)
                return;
            WeatherLookFile file = GameConfig.WeatherLook;
            palette = new Palette
            {
                SandstormGrade = Parse(file.sandstormGrade),
                HeatGrade = Parse(file.heatGrade),
                WindGrade = Parse(file.windGrade),
                SandstormGradeStrength = file.sandstormGradeStrength,
                HeatGradeStrength = file.heatGradeStrength,
                WindGradeStrength = file.windGradeStrength,
                SandstormHaze = Parse(file.sandstormHaze),
                HeatHaze = Parse(file.heatHaze),
                WindHaze = Parse(file.windHaze),
                SandstormHazeAlpha = file.sandstormHazeAlpha,
                HeatHazeAlpha = file.heatHazeAlpha,
                WindHazeAlpha = file.windHazeAlpha,
                HazeCenterRatio = file.hazeCenterRatio,
                MassSandstorm = Parse(file.massSandstorm),
                MassHeat = Parse(file.massHeat),
                MassWind = Parse(file.massWind),
                MassSandstormAlpha = file.massSandstormAlpha,
                MassHeatAlpha = file.massHeatAlpha,
                MassWindAlpha = file.massWindAlpha,
                MassCore = file.massCore,
                NoiseScale = file.noiseScale,
                NoiseStrength = file.noiseStrength
            };
            ready = true;
        }

        static float Noise(float u, float v, int seed)
        {
            float sum = 0f;
            float amp = 1f;
            float freq = palette.NoiseScale;
            float norm = 0f;
            for (int octave = 0; octave < 3; octave++)
            {
                sum += amp * SmoothNoise.Sample(u * freq, v * freq, seed);
                norm += amp;
                amp *= 0.5f;
                freq *= 2f;
            }

            float fbm = norm <= 0f ? 0f : sum / norm;
            float n = 1f - palette.NoiseStrength * 0.5f * (1f + fbm);
            if (n < 0f)
                return 0f;
            if (n > 1f)
                return 1f;
            return n;
        }

        static Texture2D NewTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }

        static Color32 White(float alpha)
        {
            int a = Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f);
            return new Color32(255, 255, 255, (byte)a);
        }

        static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        static Color Parse(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            color.a = 1f;
            return color;
        }

        static void Range(float value, float min, float max, string field)
        {
            if (value < min || value > max)
                throw new System.InvalidOperationException("weather_look.json " + field + " is outside " + min + " to " + max + ".");
        }

        static void Hex(string hex, string field)
        {
            Color color;
            if (!ColorUtility.TryParseHtmlString(hex, out color))
                throw new System.InvalidOperationException("weather_look.json " + field + " is not a colour.");
        }
    }
}
