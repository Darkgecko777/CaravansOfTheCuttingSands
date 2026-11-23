using System;
using NUnit.Framework;
using UnityEngine;

namespace Caravans.Play.Tests
{
    public class WeatherLookTests
    {
        [Test]
        public void ConfigLoadsAndRejectsBadValues()
        {
            WeatherLookFile file = GameConfig.WeatherLook;
            Assert.AreEqual(1, file.schemaVersion);
            Assert.AreEqual(0.8f, file.fadeSeconds, 0.0001f);
            Assert.IsTrue(file.showMasses);
            AssertIn(file.fadeSeconds, 0.5f, 1f);
            AssertIn(file.sandstormHazeAlpha, 0f, 0.6f);
            AssertIn(file.heatHazeAlpha, 0f, 0.6f);
            AssertIn(file.windHazeAlpha, 0f, 0.6f);
            AssertIn(file.massSandstormAlpha, 0f, 0.45f);
            AssertIn(file.massHeatAlpha, 0f, 0.45f);
            AssertIn(file.massWindAlpha, 0f, 0.45f);
            AssertIn(file.sandstormGradeStrength, 0f, 1f);
            AssertIn(file.heatGradeStrength, 0f, 1f);
            AssertIn(file.windGradeStrength, 0f, 1f);
            AssertIn(file.noiseStrength, 0f, 1f);
            float largest = Mathf.Max(file.sandstormHazeAlpha, Mathf.Max(file.heatHazeAlpha, file.windHazeAlpha));
            Assert.LessOrEqual(file.hazeCenterRatio * largest, 0.15f);
            AssertHex(file.sandstormGrade);
            AssertHex(file.sandstormHaze);
            AssertHex(file.heatGrade);
            AssertHex(file.heatHaze);
            AssertHex(file.windGrade);
            AssertHex(file.windHaze);
            AssertHex(file.massSandstorm);
            AssertHex(file.massHeat);
            AssertHex(file.massWind);

            WeatherLookFile badHex = Copy(file);
            badHex.sandstormGrade = "not-a-colour";
            StringAssert.Contains("sandstormGrade", Assert.Throws<InvalidOperationException>(() => WeatherLook.Validate(badHex)).Message);

            WeatherLookFile badFade = Copy(file);
            badFade.fadeSeconds = 0.2f;
            StringAssert.Contains("fadeSeconds", Assert.Throws<InvalidOperationException>(() => WeatherLook.Validate(badFade)).Message);

            WeatherLookFile badAlpha = Copy(file);
            badAlpha.sandstormHazeAlpha = 0.9f;
            StringAssert.Contains("sandstormHazeAlpha", Assert.Throws<InvalidOperationException>(() => WeatherLook.Validate(badAlpha)).Message);
        }

        [Test]
        public void TargetAndSnap()
        {
            WeatherKind[] kinds = { WeatherKind.Clear, WeatherKind.Heat, WeatherKind.Wind, WeatherKind.Sandstorm };
            for (int i = 0; i < kinds.Length; i++)
            {
                Assert.AreEqual(WeatherKind.Clear, WeatherLook.Target(false, kinds[i]));
                Assert.AreEqual(kinds[i], WeatherLook.Target(true, kinds[i]));
            }

            WeatherLook.Weights clear = WeatherLook.Snap(WeatherKind.Clear);
            Assert.AreEqual(0f, clear.Heat, 0f);
            Assert.AreEqual(0f, clear.Wind, 0f);
            Assert.AreEqual(0f, clear.Sandstorm, 0f);
        }

        [Test]
        public void ClearIsToday()
        {
            float[] hours = { 0f, 6f, 12f, 18f };
            WeatherLook.Weights clear = WeatherLook.Snap(WeatherKind.Clear);
            for (int i = 0; i < hours.Length; i++)
            {
                Color sun = Color.Lerp(new Color(0.55f, 0.62f, 0.8f), Color.white, TravelScale.Sunlight(hours[i]));
                Color grade = WeatherLook.MapGrade(sun, clear);
                Assert.AreEqual(sun.r, grade.r, 0f);
                Assert.AreEqual(sun.g, grade.g, 0f);
                Assert.AreEqual(sun.b, grade.b, 0f);
                Assert.AreEqual(sun.a, grade.a, 0f);
            }

            Assert.AreEqual(0f, WeatherLook.HazeColour(clear).a, 0f);
        }

