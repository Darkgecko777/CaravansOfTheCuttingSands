using System.Collections.Generic;
using UnityEngine;

namespace Caravans.Play
{
    // Side-on layers. Each layer is filled past the right edge before it can scroll into view.
    public sealed class TravelDemoView : MonoBehaviour
    {
        public const float Horizon = -1.35f;
        // Top of the near sand. Near strips, the placeholder, and props share this line.
        public const float GroundLine = -1.55f;
        public const float CaravanX = -3.55f;

        const float FarParallax = 0.24f;
        const float MidParallax = 0.58f;
        const float NearParallax = 1.18f;
        // Same scale on both axes. The old pair stretched the strips and left a hard seam.
        const float NearScale = 4.65f;
        // Source pixels of overlap. The right-hand tile fades across this strip.
        const float NearOverlapPx = 56f;
        // Painted crest above the solid sand, in sprite units before the near scale.
        const float NearCrest = 0.19f;
        // Clear padding plus the bottom fade, in sprite units at scale 1.
        const float FeatureFade = 0.36f;
        const float FarContact = -0.90f;
        const float FillTop = 0.35f;
        const float FillBottom = -6.15f;
        const float PropPad = 0.12f;
        const float CaravanFoot = 0.08f;
        // Distant land sits near 40% down a 5.4 view when the camera rests here.
        const float CameraY = -0.48f;
        const float SpawnMargin = 0.8f;
        const float PropMinGap = 7f;
        const float PropGapExtra = 9f;
        const float PropClusterGap = 3.4f;
        const float PropClusterExtra = 1.7f;
        const float DepthApart = 0.30f;
        // A couple of pixels at 100 px per world unit, one bob cycle about twice a second at Steady.
        const float BobHeight = 0.025f;
        const float BobRate = 4.5f;
        const int FarOrder = 0;
        const int MidOrder = 10;
        const int FillOrder = -15;
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
        Transform shadow;
        Transform hazeRoot;
        SpriteRenderer hazeRenderer;
        Transform sandRoot;
        SpriteRenderer sandFill;
        Material unlit;
        Material edge;
        MaterialPropertyBlock edgeBlock;
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
            public bool planted;
            public int tileSerial;
            public int lastTile = -1;
            public bool primed;
            public readonly List<Piece> pieces = new List<Piece>();
            public readonly List<Piece> pool = new List<Piece>();
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
            public float nextEntry;
            public int depthIndex = -1;
            public float lastDepth = -1f;
            public bool started;
            public readonly List<PropPiece> pieces = new List<PropPiece>();
            public readonly List<PropPiece> pool = new List<PropPiece>();
        }

        public void Build(Camera camera)
        {
            cam = camera;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 5.4f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.transform.position = new Vector3(0f, CameraY, -10f);
            }

            skyTexture = TravelDemoSprites.SkyTexture();
            var skyObject = new GameObject("Sky");
            skyObject.transform.SetParent(transform, false);
            sky = skyObject.transform;
            skyRenderer = skyObject.AddComponent<SpriteRenderer>();
            proceduralSky = TravelDemoSprites.Sky(skyTexture);
            skyRenderer.sprite = proceduralSky;
            skyRenderer.sortingOrder = -100;
            UseSprite(skyRenderer);
            FitSky();

            var sandObject = new GameObject("SandFill");
            sandObject.transform.SetParent(transform, false);
            sandRoot = sandObject.transform;
            sandFill = sandObject.AddComponent<SpriteRenderer>();
            sandFill.sprite = TravelDemoSprites.Solid();
            sandFill.sortingOrder = FillOrder;
            UseSprite(sandFill);

            var caravanObject = new GameObject("Caravan");
            caravanObject.transform.SetParent(transform, false);
            caravan = caravanObject.transform;
            var caravanRenderer = caravanObject.AddComponent<SpriteRenderer>();
            caravanRenderer.sprite = TravelDemoSprites.LoadOrNull("Travel/caravan");
            if (caravanRenderer.sprite == null)
                caravanRenderer.sprite = TravelDemoSprites.Caravan();
            caravanRenderer.sortingOrder = CaravanOrder;
            UseSprite(caravanRenderer);
            PlantCaravan();

            var shadowObject = new GameObject("ContactShadow");
            shadowObject.transform.SetParent(transform, false);
            shadow = shadowObject.transform;
            var shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = TravelDemoSprites.Shadow();
            shadowRenderer.sortingOrder = CaravanOrder - 1;
            UseSprite(shadowRenderer);
            shadow.localScale = new Vector3(0.85f, 0.22f, 1f);
            shadow.position = new Vector3(CaravanX, GroundLine, 0f);

