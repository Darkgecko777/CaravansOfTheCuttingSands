using System.Collections.Generic;
using UnityEngine;

namespace Caravans.Play
{
    // Side-on layers. The strip is duplicated so a slower layer can loop without a pop.
    public sealed class TravelDemoView : MonoBehaviour
    {
        public const float Horizon = -1.35f;
        public const float CaravanX = -3.55f;

        const float FarParallax = 0.24f;
        const float MidParallax = 0.58f;
        const float NearParallax = 1.18f;
        const float Period = 40f;
        const int FarOrder = 0;
        const int MidOrder = 10;
        const int GroundOrder = 20;
        const int PropOrder = 30;
        const int CaravanOrder = 40;
        const int NearOrder = 50;
        const int HazeOrder = 70;

        Camera cam;
        Transform sky;
        SpriteRenderer skyRenderer;
        Texture2D skyTexture;
        Transform caravan;
        Transform hazeRoot;
        SpriteRenderer hazeRenderer;
        readonly List<GroundStrip> grounds = new List<GroundStrip>();
        readonly List<Band> bands = new List<Band>();
        readonly List<SpriteRenderer> props = new List<SpriteRenderer>();
        readonly List<float> propRouteX = new List<float>();
        string bandA = "";
        string bandB = "";
        int bandSeed = int.MinValue;
        int propKey = int.MinValue;

        class GroundStrip
        {
            public string terrain;
            public Transform root;
            public SpriteRenderer renderer;
        }

        class Band
        {
            public string terrain;
            public int layer;
            public float parallax;
            public Transform root;
            public readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
            public readonly List<Color> lifts = new List<Color>();
        }

        public void Build(Camera camera)
        {
            cam = camera;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 5.4f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.transform.position = new Vector3(0f, 0f, -10f);
            }

            skyTexture = TravelDemoSprites.SkyTexture();
            var skyObject = new GameObject("Sky");
            skyObject.transform.SetParent(transform, false);
            sky = skyObject.transform;
            skyRenderer = skyObject.AddComponent<SpriteRenderer>();
            skyRenderer.sprite = TravelDemoSprites.Sky(skyTexture);
            skyRenderer.sortingOrder = -100;
            FitSky();

            var caravanObject = new GameObject("Caravan");
            caravanObject.transform.SetParent(transform, false);
            caravan = caravanObject.transform;
            caravan.position = new Vector3(CaravanX, Horizon, 0f);
            var caravanRenderer = caravanObject.AddComponent<SpriteRenderer>();
            caravanRenderer.sprite = TravelDemoSprites.LoadOrNull("Travel/caravan");
            if (caravanRenderer.sprite == null)
                caravanRenderer.sprite = TravelDemoSprites.Caravan();
            caravanRenderer.sortingOrder = CaravanOrder;

            var hazeObject = new GameObject("Haze");
            hazeObject.transform.SetParent(transform, false);
            hazeRoot = hazeObject.transform;
            hazeRenderer = hazeObject.AddComponent<SpriteRenderer>();
            hazeRenderer.sprite = TravelDemoSprites.LoadOrNull("Travel/haze");
            if (hazeRenderer.sprite == null)
                hazeRenderer.sprite = TravelDemoSprites.Haze();
            hazeRenderer.drawMode = SpriteDrawMode.Tiled;
            hazeRenderer.sortingOrder = HazeOrder;
        }

        public void Show(TravelDemoChart chart, float distance, float hour)
        {
            if (chart == null)
                return;
            float length = chart.WorldLength;
            float progress = length <= 0f ? 0f : Mathf.Repeat(distance / length, 1f);
            TravelDemoChart.Mix mix = chart.MixAt(progress);
            Color day = DayTint(hour);
            Color zenith = Zenith(hour);
            Color horizon = Color.Lerp(HorizonColor(mix.a, hour), HorizonColor(mix.b, hour), mix.bWeight);
            PaintSky(zenith, horizon);
            if (cam != null)
                cam.backgroundColor = zenith;
            ShowGround(chart, mix, distance, day);
            ShowBands(chart, mix, distance, day);
            ShowProps(chart, distance, day);
            ShowHaze(distance, day);
            TintCaravan(day);
            FitSky();
        }

        public static Color DayTint(float hour)
        {
            float h = Mathf.Repeat(hour, 24f);
            return Sample(h, DayKeys, DayColors);
        }

        static Color Zenith(float hour)
        {
            float h = Mathf.Repeat(hour, 24f);
            return Sample(h, DayKeys, ZenithColors);
        }