        [Test]
        public void SandstormIsWarmAndDusky()
        {
            Color sun = Noon();
            WeatherLook.Weights clear = WeatherLook.Snap(WeatherKind.Clear);
            WeatherLook.Weights storm = WeatherLook.Snap(WeatherKind.Sandstorm);
            Color plain = WeatherLook.MapGrade(sun, clear);
            Color graded = WeatherLook.MapGrade(sun, storm);
            Assert.Less(Luminance(graded), Luminance(plain));
            Assert.Greater(graded.r - graded.b, plain.r - plain.b);
            Assert.AreEqual(0.50f, WeatherLook.HazeColour(storm).a, 0.0001f);
            Assert.AreEqual(0.125f, WeatherLook.HazeCenterAlpha(storm), 0.0001f);
        }

        [Test]
        public void HeatAndWindArePaler()
        {
            Color sun = Noon();
            Color storm = WeatherLook.MapGrade(sun, WeatherLook.Snap(WeatherKind.Sandstorm));
            Color heat = WeatherLook.MapGrade(sun, WeatherLook.Snap(WeatherKind.Heat));
            Color wind = WeatherLook.MapGrade(sun, WeatherLook.Snap(WeatherKind.Wind));
            Assert.Greater(Luminance(heat), Luminance(storm));
            Assert.Greater(Luminance(wind), Luminance(storm));

            Color stormHaze = WeatherLook.HazeColour(WeatherLook.Snap(WeatherKind.Sandstorm));
            Color heatHaze = WeatherLook.HazeColour(WeatherLook.Snap(WeatherKind.Heat));
            Color windHaze = WeatherLook.HazeColour(WeatherLook.Snap(WeatherKind.Wind));
            Assert.Greater(Luminance(heatHaze), Luminance(stormHaze));
            Assert.Greater(Luminance(windHaze), Luminance(stormHaze));
            Assert.Less(heatHaze.a, stormHaze.a);
            Assert.Less(windHaze.a, stormHaze.a);
        }

        [Test]
        public void Fade()
        {
            WeatherLook.Weights weights = WeatherLook.Snap(WeatherKind.Clear);
            float previous = 0f;
            for (int frame = 1; frame <= 48; frame++)
            {
                weights = WeatherLook.Step(weights, WeatherKind.Sandstorm, 1f / 60f, 0.8f);
                Assert.GreaterOrEqual(weights.Sandstorm, previous);
                Assert.LessOrEqual(weights.Sandstorm, 1f);
                if (frame < 48)
                    Assert.Less(weights.Sandstorm, 1f);
                else
                    Assert.AreEqual(1f, weights.Sandstorm, 0.0001f);
                previous = weights.Sandstorm;
            }

            weights = WeatherLook.Snap(WeatherKind.Sandstorm);
            int stormEnd = -1;
            int heatEnd = -1;
            for (int frame = 1; frame <= 60; frame++)
            {
                float beforeStorm = weights.Sandstorm;
                float beforeHeat = weights.Heat;
                weights = WeatherLook.Step(weights, WeatherKind.Heat, 1f / 60f, 0.8f);
                Assert.LessOrEqual(weights.Sandstorm, beforeStorm);
                Assert.GreaterOrEqual(weights.Heat, beforeHeat);
                if (stormEnd < 0 && weights.Sandstorm <= 0.0001f)
                    stormEnd = frame;
                if (heatEnd < 0 && weights.Heat >= 0.9999f)
                    heatEnd = frame;
            }

            Assert.AreEqual(48, stormEnd);
            Assert.AreEqual(stormEnd, heatEnd);

            WeatherLook.Weights snapped = WeatherLook.Step(WeatherLook.Snap(WeatherKind.Sandstorm), WeatherKind.Heat, 1f, 0f);
            Assert.AreEqual(1f, snapped.Heat, 0f);
            Assert.AreEqual(0f, snapped.Wind, 0f);
            Assert.AreEqual(0f, snapped.Sandstorm, 0f);
        }

