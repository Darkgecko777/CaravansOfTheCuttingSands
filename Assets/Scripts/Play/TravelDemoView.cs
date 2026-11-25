using System.Collections.Generic;
using UnityEngine;

namespace Caravans.Play
{
    // Side-on layers. Each layer is filled past the right edge before it can scroll into view.
    public sealed class TravelDemoView : MonoBehaviour
    {
        public const float Horizon = -1.35f;
        public const float CaravanX = -3.55f;

        const float FarParallax = 0.24f;
        const float MidParallax = 0.58f;
        const float NearParallax = 1.18f;
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
        Sprite proceduralSky;
        Transform caravan;
        Transform hazeRoot;
        SpriteRenderer hazeRenderer;
        readonly List<GroundStrip> grounds = new List<GroundStrip>();
        readonly List<GroundStrip> backs = new List<GroundStrip>();
        readonly List<LayerStream> streams = new List<LayerStream>();
        readonly List<PropLayer> propLayers = new List<PropLayer>();
        readonly List<SpriteRenderer> props = new List<SpriteRenderer>();
        readonly List<float> propRouteX = new List<float>();
        int layoutKey = int.MinValue;
        int propKey = int.MinValue;

        class GroundStrip
        {
            public string terrain;
            public Transform root;
            public SpriteRenderer renderer;
            public float tile;
            public float parallax = 1f;
            public float y = Horizon - 2.15f;
            public float bandHeight = 4.4f;
            public float scaleY = 1f;
            public Color lift = Color.white;
        }

        class Piece
        {
            public Transform root;
            public SpriteRenderer renderer;
            public float stripX;
            public float halfWidth;
            public float y;
        }

        class LayerStream
        {
            public string terrain;
            public int layer;
            public float parallax;
            public int order;
            public Color lift;
            public float y;
            public float minGap;
            public float maxGap;
            public float minScale;
            public float maxScale;
            public float widest;
            public float cursor;
            public bool started;
            public System.Random rng;
            public string[] paths;
            public float scaleY;
            public bool tiles;
            public int tileSerial;
            public readonly List<Piece> pieces = new List<Piece>();
        }

        class PropPiece
        {
            public int index;
            public Transform root;
            public SpriteRenderer renderer;
            public float routeX;
            public float halfWidth;
            public float depth;
            public float speed;
            public float y;
            public int order;
        }

        class PropLayer
        {
            public string terrain;
            public string[] paths;
            public float speedMin;
            public float speedMax;
            public float widest;
            public int seed;
            public int nextIndex;
            public bool started;
            public readonly List<PropPiece> pieces = new List<PropPiece>();
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
            proceduralSky = TravelDemoSprites.Sky(skyTexture);
            skyRenderer.sprite = proceduralSky;
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
            ShowSky(chart, mix, zenith, horizon, day);
            if (cam != null)
                cam.backgroundColor = zenith;
            ShowGround(chart, mix, distance, day);
            ShowBack(chart, mix, distance, day);
            ShowBands(chart, mix, distance, day);
            ShowProps(chart, distance, day, zenith);
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

        void ShowSky(TravelDemoChart chart, TravelDemoChart.Mix mix, Color zenith, Color horizon, Color day)
        {
            Sprite art = SkyArt(chart, mix);
            if (art != null)
            {
                if (skyRenderer.sprite != art)
                    skyRenderer.sprite = art;
                skyRenderer.color = day;
                return;
            }

            if (proceduralSky != null && skyRenderer.sprite != proceduralSky)
                skyRenderer.sprite = proceduralSky;
            skyRenderer.color = Color.white;
            PaintSky(zenith, horizon);
        }