            var hazeObject = new GameObject("Haze");
            hazeObject.transform.SetParent(transform, false);
            hazeRoot = hazeObject.transform;
            hazeRenderer = hazeObject.AddComponent<SpriteRenderer>();
            hazeRenderer.sprite = TravelDemoSprites.LoadOrNull("Travel/haze");
            if (hazeRenderer.sprite == null)
                hazeRenderer.sprite = TravelDemoSprites.Haze();
            hazeRenderer.drawMode = SpriteDrawMode.Tiled;
            hazeRenderer.sortingOrder = HazeOrder;
            UseSprite(hazeRenderer);
        }

        void OnDestroy()
        {
            if (unlit != null)
                Destroy(unlit);
            if (edge != null)
                Destroy(edge);
        }

        public void Show(TravelDemoChart chart, float distance, float hour, float scroll)
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
            ShowSand(chart, mix, day);
            ShowGround(chart, mix, distance, day);
            ShowBack(chart, mix, distance, day);
            ShowBands(chart, mix, distance, day);
            ShowProps(chart, distance, day, zenith);
            ShowHaze(distance, day);
            TintCaravan(day, distance, scroll);
            FitSky();
        }

        Material SpriteMaterial()
        {
            if (unlit != null)
                return unlit;
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            unlit = new Material(shader);
            unlit.name = "TravelDemoSprite";
            unlit.hideFlags = HideFlags.HideAndDontSave;
            return unlit;
        }

        void UseSprite(SpriteRenderer renderer)
        {
            if (renderer == null)
                return;
            renderer.sharedMaterial = SpriteMaterial();
        }

        Material EdgeMaterial()
        {
            if (edge != null)
                return edge;
            Shader shader = Shader.Find("Caravans/TravelDemoEdge");
            if (shader == null)
                return SpriteMaterial();
            edge = new Material(shader);
            edge.name = "TravelDemoEdge";
            edge.hideFlags = HideFlags.HideAndDontSave;
            return edge;
        }

        void ApplyEdge(SpriteRenderer renderer, float edgeUv)
        {
            if (edgeBlock == null)
                edgeBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(edgeBlock);
            edgeBlock.SetFloat("_Edge", edgeUv);
            renderer.SetPropertyBlock(edgeBlock);
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
            UseSprite(renderer);
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
            UseSprite(renderer);
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
                stream.minScale = NearScale;
                stream.maxScale = NearScale;
                stream.scaleY = NearScale;
                stream.minGap = native * stream.minScale;
                stream.maxGap = stream.minGap;
                stream.y = GroundLine;
                stream.lift = Color.white;
                stream.tileSerial = stream.rng.Next(0, Mathf.Max(1, stream.paths.Length));
                stream.widest = native * stream.minScale;
            }
            else if (painted && layer == 0)
            {
                stream.planted = true;
                stream.minScale = 0.48f;
                stream.maxScale = 0.70f;
                stream.minGap = native * 0.34f;
                stream.maxGap = native * 0.52f;
                stream.y = FarContact;
                stream.lift = new Color(0.90f, 0.93f, 0.97f, 1f);
                stream.widest = Widest(stream);
            }
            else if (painted && layer == 1)
            {
                stream.planted = true;
                stream.minScale = 0.78f;
                stream.maxScale = 1.08f;
                stream.minGap = native * 0.46f;
                stream.maxGap = native * 0.70f;
                stream.y = GroundLine;
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
            bool backfill = !stream.primed;
            if (!stream.started)
            {
                stream.cursor = -half + travel - stream.maxGap - stream.widest;
                stream.started = true;
            }

            int guard = 0;
            while (guard < 64)
            {
                guard++;
                if (stream.cursor - travel > half + SpawnMargin + stream.widest)
                    break;
                if (stream.tiles)
                {
                    SpawnTile(stream);
                    continue;
                }

                float gap = Mathf.Lerp(stream.minGap, stream.maxGap, (float)stream.rng.NextDouble());
                stream.cursor += gap;
                Spawn(stream, travel, half, backfill);
            }

            stream.primed = true;
        }

        void SpawnTile(LayerStream stream)
        {
            int count = stream.paths != null && stream.paths.Length > 0 ? stream.paths.Length : 1;
            int variant = stream.rng.Next(0, count);
            if (count > 1 && variant == stream.lastTile)
                variant = (variant + 1 + stream.rng.Next(0, count - 1)) % count;
            stream.lastTile = variant;
            stream.tileSerial++;
            Sprite sprite = SpriteAt(stream.paths, variant, stream.terrain, stream.layer);
            float scale = stream.minScale;
            float scaleY = stream.scaleY > 0.01f ? stream.scaleY : scale;
            float spriteWidth = sprite != null ? sprite.bounds.size.x : 4f;
            float spriteHeight = sprite != null ? sprite.bounds.size.y : 1.15f;
            float width = Mathf.Max(0.5f, spriteWidth * scale);
            float ppu = sprite != null && sprite.pixelsPerUnit > 0.01f ? sprite.pixelsPerUnit : 100f;
            float overlap = NearOverlapPx / ppu * scale;
            if (overlap > width * 0.45f)
                overlap = width * 0.45f;
            float edgeUv = sprite != null ? NearOverlapPx / Mathf.Max(1f, sprite.rect.width) : 0.05f;
            Piece piece = TakePiece(stream, "GroundStrip");
            piece.root.localScale = new Vector3(scale, scaleY, 1f);
            piece.renderer.sprite = sprite;
            piece.renderer.sharedMaterial = EdgeMaterial();
            ApplyEdge(piece.renderer, edgeUv);
            piece.renderer.sortingOrder = stream.order;
            piece.halfWidth = width * 0.5f;
            piece.stripX = stream.cursor + piece.halfWidth;
            // Centre pivot. The crest line, not the texture top, is the ground.
            float top = GroundLine + NearCrest * scaleY;
            piece.y = top - spriteHeight * scaleY * 0.5f;
            stream.cursor += width - overlap;
        }

        void Spawn(LayerStream stream, float travel, float half, bool backfill)
        {
            int variant = stream.rng.Next(0, 6);
            float scale = Mathf.Lerp(stream.minScale, stream.maxScale, (float)stream.rng.NextDouble());
            bool flip = stream.rng.NextDouble() > 0.5f;
            float jitter = ((float)stream.rng.NextDouble() * 2f - 1f) * (stream.planted ? 0.04f : 0.12f);
            Sprite sprite = SpriteAt(stream.paths, variant, stream.terrain, stream.layer);
            float spriteWidth = sprite != null ? sprite.bounds.size.x : 3f;
            float halfWidth = spriteWidth * scale * 0.5f;
            // Later spawns stay fully past the right edge. Travel already includes this layer's speed.
            if (!backfill)
            {
                float minCenter = travel + half + SpawnMargin + halfWidth;
                if (stream.cursor < minCenter)
                    stream.cursor = minCenter;
            }

            Piece piece = TakePiece(stream, "Feature");
            float signed = flip ? -scale : scale;
            piece.root.localScale = new Vector3(signed, scale, 1f);
            piece.renderer.sprite = sprite;
            piece.renderer.sortingOrder = stream.order;
            piece.stripX = stream.cursor;
            piece.halfWidth = halfWidth;
            // Bottom pivot. The fade hangs below the contact line into the surface behind it.
            piece.y = stream.planted ? stream.y - FeatureFade * scale + jitter : stream.y + jitter;
        }

        Piece TakePiece(LayerStream stream, string label)
        {
            Piece piece;
            int last = stream.pool.Count - 1;
            if (last >= 0)
            {
                piece = stream.pool[last];
                stream.pool.RemoveAt(last);
                piece.root.gameObject.SetActive(true);
            }
            else
            {
                var host = new GameObject(label);
                host.transform.SetParent(transform, false);
                piece = new Piece();
                piece.root = host.transform;
                piece.renderer = host.AddComponent<SpriteRenderer>();
                UseSprite(piece.renderer);
            }

            stream.pieces.Add(piece);
            return piece;
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
                    ReleasePiece(stream, i);
                    continue;
                }

                piece.root.position = new Vector3(worldX, piece.y, 0f);
                Color lift = stream.lift;
                piece.renderer.color = new Color(lift.r * day.r, lift.g * day.g, lift.b * day.b, alpha);
            }

            if (stream.tiles)
                OrderTiles(stream);
        }

        void OrderTiles(LayerStream stream)
        {
            int count = stream.pieces.Count;
            for (int i = 0; i < count; i++)
            {
                int above = 0;
                float x = stream.pieces[i].stripX;
                for (int j = 0; j < count; j++)
                {
                    if (stream.pieces[j].stripX > x)
                        above++;
                }

                int boost = count - 1 - above;
                if (boost > 3)
                    boost = 3;
                stream.pieces[i].renderer.sortingOrder = stream.order + boost;
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
                    UseSprite(renderer);
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
            // Far props keep mid-dune pace. Near props run slightly ahead of the ground.
            float near = kit.nearSpeed > 0.001f ? kit.nearSpeed : NearParallax;
            layer.speedMin = near * 0.50f;
            layer.speedMax = near * 1.05f;
            layer.seed = MixSeed(chart.Seed, terrain, 9);
            layer.widest = NativeWidth(layer.paths) * 1.2f;
            if (layer.widest < 1f)
                layer.widest = 2f;
            propLayers.Add(layer);
        }

        void FillProps(PropLayer layer, float distance, float half)
        {
            float slow = Mathf.Max(0.25f, layer.speedMin);
            float reach = (half * 2f + SpawnMargin + layer.widest) / slow;
            float lead = (half + SpawnMargin + layer.widest) / slow;
            if (!layer.started)
            {
                layer.nextIndex = 0;
                layer.nextEntry = 0f;
                layer.started = true;
            }

            int guard = 0;
            float window = distance - reach;
            while (layer.nextEntry < window && guard < 96)
            {
                guard++;
                layer.nextIndex++;
                layer.nextEntry += GapBefore(layer, layer.nextIndex);
            }

            while (layer.nextEntry < distance + lead && guard < 96)
            {
                guard++;
                SpawnProp(layer, layer.nextIndex, layer.nextEntry, half);
                layer.nextIndex++;
                layer.nextEntry += GapBefore(layer, layer.nextIndex);
            }
        }

        static float GapBefore(PropLayer layer, int index)
        {
            if (index <= 0)
                return PropMinGap;
            int group = index / 3;
            int place = index % 3;
            if (Hash01(layer.seed, group * 13 + 7) < 0.18f)
            {
                int size = Hash01(layer.seed, group * 13 + 8) < 0.5f ? 2 : 3;
                if (place > 0 && place < size)
                    return PropClusterGap + Hash01(layer.seed, index * 5 + 2) * PropClusterExtra;
            }

            return PropMinGap + Hash01(layer.seed, index * 5) * PropGapExtra;
        }

        float DepthAt(PropLayer layer, int index)
        {
            if (index == layer.depthIndex)
                return layer.lastDepth;
            int from = layer.depthIndex + 1;
            if (index < layer.depthIndex)
            {
                from = 0;
                layer.lastDepth = -1f;
            }

            float depth = layer.lastDepth;
            for (int i = from; i <= index; i++)
            {
                float raw = Hash01(layer.seed, i * 5 + 1);
                if (depth >= 0f && Mathf.Abs(raw - depth) < DepthApart)
                    raw = Mathf.Repeat(depth + 0.42f + Hash01(layer.seed, i * 5 + 4) * 0.22f, 1f);
                depth = raw;
            }

            layer.lastDepth = depth;
            layer.depthIndex = index;
            return depth;
        }

        void SpawnProp(PropLayer layer, int index, float entry, float half)
        {
            float depth = DepthAt(layer, index);
            float speed = Mathf.Lerp(layer.speedMin, layer.speedMax, depth);
            float scale = Mathf.Lerp(0.6f, 1.2f, depth);
            bool flip = Hash01(layer.seed, index * 5 + 3) > 0.5f;
            int variant = 0;
            if (layer.paths != null && layer.paths.Length > 0)
                variant = Mathf.FloorToInt(Hash01(layer.seed, index * 5 + 2) * layer.paths.Length) % layer.paths.Length;
            Sprite sprite = SpriteAt(layer.paths, variant, layer.terrain, 3);
            float spriteWidth = sprite != null ? sprite.bounds.size.x : 1.5f;
            float halfWidth = spriteWidth * scale * 0.5f;
            // Born with its left edge past the right of the frame. Its own speed carries it in.
            float entryX = half + SpawnMargin + halfWidth;
            PropPiece piece = TakeProp(layer);
            float signed = flip ? -scale : scale;
            piece.root.localScale = new Vector3(signed, scale, 1f);
            piece.renderer.sprite = sprite;
            piece.index = index;
            piece.routeX = entryX + entry * speed;
            piece.halfWidth = halfWidth;
            piece.depth = depth;
            piece.speed = speed;
            // Depth 0 is far: higher, smaller, slower. Feet stay on the near sand.
            float feet = Mathf.Lerp(GroundLine - 0.06f, GroundLine - 1.45f, depth);
            piece.y = feet - PropPad * scale;
            piece.order = 24 + Mathf.RoundToInt(depth * 14f);
            piece.renderer.sortingOrder = piece.order;
        }

        PropPiece TakeProp(PropLayer layer)
        {
            PropPiece piece;
            int last = layer.pool.Count - 1;
            if (last >= 0)
            {
                piece = layer.pool[last];
                layer.pool.RemoveAt(last);
                piece.root.gameObject.SetActive(true);
            }
            else
            {
                var host = new GameObject("Prop");
                host.transform.SetParent(transform, false);
                piece = new PropPiece();
                piece.root = host.transform;
                piece.renderer = host.AddComponent<SpriteRenderer>();
                UseSprite(piece.renderer);
            }

            layer.pieces.Add(piece);
            return piece;
        }

        void PlaceProps(PropLayer layer, float distance, float half, Color day, Color sky, float alpha)
        {
            for (int i = layer.pieces.Count - 1; i >= 0; i--)
            {
                PropPiece piece = layer.pieces[i];
                float worldX = piece.routeX - distance * piece.speed;
                if (worldX + piece.halfWidth < -half)
                {
                    ReleaseProp(layer, i);
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
            float hazeY = cam != null ? cam.transform.position.y : 0f;
            hazeRoot.position = new Vector3(-shift, hazeY, 0f);
            hazeRenderer.color = new Color(day.r, day.g * 0.92f, day.b * 0.75f, 0.2f);
        }

        void ShowSand(TravelDemoChart chart, TravelDemoChart.Mix mix, Color day)
        {
            if (sandFill == null)
                return;
            float alpha = 0f;
            if (NearTiles(chart, mix.a))
                alpha += Weight(mix, mix.a);
            if (mix.b != mix.a && NearTiles(chart, mix.b))
                alpha += Weight(mix, mix.b);
            alpha = Mathf.Clamp01(alpha);
            sandRoot.gameObject.SetActive(alpha > 0.01f);
            if (alpha <= 0.01f)
                return;
            float width = ViewWidth() + 4f;
            float height = FillTop - FillBottom;
            sandRoot.localScale = new Vector3(width, height, 1f);
            float x = cam != null ? cam.transform.position.x : 0f;
            sandRoot.position = new Vector3(x, (FillTop + FillBottom) * 0.5f, 0f);
            sandFill.color = new Color(0.86f * day.r, 0.63f * day.g, 0.35f * day.b, alpha);
        }

        void PlantCaravan()
        {
            if (caravan == null)
                return;
            caravan.position = new Vector3(CaravanX, GroundLine - CaravanFoot, 0f);
        }

        void TintCaravan(Color day, float distance, float scroll)
        {
            if (caravan == null)
                return;
            float bob = scroll > 0.01f ? Mathf.Sin(distance * BobRate) * BobHeight : 0f;
            caravan.position = new Vector3(CaravanX, GroundLine - CaravanFoot + bob, 0f);
            if (shadow != null)
                shadow.position = new Vector3(CaravanX, GroundLine, 0f);
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

        void ReleasePiece(LayerStream stream, int index)
        {
            Piece piece = stream.pieces[index];
            stream.pieces.RemoveAt(index);
            if (piece.root == null)
                return;
            piece.root.gameObject.SetActive(false);
            stream.pool.Add(piece);
        }

        void ReleaseProp(PropLayer layer, int index)
        {
            PropPiece piece = layer.pieces[index];
            layer.pieces.RemoveAt(index);
            if (piece.root == null)
                return;
            piece.root.gameObject.SetActive(false);
            layer.pool.Add(piece);
        }

        void ClearPropPieces(PropLayer layer)
        {
            for (int i = 0; i < layer.pieces.Count; i++)
            {
                PropPiece piece = layer.pieces[i];
                if (piece.root != null)
                    Destroy(piece.root.gameObject);
            }

            for (int i = 0; i < layer.pool.Count; i++)
            {
                PropPiece piece = layer.pool[i];
                if (piece.root != null)
                    Destroy(piece.root.gameObject);
            }

            layer.pieces.Clear();
            layer.pool.Clear();
        }

        void ClearPieces(LayerStream stream)
        {
            for (int i = 0; i < stream.pieces.Count; i++)
            {
                Piece piece = stream.pieces[i];
                if (piece.root != null)
                    Destroy(piece.root.gameObject);
            }

            for (int i = 0; i < stream.pool.Count; i++)
            {
                Piece piece = stream.pool[i];
                if (piece.root != null)
                    Destroy(piece.root.gameObject);
            }

            stream.pieces.Clear();
            stream.pool.Clear();
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