        [Test]
        public void Contrast()
        {
            Color sun = Noon();
            Color token = new Color(0.93f, 0.82f, 0.55f, 1f);
            Color sand = new Color(0.72f, 0.61f, 0.42f, 1f);
            Color rock = new Color(0.30f, 0.24f, 0.18f, 1f);
            Color route = new Color(0.93f, 0.78f, 0.35f, 1f);
            WeatherLook.Weights clear = WeatherLook.Snap(WeatherKind.Clear);
            float clearSand = TokenContrast(sun, token, sand, clear);
            float clearRock = TokenContrast(sun, token, rock, clear);
            float clearRoute = RouteContrast(sun, route, sand, WeatherKind.Clear, clear);

            WeatherKind[] kinds = { WeatherKind.Clear, WeatherKind.Heat, WeatherKind.Wind, WeatherKind.Sandstorm };
            for (int i = 0; i < kinds.Length; i++)
            {
                WeatherLook.Weights weights = WeatherLook.Snap(kinds[i]);
                float sandRatio = TokenContrast(sun, token, sand, weights);
                float rockRatio = TokenContrast(sun, token, rock, weights);
                float routeRatio = RouteContrast(sun, route, sand, kinds[i], weights);
                Assert.GreaterOrEqual(sandRatio, clearSand * 0.9f, kinds[i] + " token on sand " + sandRatio + " / " + clearSand);
                Assert.GreaterOrEqual(rockRatio, clearRock * 0.9f, kinds[i] + " token on rock " + rockRatio + " / " + clearRock);
                Assert.GreaterOrEqual(routeRatio, clearRoute * 0.9f, kinds[i] + " route " + routeRatio + " / " + clearRoute);
            }
        }

        [Test]
        public void Patches()
        {
            Assert.AreEqual(320f, WeatherLook.MassDiameter(0.16f, 1000f), 0f);

            Texture2D mass = WeatherLook.BuildMassTexture(4765690);
            Texture2D massAgain = WeatherLook.BuildMassTexture(4765690);
            Texture2D haze = WeatherLook.BuildHazeTexture(4765690);
            Texture2D hazeAgain = WeatherLook.BuildHazeTexture(4765690);
            try
            {
                AssertSame(mass, massAgain);
                AssertSame(haze, hazeAgain);
                float radius = mass.width * 0.5f;
                float centre = AlphaAt(mass, mass.width / 2, mass.height / 2);
                Assert.GreaterOrEqual(centre, 0.35f);
                float rim = 0f;
                for (int y = 0; y < mass.height; y++)
                {
                    for (int x = 0; x < mass.width; x++)
                    {
                        float alpha = AlphaAt(mass, x, y);
                        bool border = x == 0 || y == 0 || x == mass.width - 1 || y == mass.height - 1;
                        float dx = (x + 0.5f) - radius;
                        float dy = (y + 0.5f) - radius;
                        float t = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                        if (border || t >= 1f)
                            rim = Mathf.Max(rim, alpha);
                    }
                }

                Assert.LessOrEqual(rim, 0.01f);
                float hazeCentre = AlphaAt(haze, haze.width / 2, haze.height / 2);
                float corner = AlphaAt(haze, 0, 0);
                Assert.Less(hazeCentre, corner);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mass);
                UnityEngine.Object.DestroyImmediate(massAgain);
                UnityEngine.Object.DestroyImmediate(haze);
                UnityEngine.Object.DestroyImmediate(hazeAgain);
            }
        }

        [Test]
        public void NoGameplayChange()
        {
            WeatherFile weather = GameConfig.Weather;
            Assert.AreEqual(1.15f, weather.heatStretch, 0.0001f);
            Assert.AreEqual(1.15f, weather.windStretch, 0.0001f);
            Assert.AreEqual(1.4f, weather.sandstormStretch, 0.0001f);
            Assert.AreEqual(0.17f, weather.visionDay, 0.0001f);
            Assert.AreEqual(0.09f, weather.visionNight, 0.0001f);
            Assert.AreEqual(0.07f, weather.visionSandstorm, 0.0001f);

            var session = new CaravanSession();
            Assert.AreEqual(3, session.Masses.Count);
            ExpectMass(session.Masses[0], WeatherKind.Sandstorm, new Vector2(0.82f, 0.72f), 0.16f, new Vector2(-0.004f, 0.002f));
            ExpectMass(session.Masses[1], WeatherKind.Heat, new Vector2(0.16f, 0.88f), 0.18f, new Vector2(0.006f, -0.002f));
            ExpectMass(session.Masses[2], WeatherKind.Wind, new Vector2(0.72f, 0.08f), 0.14f, new Vector2(-0.004f, 0.003f));

            Vector2 velocity = session.Masses[0].Velocity;
            Assert.IsTrue(session.ShotPlaceWeather(WeatherKind.Sandstorm, RoadGraph.DockPoint(PlaceId.Kharun), 0.10f));
            Assert.AreEqual(3, session.Masses.Count);
            Assert.AreEqual(velocity, session.Masses[0].Velocity);
            Assert.AreEqual(WeatherKind.Sandstorm, session.WeatherAt(session.CaravanPoint));
            Assert.AreEqual(0.07f, session.VisionRadius(), 0.0001f);

            session.ShotResetWeather();
            Assert.AreEqual(3, session.Masses.Count);
            ExpectMass(session.Masses[0], WeatherKind.Sandstorm, new Vector2(0.82f, 0.72f), 0.16f, new Vector2(-0.004f, 0.002f));
            ExpectMass(session.Masses[1], WeatherKind.Heat, new Vector2(0.16f, 0.88f), 0.18f, new Vector2(0.006f, -0.002f));
            ExpectMass(session.Masses[2], WeatherKind.Wind, new Vector2(0.72f, 0.08f), 0.14f, new Vector2(-0.004f, 0.003f));

            Assert.IsFalse(session.ShotPlaceWeather(WeatherKind.Clear, Vector2.zero, 0.1f));
            Assert.AreEqual(3, session.Masses.Count);
        }