        static Sprite SkyArt(TravelDemoChart chart, TravelDemoChart.Mix mix)
        {
            string terrain = mix.bWeight > 0.5f ? mix.b : mix.a;
            TravelKitsFile.Terrain kit = chart.Kits != null ? chart.Kits.Find(terrain) : null;
            if (kit == null)
                return null;
            Sprite sprite = TravelDemoSprites.LoadOrNull(kit.sky);
            if (sprite == null)
                sprite = TravelDemoSprites.LoadOrNull(kit.skyStrip);
            return sprite;
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
            if (!NearTiles(chart, mix.a))
                PlaceGround(chart, mix.a);
            if (mix.b != mix.a && !NearTiles(chart, mix.b))
                PlaceGround(chart, mix.b);
            float viewW = ViewWidth();
            for (int i = 0; i < grounds.Count; i++)
            {
                GroundStrip strip = grounds[i];
                float alpha = Weight(mix, strip.terrain);
                strip.root.gameObject.SetActive(alpha > 0.01f);
                float tile = strip.tile > 0.25f ? strip.tile : 4f;
                float shift = Mathf.Repeat(distance * strip.parallax, tile);
                // The shift walks a full tile. Four extra tiles keep one spare tile past each edge.
                float width = viewW + tile * 4f;
                strip.root.position = new Vector3(-shift, strip.y, 0f);
                strip.renderer.size = new Vector2(width / Mathf.Max(0.01f, strip.root.localScale.x), strip.bandHeight);
                Color lift = strip.lift;
                strip.renderer.color = new Color(lift.r * day.r, lift.g * day.g, lift.b * day.b, alpha);
            }
        }

        void ShowBack(TravelDemoChart chart, TravelDemoChart.Mix mix, float distance, Color day)
        {
            PlaceBack(chart, mix.a);
            if (mix.b != mix.a)
                PlaceBack(chart, mix.b);
            float viewW = ViewWidth();
            for (int i = 0; i < backs.Count; i++)
            {
                GroundStrip strip = backs[i];
                float alpha = Weight(mix, strip.terrain);
                strip.root.gameObject.SetActive(alpha > 0.01f && strip.renderer.sprite != null);
                float tile = strip.tile > 0.25f ? strip.tile : 4f;
                float shift = Mathf.Repeat(distance * strip.parallax, tile);
                float width = viewW + tile * 3f;
                strip.root.position = new Vector3(-shift, strip.y, 0f);
                strip.renderer.size = new Vector2(width, strip.bandHeight);
                Color lift = strip.lift;
                strip.renderer.color = new Color(lift.r * day.r, lift.g * day.g, lift.b * day.b, alpha);
            }
        }

        void PlaceBack(TravelDemoChart chart, string terrain)
        {
            for (int i = 0; i < backs.Count; i++)
            {
                if (backs[i].terrain == terrain)
                    return;
            }

            TravelKitsFile.Terrain kit = chart.Kits != null ? chart.Kits.Find(terrain) : null;
            if (kit == null || string.IsNullOrEmpty(kit.back))
                return;
            Sprite sprite = TravelDemoSprites.LoadOrNull(kit.back);
            if (sprite == null)
                return;

            var host = new GameObject("Back " + terrain);
            host.transform.SetParent(transform, false);
            float scaleY = 0.62f;
            host.transform.localScale = new Vector3(1f, scaleY, 1f);
            var renderer = host.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.sortingOrder = -30;
            float worldH = sprite.bounds.size.y * scaleY;
            var strip = new GroundStrip();
            strip.terrain = terrain;
            strip.root = host.transform;
            strip.renderer = renderer;
            strip.tile = sprite.bounds.size.x;
            strip.parallax = kit.backSpeed > 0.001f ? kit.backSpeed : 0.08f;
            strip.bandHeight = sprite.bounds.size.y;
            strip.scaleY = scaleY;
            strip.y = Horizon - 0.08f + worldH * 0.5f;
            strip.lift = new Color(0.88f, 0.90f, 0.94f, 1f);
            backs.Add(strip);
        }

        static bool NearTiles(TravelDemoChart chart, string terrain)
        {
            TravelKitsFile.Terrain kit = chart.Kits != null ? chart.Kits.Find(terrain) : null;
            return kit != null && kit.nearTiles && kit.near != null && kit.near.Length > 0;
        }

        void PlaceGround(TravelDemoChart chart, string terrain)
        {
            for (int i = 0; i < grounds.Count; i++)
            {
                if (grounds[i].terrain == terrain)
                    return;
            }

            var host = new GameObject("Ground " + terrain);
            host.transform.SetParent(transform, false);
            var renderer = host.AddComponent<SpriteRenderer>();
            TravelKitsFile.Terrain kit = chart.Kits != null ? chart.Kits.Find(terrain) : null;
            Sprite sprite = kit != null ? TravelDemoSprites.LoadOrNull(kit.ground) : null;
            if (sprite == null)
                sprite = TravelDemoSprites.Ground(terrain);
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.sortingOrder = GroundOrder;
            var strip = new GroundStrip();
            strip.terrain = terrain;
            strip.root = host.transform;
            strip.renderer = renderer;
            strip.tile = sprite != null ? sprite.bounds.size.x : 4f;
            grounds.Add(strip);
        }