        static Color HorizonColor(string terrain, float hour)
        {
            Color ground = TravelDemoSprites.TerrainColor(terrain);
            Color day = DayTint(hour);
            return new Color(
                ground.r * 0.55f + day.r * 0.45f,
                ground.g * 0.5f + day.g * 0.5f,
                ground.b * 0.45f + day.b * 0.55f,
                1f);
        }

        static readonly float[] DayKeys = { 0f, 3f, 6f, 9f, 12f, 15f, 18f, 21f, 24f };

        static readonly Color[] DayColors =
        {
            new Color(0.32f, 0.36f, 0.55f, 1f),
            new Color(0.20f, 0.22f, 0.36f, 1f),
            new Color(1.00f, 0.72f, 0.55f, 1f),
            new Color(1.00f, 0.92f, 0.78f, 1f),
            new Color(1.00f, 0.97f, 0.88f, 1f),
            new Color(1.00f, 0.84f, 0.60f, 1f),
            new Color(0.86f, 0.52f, 0.46f, 1f),
            new Color(0.46f, 0.40f, 0.60f, 1f),
            new Color(0.32f, 0.36f, 0.55f, 1f),
        };

        static readonly Color[] ZenithColors =
        {
            new Color(0.10f, 0.12f, 0.24f, 1f),
            new Color(0.06f, 0.07f, 0.14f, 1f),
            new Color(0.55f, 0.42f, 0.58f, 1f),
            new Color(0.45f, 0.62f, 0.82f, 1f),
            new Color(0.38f, 0.58f, 0.86f, 1f),
            new Color(0.55f, 0.58f, 0.78f, 1f),
            new Color(0.42f, 0.28f, 0.48f, 1f),
            new Color(0.16f, 0.14f, 0.32f, 1f),
            new Color(0.10f, 0.12f, 0.24f, 1f),
        };

        static Color Sample(float hour, float[] keys, Color[] colors)
        {
            for (int i = 0; i < keys.Length - 1; i++)
            {
                if (hour <= keys[i + 1] || i == keys.Length - 2)
                {
                    float t = Mathf.InverseLerp(keys[i], keys[i + 1], hour);
                    return Color.Lerp(colors[i], colors[i + 1], t);
                }
            }

            return colors[0];
        }

        void PaintSky(Color zenith, Color horizon)
        {
            if (skyTexture == null || cam == null)
                return;
            float view = cam.orthographicSize * 2f;
            float horizonV = Mathf.Clamp01((Horizon - (cam.transform.position.y - cam.orthographicSize)) / view);
            TravelDemoSprites.PaintSky(skyTexture, horizon, zenith, horizonV);
        }

        void FitSky()
        {
            if (skyRenderer == null || skyRenderer.sprite == null || cam == null)
                return;
            float viewH = cam.orthographicSize * 2f;
            float viewW = viewH * Mathf.Max(cam.aspect, 0.5f);
            Vector2 size = skyRenderer.sprite.bounds.size;
            sky.localScale = new Vector3(viewW / Mathf.Max(0.01f, size.x), viewH / Mathf.Max(0.01f, size.y), 1f);
            sky.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
        }

        void ShowGround(TravelDemoChart chart, TravelDemoChart.Mix mix, float distance, Color day)
        {
            float viewW = ViewWidth() + 4f;
            PlaceGround(chart, mix.a, viewW);
            if (mix.b != mix.a)
                PlaceGround(chart, mix.b, viewW);
            float shift = Mathf.Repeat(distance, 4f);
            for (int i = 0; i < grounds.Count; i++)
            {
                GroundStrip strip = grounds[i];
                float alpha = Weight(mix, strip.terrain);
                strip.root.gameObject.SetActive(alpha > 0.01f);
                strip.root.position = new Vector3(-shift, Horizon - 2.15f, 0f);
                strip.renderer.size = new Vector2(viewW, 4.4f);
                strip.renderer.color = new Color(day.r, day.g, day.b, alpha);
            }
        }

        void PlaceGround(TravelDemoChart chart, string terrain, float width)
        {
            for (int i = 0; i < grounds.Count; i++)
            {
                if (grounds[i].terrain == terrain)
                    return;
            }

            var host = new GameObject("Ground " + terrain);
            host.transform.SetParent(transform, false);
            var renderer = host.AddComponent<SpriteRenderer>();
            TravelKitsFile.Terrain kit = chart.Kits.Find(terrain);
            Sprite sprite = kit != null ? TravelDemoSprites.LoadOrNull(kit.ground) : null;
            if (sprite == null)
                sprite = TravelDemoSprites.Ground(terrain);
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(width, 4.4f);
            renderer.sortingOrder = GroundOrder;
            var strip = new GroundStrip();
            strip.terrain = terrain;
            strip.root = host.transform;
            strip.renderer = renderer;
            grounds.Add(strip);
        }