        static Color Noon()
        {
            return Color.Lerp(new Color(0.55f, 0.62f, 0.8f), Color.white, TravelScale.Sunlight(12f));
        }

        static float TokenContrast(Color sun, Color token, Color ground, WeatherLook.Weights weights)
        {
            Color haze = WeatherLook.HazeColour(weights);
            haze.a = WeatherLook.HazeCenterAlpha(weights);
            Color tokenOut = Over(token, haze);
            Color groundOut = Over(Multiply(ground, WeatherLook.MapGrade(sun, weights)), haze);
            return ContrastRatio(tokenOut, groundOut);
        }

        static float RouteContrast(Color sun, Color route, Color ground, WeatherKind kind, WeatherLook.Weights weights)
        {
            Color graded = Multiply(ground, WeatherLook.MapGrade(sun, weights));
            Color under = Over(graded, WeatherLook.MassColour(kind));
            return ContrastRatio(route, under);
        }

        static Color Multiply(Color texel, Color grade)
        {
            return new Color(texel.r * grade.r, texel.g * grade.g, texel.b * grade.b, 1f);
        }

        static Color Over(Color under, Color over)
        {
            float alpha = over.a;
            return new Color(
                over.r * alpha + under.r * (1f - alpha),
                over.g * alpha + under.g * (1f - alpha),
                over.b * alpha + under.b * (1f - alpha),
                1f);
        }

        static float ContrastRatio(Color a, Color b)
        {
            float lighter = Mathf.Max(Luminance(a), Luminance(b));
            float darker = Mathf.Min(Luminance(a), Luminance(b));
            return (lighter + 0.05f) / (darker + 0.05f);
        }

        static float Luminance(Color color)
        {
            return 0.2126f * Channel(color.r) + 0.7152f * Channel(color.g) + 0.0722f * Channel(color.b);
        }

        static float Channel(float value)
        {
            if (value <= 0.03928f)
                return value / 12.92f;
            return Mathf.Pow((value + 0.055f) / 1.055f, 2.4f);
        }

        static void ExpectMass(WeatherMass mass, WeatherKind kind, Vector2 center, float radius, Vector2 velocity)
        {
            Assert.AreEqual(kind, mass.Kind);
            Assert.AreEqual(center, mass.Center);
            Assert.AreEqual(radius, mass.Radius, 0.0001f);
            Assert.AreEqual(velocity, mass.Velocity);
        }

        static float AlphaAt(Texture2D texture, int x, int y)
        {
            return texture.GetPixels32()[y * texture.width + x].a / 255f;
        }

        static void AssertSame(Texture2D first, Texture2D second)
        {
            Color32[] a = first.GetPixels32();
            Color32[] b = second.GetPixels32();
            Assert.AreEqual(a.Length, b.Length);
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i].r == b[i].r && a[i].g == b[i].g && a[i].b == b[i].b && a[i].a == b[i].a)
                    continue;
                Assert.Fail("Pixel " + i + " differed.");
            }
        }

        static void AssertIn(float value, float min, float max)
        {
            Assert.GreaterOrEqual(value, min);
            Assert.LessOrEqual(value, max);
        }

        static void AssertHex(string hex)
        {
            Color color;
            Assert.IsTrue(ColorUtility.TryParseHtmlString(hex, out color));
        }

        static WeatherLookFile Copy(WeatherLookFile file)
        {
            return JsonUtility.FromJson<WeatherLookFile>(JsonUtility.ToJson(file));
        }
    }
}