        void ShowBands(TravelDemoChart chart, TravelDemoChart.Mix mix, float distance, Color day)
        {
            int key = LayoutKey(chart);
            if (layoutKey != key)
            {
                ClearStreams();
                layoutKey = key;
            }

            EnsureTrio(chart, mix.a);
            if (mix.b != mix.a)
                EnsureTrio(chart, mix.b);
            DropStreams(mix);

            float half = ViewWidth() * 0.5f;
            for (int i = 0; i < streams.Count; i++)
            {
                LayerStream stream = streams[i];
                FillStream(stream, distance, half);
                PlaceStream(stream, distance, half, day, Weight(mix, stream.terrain));
            }
        }

        static int LayoutKey(TravelDemoChart chart)
        {
            unchecked
            {
                int hash = chart.Seed == 0 ? 1 : chart.Seed;
                hash = hash * 397 ^ (chart.RouteIndex + 1) * 17;
                hash = hash * 397 ^ (chart.Forward ? 17 : 31);
                return hash;
            }
        }

        void EnsureTrio(TravelDemoChart chart, string terrain)
        {
            TravelKitsFile.Terrain kit = chart.Kits != null ? chart.Kits.Find(terrain) : null;
            float farSpeed = kit != null && kit.farSpeed > 0.001f ? kit.farSpeed : FarParallax;
            float midSpeed = kit != null && kit.midSpeed > 0.001f ? kit.midSpeed : MidParallax;
            float nearSpeed = kit != null && kit.nearSpeed > 0.001f ? kit.nearSpeed : NearParallax;
            EnsureStream(chart, terrain, 0, farSpeed, FarOrder, new Color(0.78f, 0.84f, 0.96f, 1f));
            EnsureStream(chart, terrain, 1, midSpeed, MidOrder, new Color(0.94f, 0.90f, 0.84f, 1f));
            int nearOrder = kit != null && kit.nearTiles ? GroundOrder : NearOrder;
            EnsureStream(chart, terrain, 2, nearSpeed, nearOrder, Color.white);
        }

        void EnsureStream(TravelDemoChart chart, string terrain, int layer, float parallax, int order, Color lift)
        {
            for (int i = 0; i < streams.Count; i++)
            {
                if (streams[i].terrain == terrain && streams[i].layer == layer)
                    return;
            }

            var stream = new LayerStream();
            stream.terrain = terrain;
            stream.layer = layer;
            stream.parallax = parallax;
            stream.order = order;
            stream.lift = lift;
            stream.y = layer == 0 ? Horizon + 1.35f : layer == 1 ? Horizon + 0.05f : Horizon - 1.05f;
            stream.minGap = layer == 0 ? 7.5f : layer == 1 ? 4.2f : 5.5f;
            stream.maxGap = layer == 0 ? 12f : layer == 1 ? 7f : 9f;
            stream.minScale = layer == 0 ? 1.7f : 1.05f;
            stream.maxScale = layer == 0 ? 2.5f : 1.7f;
            TravelKitsFile.Terrain kit = chart.Kits != null ? chart.Kits.Find(terrain) : null;
            if (layer == 0)
                stream.paths = kit != null ? kit.far : null;
            else if (layer == 1)
                stream.paths = kit != null ? kit.mid : null;
            else
                stream.paths = kit != null ? kit.near : null;
            stream.rng = new System.Random(MixSeed(chart.Seed, terrain, layer));
            float native = NativeWidth(stream.paths);
            bool painted = native > 8f;
            if (layer == 2 && kit != null && kit.nearTiles && native > 0.5f)
            {
                stream.tiles = true;
                stream.minScale = 1.72f;
                stream.maxScale = 1.72f;
                stream.scaleY = 4.15f;
                stream.minGap = native * stream.minScale;
                stream.maxGap = stream.minGap;
                stream.y = Horizon;
                stream.lift = Color.white;
                stream.tileSerial = stream.rng.Next(0, Mathf.Max(1, stream.paths.Length));
                stream.widest = native * stream.minScale;
            }
            else if (painted && layer == 0)
            {
                stream.minScale = 0.48f;
                stream.maxScale = 0.70f;
                stream.minGap = native * 0.34f;
                stream.maxGap = native * 0.52f;
                stream.y = Horizon + 0.42f;
                stream.lift = new Color(0.90f, 0.93f, 0.97f, 1f);
                stream.widest = Widest(stream);
            }
            else if (painted && layer == 1)
            {
                stream.minScale = 0.78f;
                stream.maxScale = 1.08f;
                stream.minGap = native * 0.46f;
                stream.maxGap = native * 0.70f;
                stream.y = Horizon + 0.02f;
                stream.lift = Color.white;
                stream.widest = Widest(stream);
            }
            else
            {
                stream.widest = Widest(stream);
            }

            streams.Add(stream);
        }