        void ShowBands(TravelDemoChart chart, TravelDemoChart.Mix mix, float distance, Color day)
        {
            if (bandSeed != chart.Seed || bandA != mix.a || bandB != mix.b)
            {
                ClearBands();
                BuildBand(chart, mix.a, 0, FarParallax, FarOrder, new Color(0.78f, 0.84f, 0.96f, 1f));
                BuildBand(chart, mix.a, 1, MidParallax, MidOrder, new Color(0.94f, 0.90f, 0.84f, 1f));
                BuildBand(chart, mix.a, 2, NearParallax, NearOrder, Color.white);
                if (mix.b != mix.a)
                {
                    BuildBand(chart, mix.b, 0, FarParallax, FarOrder, new Color(0.78f, 0.84f, 0.96f, 1f));
                    BuildBand(chart, mix.b, 1, MidParallax, MidOrder, new Color(0.94f, 0.90f, 0.84f, 1f));
                    BuildBand(chart, mix.b, 2, NearParallax, NearOrder, Color.white);
                }

                bandA = mix.a;
                bandB = mix.b;
                bandSeed = chart.Seed;
            }

            for (int i = 0; i < bands.Count; i++)
            {
                Band band = bands[i];
                float alpha = Weight(mix, band.terrain);
                float offset = Mathf.Repeat(distance * band.parallax, Period);
                band.root.localPosition = new Vector3(-offset, 0f, 0f);
                for (int p = 0; p < band.renderers.Count; p++)
                {
                    Color lift = band.lifts[p];
                    band.renderers[p].color = new Color(lift.r * day.r, lift.g * day.g, lift.b * day.b, alpha);
                }
            }
        }

        void BuildBand(TravelDemoChart chart, string terrain, int layer, float parallax, int order, Color lift)
        {
            var root = new GameObject("Band " + terrain + " " + layer);
            root.transform.SetParent(transform, false);
            var band = new Band();
            band.terrain = terrain;
            band.layer = layer;
            band.parallax = parallax;
            band.root = root.transform;
            TravelKitsFile.Terrain kit = chart.Kits.Find(terrain);
            string[] paths = layer == 0 ? kit != null ? kit.far : null : layer == 1 ? kit != null ? kit.mid : null : kit != null ? kit.near : null;
            var rng = new System.Random(MixSeed(chart.Seed, terrain, layer));
            float minGap = layer == 0 ? 7.5f : layer == 1 ? 4.2f : 5.5f;
            float maxGap = layer == 0 ? 12f : layer == 1 ? 7f : 9f;
            float y = layer == 0 ? Horizon + 1.35f : layer == 1 ? Horizon + 0.05f : Horizon - 1.05f;
            float x = 1.5f;
            int guard = 0;
            while (x < Period && guard < 24)
            {
                guard++;
                float gap = Mathf.Lerp(minGap, maxGap, (float)rng.NextDouble());
                x += gap;
                if (x >= Period)
                    break;
                int variant = rng.Next(0, 6);
                Sprite sprite = SpriteAt(paths, variant, terrain, layer);
                float scale = Mathf.Lerp(layer == 0 ? 1.7f : 1.05f, layer == 0 ? 2.5f : 1.7f, (float)rng.NextDouble());
                bool flip = rng.NextDouble() > 0.5;
                float jitter = ((float)rng.NextDouble() * 2f - 1f) * 0.12f;
                PlaceCopies(band, sprite, x, y + jitter, scale, flip, order, lift);
            }

            bands.Add(band);
        }

        static Sprite SpriteAt(string[] paths, int variant, string terrain, int layer)
        {
            if (paths != null && paths.Length > 0)
            {
                Sprite loaded = TravelDemoSprites.LoadOrNull(paths[variant % paths.Length]);
                if (loaded != null)
                    return loaded;
            }

            return TravelDemoSprites.Feature(terrain, layer, variant);
        }

        void PlaceCopies(Band band, Sprite sprite, float x, float y, float scale, bool flip, int order, Color lift)
        {
            PlaceOne(band, sprite, x, y, scale, flip, order, lift);
            PlaceOne(band, sprite, x - Period, y, scale, flip, order, lift);
            PlaceOne(band, sprite, x + Period, y, scale, flip, order, lift);
        }