        static float NativeWidth(string[] paths)
        {
            float width = 0f;
            if (paths == null)
                return width;
            for (int i = 0; i < paths.Length; i++)
            {
                Sprite sprite = TravelDemoSprites.LoadOrNull(paths[i]);
                if (sprite != null)
                    width = Mathf.Max(width, sprite.bounds.size.x);
            }

            return width;
        }

        static float Widest(LayerStream stream)
        {
            float width = 0f;
            if (stream.paths != null)
            {
                for (int i = 0; i < stream.paths.Length; i++)
                {
                    Sprite sprite = TravelDemoSprites.LoadOrNull(stream.paths[i]);
                    if (sprite != null)
                        width = Mathf.Max(width, sprite.bounds.size.x);
                }
            }

            if (width <= 0.01f)
            {
                Sprite sprite = TravelDemoSprites.Feature(stream.terrain, stream.layer, 0);
                if (sprite != null)
                    width = sprite.bounds.size.x;
            }

            if (width <= 0.01f)
                width = 3f;
            return width * stream.maxScale;
        }

        void FillStream(LayerStream stream, float distance, float half)
        {
            float travel = distance * stream.parallax;
            // Ahead of the camera's right edge by one screen plus the widest sprite.
            // Distance already carries the pace, and travel scales it by this layer's parallax.
            float lead = half + half * 2f + stream.widest;
            if (!stream.started)
            {
                stream.cursor = -half + travel - stream.maxGap - stream.widest;
                stream.started = true;
            }

            int guard = 0;
            while (stream.cursor - travel < lead && guard < 64)
            {
                guard++;
                if (stream.tiles)
                {
                    SpawnTile(stream);
                    continue;
                }

                float gap = Mathf.Lerp(stream.minGap, stream.maxGap, (float)stream.rng.NextDouble());
                stream.cursor += gap;
                Spawn(stream);
            }
        }

        void SpawnTile(LayerStream stream)
        {
            int variant = stream.tileSerial++;
            Sprite sprite = SpriteAt(stream.paths, variant, stream.terrain, stream.layer);
            float scaleX = stream.minScale;
            float scaleY = stream.scaleY > 0.01f ? stream.scaleY : scaleX;
            float spriteWidth = sprite != null ? sprite.bounds.size.x : 4f;
            float spriteHeight = sprite != null ? sprite.bounds.size.y : 1f;
            float width = Mathf.Max(0.5f, spriteWidth * scaleX);
            var host = new GameObject("GroundStrip");
            host.transform.SetParent(transform, false);
            host.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            var renderer = host.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = stream.order;
            var piece = new Piece();
            piece.root = host.transform;
            piece.renderer = renderer;
            piece.halfWidth = width * 0.5f;
            piece.stripX = stream.cursor + piece.halfWidth;
            piece.y = -5.9f + spriteHeight * scaleY * 0.5f;
            stream.pieces.Add(piece);
            stream.cursor += width * 0.992f;
        }

        void Spawn(LayerStream stream)
        {
            int variant = stream.rng.Next(0, 6);
            float scale = Mathf.Lerp(stream.minScale, stream.maxScale, (float)stream.rng.NextDouble());
            bool flip = stream.rng.NextDouble() > 0.5f;
            float jitter = ((float)stream.rng.NextDouble() * 2f - 1f) * 0.12f;
            Sprite sprite = SpriteAt(stream.paths, variant, stream.terrain, stream.layer);
            var host = new GameObject("Feature");
            host.transform.SetParent(transform, false);
            float signed = flip ? -scale : scale;
            host.transform.localScale = new Vector3(signed, scale, 1f);
            var renderer = host.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = stream.order;
            var piece = new Piece();
            piece.root = host.transform;
            piece.renderer = renderer;
            piece.stripX = stream.cursor;
            float spriteWidth = sprite != null ? sprite.bounds.size.x : 3f;
            piece.halfWidth = spriteWidth * scale * 0.5f;
            piece.y = stream.y + jitter;
            stream.pieces.Add(piece);
        }

        void PlaceStream(LayerStream stream, float distance, float half, Color day, float alpha)
        {
            float travel = distance * stream.parallax;
            for (int i = stream.pieces.Count - 1; i >= 0; i--)
            {
                Piece piece = stream.pieces[i];
                float worldX = piece.stripX - travel;
                if (worldX + piece.halfWidth < -half)
                {
                    if (piece.root != null)
                        Destroy(piece.root.gameObject);
                    stream.pieces.RemoveAt(i);
                    continue;
                }

                piece.root.position = new Vector3(worldX, piece.y, 0f);
                Color lift = stream.lift;
                piece.renderer.color = new Color(lift.r * day.r, lift.g * day.g, lift.b * day.b, alpha);
            }
        }

        void DropStreams(TravelDemoChart.Mix mix)
        {
            for (int i = streams.Count - 1; i >= 0; i--)
            {
                string terrain = streams[i].terrain;
                if (terrain == mix.a || terrain == mix.b)
                    continue;
                ClearPieces(streams[i]);
                streams.RemoveAt(i);
            }
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

        void ShowProps(TravelDemoChart chart, float distance, Color day, Color sky)
        {
            int key = chart.Seed ^ (chart.RouteIndex * 397) ^ (chart.Forward ? 17 : 0);
            if (propKey != key)
            {
                ClearProps();
                ClearPropLayers();
                string folder = chart.Kits != null ? chart.Kits.propFolder : null;
                IReadOnlyList<TravelDemoChart.PlacedProp> marks = chart.Props;
                for (int i = 0; i < marks.Count; i++)
                {
                    TravelDemoChart.PlacedProp mark = marks[i];
                    if (HasArtProps(chart, mark.terrain))
                        continue;
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

            ShowArtProps(chart, chart.MixAt(length <= 0f ? 0f : Mathf.Repeat(distance / length, 1f)), distance, day, sky);
        }

        static bool HasArtProps(TravelDemoChart chart, string terrain)
        {
            TravelKitsFile.Terrain kit = chart.Kits != null ? chart.Kits.Find(terrain) : null;
            return kit != null && kit.props != null && kit.props.Length > 0;
        }

        void ShowArtProps(TravelDemoChart chart, TravelDemoChart.Mix mix, float distance, Color day, Color sky)
        {
            EnsurePropLayer(chart, mix.a);
            if (mix.b != mix.a)
                EnsurePropLayer(chart, mix.b);
            DropPropLayers(mix);
            float half = ViewWidth() * 0.5f;
            for (int i = 0; i < propLayers.Count; i++)
            {
                PropLayer layer = propLayers[i];
                FillProps(layer, distance, half);
                PlaceProps(layer, distance, half, day, sky, Weight(mix, layer.terrain));
            }
        }

        void EnsurePropLayer(TravelDemoChart chart, string terrain)
        {
            for (int i = 0; i < propLayers.Count; i++)
            {
                if (propLayers[i].terrain == terrain)
                    return;
            }

            TravelKitsFile.Terrain kit = chart.Kits != null ? chart.Kits.Find(terrain) : null;
            if (kit == null || kit.props == null || kit.props.Length == 0)
                return;
            var layer = new PropLayer();
            layer.terrain = terrain;
            layer.paths = kit.props;
            layer.speedMin = kit.propSpeedMin > 0.01f ? kit.propSpeedMin : 0.90f;
            layer.speedMax = kit.propSpeedMax > layer.speedMin ? kit.propSpeedMax : layer.speedMin + 0.15f;
            layer.seed = MixSeed(chart.Seed, terrain, 9);
            layer.widest = NativeWidth(layer.paths) * 0.64f;
            if (layer.widest < 1f)
                layer.widest = 2f;
            propLayers.Add(layer);
        }

        void FillProps(PropLayer layer, float distance, float half)
        {
            const float spacing = 8.6f;
            float lead = half + half * 2f + layer.widest;
            if (!layer.started)
            {
                float startRoute = (-half - layer.widest - spacing) + distance * layer.speedMin;
                layer.nextIndex = Mathf.FloorToInt(startRoute / spacing) - 1;
                layer.started = true;
            }

            int guard = 0;
            while (guard < 64)
            {
                float route = PropRoute(layer, layer.nextIndex);
                if (route - distance * layer.speedMin > lead)
                    break;
                SpawnProp(layer, layer.nextIndex, route);
                layer.nextIndex++;
                guard++;
            }
        }

        static float PropRoute(PropLayer layer, int index)
        {
            float jitter = (Hash01(layer.seed, index * 3) - 0.5f) * 2.8f;
            return index * 8.6f + jitter;
        }

        void SpawnProp(PropLayer layer, int index, float route)
        {
            float depth = Hash01(layer.seed, index * 3 + 1);
            float speed = Mathf.Lerp(layer.speedMin, layer.speedMax, depth);
            float scale = Mathf.Lerp(0.28f, 0.62f, depth);
            bool flip = Hash01(layer.seed, index * 3 + 4) > 0.5f;
            int variant = 0;
            if (layer.paths != null && layer.paths.Length > 0)
                variant = Mathf.FloorToInt(Hash01(layer.seed, index * 3 + 2) * layer.paths.Length) % layer.paths.Length;
            Sprite sprite = SpriteAt(layer.paths, variant, layer.terrain, 3);
            float spriteWidth = sprite != null ? sprite.bounds.size.x : 1.5f;
            var host = new GameObject("Prop");
            host.transform.SetParent(transform, false);
            float signed = flip ? -scale : scale;
            host.transform.localScale = new Vector3(signed, scale, 1f);
            var renderer = host.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            var piece = new PropPiece();
            piece.index = index;
            piece.root = host.transform;
            piece.renderer = renderer;
            piece.routeX = route;
            piece.halfWidth = spriteWidth * scale * 0.5f;
            piece.depth = depth;
            piece.speed = speed;
            piece.y = Mathf.Lerp(Horizon - 0.05f, Horizon - 1.25f, depth);
            piece.order = 22 + Mathf.RoundToInt(depth * 16f);
            renderer.sortingOrder = piece.order;
            layer.pieces.Add(piece);
        }

        void PlaceProps(PropLayer layer, float distance, float half, Color day, Color sky, float alpha)
        {
            for (int i = layer.pieces.Count - 1; i >= 0; i--)
            {
                PropPiece piece = layer.pieces[i];
                float worldX = piece.routeX - distance * piece.speed;
                if (worldX + piece.halfWidth < -half)
                {
                    if (piece.root != null)
                        Destroy(piece.root.gameObject);
                    layer.pieces.RemoveAt(i);
                    continue;
                }

                piece.root.position = new Vector3(worldX, piece.y, 0f);
                piece.renderer.color = PropTint(piece.depth, day, sky, alpha);
                piece.renderer.sortingOrder = piece.order;
            }
        }

        static Color PropTint(float depth, Color day, Color sky, float alpha)
        {
            float haze = (1f - Mathf.Clamp01(depth)) * 0.72f;
            Color tint = Color.Lerp(day, sky, haze);
            return new Color(tint.r, tint.g, tint.b, alpha);
        }

        static float Hash01(int seed, int index)
        {
            unchecked
            {
                int n = seed * 374761393 + index * 668265263;
                n = (n ^ (n >> 13)) * 1274126177;
                n = n ^ (n >> 16);
                return (n & 0xFFFFFF) / (float)0x1000000;
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

        void DropPropLayers(TravelDemoChart.Mix mix)
        {
            for (int i = propLayers.Count - 1; i >= 0; i--)
            {
                string terrain = propLayers[i].terrain;
                if (terrain == mix.a || terrain == mix.b)
                    continue;
                ClearPropPieces(propLayers[i]);
                propLayers.RemoveAt(i);
            }
        }

        void ClearStreams()
        {
            for (int i = 0; i < streams.Count; i++)
                ClearPieces(streams[i]);
            streams.Clear();
            ClearPropLayers();
        }

        void ClearPropLayers()
        {
            for (int i = 0; i < propLayers.Count; i++)
                ClearPropPieces(propLayers[i]);
            propLayers.Clear();
        }

        void ClearPropPieces(PropLayer layer)
        {
            for (int i = 0; i < layer.pieces.Count; i++)
            {
                PropPiece piece = layer.pieces[i];
                if (piece.root != null)
                    Destroy(piece.root.gameObject);
            }

            layer.pieces.Clear();
        }

        void ClearPieces(LayerStream stream)
        {
            for (int i = 0; i < stream.pieces.Count; i++)
            {
                Piece piece = stream.pieces[i];
                if (piece.root != null)
                    Destroy(piece.root.gameObject);
            }

            stream.pieces.Clear();
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