        void PlaceOne(Band band, Sprite sprite, float x, float y, float scale, bool flip, int order, Color lift)
        {
            var host = new GameObject("Feature");
            host.transform.SetParent(band.root, false);
            host.transform.localPosition = new Vector3(x, y, 0f);
            float signed = flip ? -scale : scale;
            host.transform.localScale = new Vector3(signed, scale, 1f);
            var renderer = host.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            band.renderers.Add(renderer);
            band.lifts.Add(lift);
        }

        void ShowProps(TravelDemoChart chart, float distance, Color day)
        {
            int key = chart.Seed ^ (chart.RouteIndex * 397) ^ (chart.Forward ? 17 : 0);
            if (propKey != key)
            {
                ClearProps();
                string folder = chart.Kits != null ? chart.Kits.propFolder : null;
                IReadOnlyList<TravelDemoChart.PlacedProp> marks = chart.Props;
                for (int i = 0; i < marks.Count; i++)
                {
                    TravelDemoChart.PlacedProp mark = marks[i];
                    Sprite sprite = null;
                    if (!string.IsNullOrEmpty(folder))
                        sprite = TravelDemoSprites.LoadOrNull(folder + "/" + mark.id);
                    if (sprite == null)
                        sprite = TravelDemoSprites.Prop(mark.id);
                    var host = new GameObject(mark.id);
                    host.transform.SetParent(transform, false);
                    var renderer = host.AddComponent<SpriteRenderer>();
                    renderer.sprite = sprite;
                    renderer.sortingOrder = PropOrder;
                    props.Add(renderer);
                    propRouteX.Add(mark.routeX);
                }

                propKey = key;
            }

            float length = Mathf.Max(1f, chart.WorldLength);
            float camX = Mathf.Repeat(distance, length);
            for (int i = 0; i < props.Count; i++)
            {
                float delta = propRouteX[i] - camX;
                if (delta > length * 0.5f)
                    delta -= length;
                if (delta < -length * 0.5f)
                    delta += length;
                props[i].transform.position = new Vector3(delta, Horizon, 0f);
                props[i].color = day;
            }
        }

        void ShowHaze(float distance, Color day)
        {
            if (hazeRenderer == null)
                return;
            float viewW = ViewWidth() + 6f;
            float viewH = cam != null ? cam.orthographicSize * 2f : 10.8f;
            hazeRenderer.size = new Vector2(viewW, viewH);
            float shift = Mathf.Repeat(distance * 0.35f, 3f);
            hazeRoot.position = new Vector3(-shift, 0f, 0f);
            hazeRenderer.color = new Color(day.r, day.g * 0.92f, day.b * 0.75f, 0.2f);
        }

        void TintCaravan(Color day)
        {
            if (caravan == null)
                return;
            var renderer = caravan.GetComponent<SpriteRenderer>();
            if (renderer != null)
                renderer.color = day;
        }

        float ViewWidth()
        {
            if (cam == null)
                return 20f;
            return cam.orthographicSize * 2f * Mathf.Max(cam.aspect, 0.5f);
        }

        static float Weight(TravelDemoChart.Mix mix, string terrain)
        {
            if (mix.a == mix.b)
                return mix.a == terrain ? 1f : 0f;
            float weight = 0f;
            if (mix.a == terrain)
                weight += 1f - mix.bWeight;
            if (mix.b == terrain)
                weight += mix.bWeight;
            return Mathf.Clamp01(weight);
        }

        static int MixSeed(int seed, string terrain, int layer)
        {
            unchecked
            {
                int hash = seed == 0 ? 1 : seed;
                hash = hash * 397 ^ (layer + 1) * 17;
                if (!string.IsNullOrEmpty(terrain))
                {
                    for (int i = 0; i < terrain.Length; i++)
                        hash = hash * 31 + terrain[i];
                }

                if (hash == int.MinValue)
                    hash = 1;
                return hash & int.MaxValue;
            }
        }

        void ClearBands()
        {
            for (int i = 0; i < bands.Count; i++)
            {
                if (bands[i].root != null)
                    Destroy(bands[i].root.gameObject);
            }

            bands.Clear();
        }

        void ClearProps()
        {
            for (int i = 0; i < props.Count; i++)
            {
                if (props[i] != null)
                    Destroy(props[i].gameObject);
            }

            props.Clear();
            propRouteX.Clear();
        }

    }
}
