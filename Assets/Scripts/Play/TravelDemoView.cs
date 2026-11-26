using System.Collections.Generic;
using UnityEngine;

namespace Caravans.Play
{
    // Side-on layers. Each layer is filled past the right edge before it can scroll into view.
    public sealed class TravelDemoView : MonoBehaviour
    {
        public const float Horizon = -1.35f;
        // Anchor for the near strips. Their texture top sits above the opaque sand.
        public const float GroundLine = -1.55f;
        // Inked crest of the near sand. The strip's first opaque pixels are higher; this is the line the feet and the props sit under.
        public const float SandTop = GroundLine + 0.20f;
        // Feet sit 12% of the mount height under that crest. PlantCaravan sets this from the sprite.
        public static float WalkLane = SandTop - 0.31f;
        public const float CaravanX = -3.55f;

        const float FarParallax = 0.24f;
        const float MidParallax = 0.58f;
        const float NearParallax = 1.18f;
        // Same scale on both axes. The old pair stretched the strips and left a hard seam.
        const float NearScale = 4.65f;
        // Source pixels of overlap on a butt join. The strips already loop.
        const float NearOverlapPx = 1f;
        // Painted crest above the solid sand, in sprite units before the near scale.
        const float NearCrest = 0.19f;
        // Prop bases sit in the near sand, under the crest, with a small seeded jitter.
        const float PropBandTop = 0.1f;
        const float PropBandBottom = 1.6f;
        const float PropBandJitter = 0.06f;
        // Clear padding plus the bottom fade, in sprite units at scale 1.
        const float FeatureFade = 0.36f;
        const float FarContact = -0.90f;
        const float FillTop = 0.35f;
        const float FillBottom = -6.15f;
        // Distant land sits near 40% down a 5.4 view when the camera rests here.
        const float CameraY = -0.48f;
        const float SpawnMargin = 0.8f;
        const float PropMinGap = 0.6f;
        const float PropGapExtra = 1.9f;
        const float PropClusterGap = 0.15f;
        const float PropClusterExtra = 0.2f;
        // A couple of pixels at 100 px per world unit, one bob cycle about twice a second at Steady.
        const float BobHeight = 0.025f;
        const float BobRate = 4.5f;
        // sky is -100 and the back plain is -30, both below these.
        const int FillOrder = -20;
        const int FarOrder = 0;
        const int MidOrder = 10;
        const int PlainOrder = 12;
        const int PropOrder = 16;
        const int CaravanOrder = 40;
        const int GroundOrder = 50;
        const int NearOrder = 56;
        const int NearPropOrder = 64;
        const int HazeOrder = 80;

        Camera cam;
        Transform sky;
        SpriteRenderer skyRenderer;
        Texture2D skyTexture;
        Sprite proceduralSky;
        Transform caravan;
        float mountY;
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
        readonly List<PropLane> propLanes = new List<PropLane>();
        readonly List<SpriteRenderer> props = new List<SpriteRenderer>();
        readonly List<float> propRouteX = new List<float>();
        int layoutKey = int.MinValue;
        int propKey = int.MinValue;
        float anchoredDistance = float.NaN;
        bool opened;
        bool fillWindow;
        bool refill;

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
            public int tileVariant;
            public float tileWidth;
            public float tileStep;
            public readonly List<Piece> pieces = new List<Piece>();
            public readonly List<Piece> pool = new List<Piece>();
        }

        class PropPiece
        {
            public Transform root;
            public SpriteRenderer renderer;
            public float stripX;
            public float halfWidth;
            public float depth;
            public float y;
            public int order;
        }

        class PropLane
        {
            public string terrain;
            public string[] paths;
            public float speed;
            public float scaleMin;
            public float scaleMax;
            public float depth;
            public int order;
            public float widest;
            public int seed;
            public int nextIndex;
            public float cursor;
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
            caravanRenderer.sprite = TravelDemoSprites.LoadOrNull("Art/Travel/Avatar/avatar_t1_bird_01");
            if (caravanRenderer.sprite == null)
                caravanRenderer.sprite = TravelDemoSprites.LoadOrNull("Travel/caravan");
            if (caravanRenderer.sprite == null)
                caravanRenderer.sprite = TravelDemoSprites.Caravan();
            caravanRenderer.sortingOrder = CaravanOrder;
            UseSprite(caravanRenderer);
            PlantCaravan();

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
            // A discontinuous move, such as the still, fills again. Scroll gaps do not.
            refill = !float.IsNaN(anchoredDistance) && Mathf.Abs(distance - anchoredDistance) > ViewWidth();
            anchoredDistance = distance;
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
            opened = true;
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
            renderer.sortingOrder = PlainOrder;
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
            fillWindow = !opened || refill;
            if (refill)
            {
                for (int i = 0; i < streams.Count; i++)
                    streams[i].started = false;
            }

            if (layoutKey != key)
            {
                ClearStreams();
                layoutKey = key;
                fillWindow = true;
            }

            EnsureTrio(chart, mix.a);
            if (mix.b != mix.a)
                EnsureTrio(chart, mix.b);
            DropStreams(mix);

            float half = ViewWidth() * 0.5f;
            for (int i = 0; i < streams.Count; i++)
            {
                LayerStream stream = streams[i];
                FillStream(stream, distance, half, fillWindow);
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
                CacheTile(stream);
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

        void CacheTile(LayerStream stream)
        {
            int count = stream.paths != null && stream.paths.Length > 0 ? stream.paths.Length : 1;
            stream.tileVariant = stream.rng.Next(0, count);
            Sprite sprite = SpriteAt(stream.paths, stream.tileVariant, stream.terrain, stream.layer);
            float scale = stream.minScale;
            float spriteWidth = sprite != null ? sprite.bounds.size.x : 4f;
            float ppu = sprite != null && sprite.pixelsPerUnit > 0.01f ? sprite.pixelsPerUnit : 100f;
            float overlap = NearOverlapPx / ppu * scale;
            float width = Mathf.Max(0.5f, spriteWidth * scale);
            if (overlap > width * 0.25f)
                overlap = width * 0.25f;
            stream.tileWidth = width;
            stream.tileStep = Mathf.Max(0.25f, width - overlap);
            stream.widest = width;
        }

        static float TileStart(LayerStream stream, float travel, float half, bool placeOnScreen)
        {
            float step = Mathf.Max(0.25f, stream.tileStep);
            float lead = placeOnScreen ? -half - step : half + SpawnMargin;
            float minLeft = travel + lead;
            int n = placeOnScreen
                ? Mathf.FloorToInt(minLeft / step)
                : Mathf.CeilToInt(minLeft / step - 0.0001f);
            return n * step;
        }

        void FillStream(LayerStream stream, float distance, float half, bool placeOnScreen)
        {
            float travel = distance * stream.parallax;
            if (!stream.started)
            {
                stream.started = true;
                if (stream.tiles)
                    stream.cursor = TileStart(stream, travel, half, placeOnScreen);
                else if (placeOnScreen)
                    stream.cursor = travel - half - stream.widest;
                else
                    stream.cursor = travel + half + SpawnMargin;
            }

            if (stream.tiles)
                FillTiles(stream, travel, half);
            else
                FillFeatures(stream, travel, half, placeOnScreen);
        }

        void FillTiles(LayerStream stream, float travel, float half)
        {
            float step = Mathf.Max(0.25f, stream.tileStep);
            float width = stream.tileWidth > 0.01f ? stream.tileWidth : step;
            int skip = 0;
            while (skip < 64 && stream.cursor + width - travel < -half)
            {
                stream.cursor += step;
                skip++;
            }

            int spawn = 0;
            while (spawn < 64 && stream.cursor - travel <= half + SpawnMargin)
            {
                SpawnTile(stream);
                spawn++;
            }
        }

        void FillFeatures(LayerStream stream, float travel, float half, bool placeOnScreen)
        {
            // On-screen backfill only on the first fill. A lagging cursor jumps past the right edge.
            bool cover = placeOnScreen;
            int skipGuard = 0;
            int spawnGuard = 0;
            while (spawnGuard < 64)
            {
                if (stream.cursor - travel > half + SpawnMargin + stream.widest)
                    break;
                float gap = Mathf.Lerp(stream.minGap, stream.maxGap, (float)stream.rng.NextDouble());
                stream.cursor += gap;
                Spawn(stream, travel, half, cover);
                int last = stream.pieces.Count - 1;
                if (last < 0)
                    break;
                Piece piece = stream.pieces[last];
                if (piece.stripX + piece.halfWidth - travel < -half)
                {
                    ReleasePiece(stream, last);
                    skipGuard++;
                    if (skipGuard >= 64)
                        break;
                    continue;
                }

                spawnGuard++;
                if (!cover && piece.stripX - piece.halfWidth - travel >= half + SpawnMargin - 0.02f)
                    break;
            }
        }

        void SpawnTile(LayerStream stream)
        {
            Sprite sprite = SpriteAt(stream.paths, stream.tileVariant, stream.terrain, stream.layer);
            float scale = stream.minScale;
            float scaleY = stream.scaleY > 0.01f ? stream.scaleY : scale;
            float spriteHeight = sprite != null ? sprite.bounds.size.y : 1.15f;
            float width = stream.tileWidth > 0.01f ? stream.tileWidth : Mathf.Max(0.5f, (sprite != null ? sprite.bounds.size.x : 4f) * scale);
            float step = stream.tileStep > 0.01f ? stream.tileStep : width;
            Piece piece = TakePiece(stream, "GroundStrip");
            piece.root.localScale = new Vector3(scale, scaleY, 1f);
            piece.renderer.sprite = sprite;
            // The strip's two ends do not match. A mirrored copy meets each end with itself.
            int tileIndex = Mathf.FloorToInt(stream.cursor / Mathf.Max(0.25f, step));
            piece.renderer.flipX = (tileIndex & 1) != 0;
            piece.renderer.sharedMaterial = SpriteMaterial();
            piece.renderer.SetPropertyBlock(null);
            piece.renderer.sortingOrder = stream.order;
            piece.halfWidth = width * 0.5f;
            piece.stripX = stream.cursor + piece.halfWidth;
            // Centre pivot. The crest line, not the texture top, is the ground.
            float top = GroundLine + NearCrest * scaleY;
            piece.y = top - spriteHeight * scaleY * 0.5f;
            stream.cursor += step;
        }

        void Spawn(LayerStream stream, float travel, float half, bool cover)
        {
            int variant = stream.rng.Next(0, 6);
            float scale = Mathf.Lerp(stream.minScale, stream.maxScale, (float)stream.rng.NextDouble());
            bool flip = stream.rng.NextDouble() > 0.5f;
            float jitter = ((float)stream.rng.NextDouble() * 2f - 1f) * (stream.planted ? 0.04f : 0.12f);
            Sprite sprite = SpriteAt(stream.paths, variant, stream.terrain, stream.layer);
            float spriteWidth = sprite != null ? sprite.bounds.size.x : 3f;
            float halfWidth = spriteWidth * scale * 0.5f;
            // Later spawns stay fully past the right edge. Travel already includes this layer's speed.
            if (!cover)
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
                ClearPropLanes();
                fillWindow = true;
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
            EnsurePropLanes(chart, mix.a);
            if (mix.b != mix.a)
                EnsurePropLanes(chart, mix.b);
            DropPropLanes(mix);
            float half = ViewWidth() * 0.5f;
            for (int i = 0; i < propLanes.Count; i++)
            {
                PropLane lane = propLanes[i];
                if (refill)
                    lane.started = false;
                FillLane(lane, distance, half, fillWindow);
                PlaceProps(lane, distance, half, day, sky, Weight(mix, lane.terrain));
            }
        }

        void EnsurePropLanes(TravelDemoChart chart, string terrain)
        {
            for (int i = 0; i < propLanes.Count; i++)
            {
                if (propLanes[i].terrain == terrain)
                    return;
            }

            TravelKitsFile.Terrain kit = chart.Kits != null ? chart.Kits.Find(terrain) : null;
            if (kit == null || kit.props == null || kit.props.Length == 0)
                return;
            float near = kit.nearSpeed > 0.001f ? kit.nearSpeed : NearParallax;
            float widest = NativeWidth(kit.props);
            if (widest < 0.5f)
                widest = 1.5f;
            AddLane(terrain, kit.props, near, 0.95f, 1.2f, 1f, NearPropOrder, widest * 1.2f, MixSeed(chart.Seed, terrain, 11));
        }

        void AddLane(string terrain, string[] paths, float speed, float scaleMin, float scaleMax, float depth, int order, float widest, int seed)
        {
            var lane = new PropLane();
            lane.terrain = terrain;
            lane.paths = paths;
            lane.speed = speed;
            lane.scaleMin = scaleMin;
            lane.scaleMax = scaleMax;
            lane.depth = depth;
            lane.order = order;
            lane.widest = Mathf.Max(1f, widest);
            lane.seed = seed;
            propLanes.Add(lane);
        }

        void FillLane(PropLane lane, float distance, float half, bool placeOnScreen)
        {
            float travel = distance * lane.speed;
            if (!lane.started)
            {
                lane.started = true;
                lane.nextIndex = 0;
                lane.cursor = placeOnScreen ? travel - half - lane.widest : travel + half + SpawnMargin;
            }

            int skip = 0;
            while (skip < 96)
            {
                float gap = GapAt(lane, lane.nextIndex);
                float width = WidthAt(lane, lane.nextIndex);
                float right = lane.cursor + gap + width;
                if (right - travel >= -half)
                    break;
                lane.cursor = right;
                lane.nextIndex++;
                skip++;
            }

            // On-screen backfill only on the first fill. A lagging cursor jumps past the right edge.
            bool cover = placeOnScreen;
            int spawn = 0;
            while (spawn < 96)
            {
                float gap = GapAt(lane, lane.nextIndex);
                float width = WidthAt(lane, lane.nextIndex);
                float left = lane.cursor + gap;
                float minLeft = travel + half + SpawnMargin;
                if (!cover && left < minLeft)
                    left = minLeft;
                if (left - travel > half + SpawnMargin)
                    break;
                SpawnProp(lane, lane.nextIndex, left, width, travel);
                lane.cursor = left + width;
                lane.nextIndex++;
                spawn++;
                if (!cover)
                    break;
            }
        }

        static float GapAt(PropLane lane, int index)
        {
            if (index <= 0)
                return PropMinGap;
            int group = index / 3;
            int place = index % 3;
            if (Hash01(lane.seed, group * 13 + 7) < 0.18f)
            {
                int size = Hash01(lane.seed, group * 13 + 8) < 0.5f ? 2 : 3;
                if (place > 0 && place < size)
                    return PropClusterGap + Hash01(lane.seed, index * 5 + 6) * PropClusterExtra;
            }

            return PropMinGap + Hash01(lane.seed, index * 5) * PropGapExtra;
        }

        float WidthAt(PropLane lane, int index)
        {
            float scale;
            int variant;
            bool flip;
            PickProp(lane, index, out scale, out variant, out flip);
            Sprite sprite = SpriteAt(lane.paths, variant, lane.terrain, 3);
            float spriteWidth = sprite != null ? sprite.bounds.size.x : 1.5f;
            return Mathf.Max(0.2f, spriteWidth * scale);
        }

        static float PropDrop(PropLane lane, int index)
        {
            float along = Hash01(lane.seed, index * 5 + 1);
            float jitter = (Hash01(lane.seed, index * 5 + 4) * 2f - 1f) * PropBandJitter;
            float drop = Mathf.Lerp(PropBandTop, PropBandBottom, along) + jitter;
            return Mathf.Clamp(drop, PropBandTop, PropBandBottom + PropBandJitter);
        }

        static void PickProp(PropLane lane, int index, out float scale, out int variant, out bool flip)
        {
            float depth = Mathf.InverseLerp(PropBandTop, PropBandBottom, PropDrop(lane, index));
            scale = Mathf.Lerp(lane.scaleMin, lane.scaleMax, depth);
            flip = Hash01(lane.seed, index * 5 + 3) > 0.5f;
            variant = 0;
            if (lane.paths != null && lane.paths.Length > 0)
                variant = Mathf.FloorToInt(Hash01(lane.seed, index * 5 + 2) * lane.paths.Length) % lane.paths.Length;
        }

        void SpawnProp(PropLane lane, int index, float left, float width, float travel)
        {
            float scale;
            int variant;
            bool flip;
            PickProp(lane, index, out scale, out variant, out flip);
            Sprite sprite = SpriteAt(lane.paths, variant, lane.terrain, 3);
            PropPiece piece = TakeProp(lane);
            float signed = flip ? -scale : scale;
            piece.root.localScale = new Vector3(signed, scale, 1f);
            piece.renderer.sprite = sprite;
            piece.stripX = left + width * 0.5f;
            piece.halfWidth = width * 0.5f;
            piece.depth = lane.depth;
            float sink = sprite != null ? sprite.bounds.min.y : 0f;
            float feet = SandTop - PropDrop(lane, index);
            piece.y = feet - sink * scale;
            // A ledge in front of the mount reads as the ground it stands on. Draw that piece behind the mount. The near sand stays in front and still hides the feet.
            float spriteHeight = sprite != null ? sprite.bounds.size.y * scale : 0f;
            bool shelvesMount = feet + spriteHeight > SandTop && CoversMount(left, width, travel);
            piece.order = shelvesMount ? CaravanOrder - 2 : lane.order;
            piece.renderer.sortingOrder = piece.order;
        }

        bool CoversMount(float left, float width, float travel)
        {
            float propLeft = left - travel;
            float propRight = propLeft + width;
            float mountHalf = 1.2f;
            if (caravan != null)
            {
                var renderer = caravan.GetComponent<SpriteRenderer>();
                if (renderer != null && renderer.sprite != null)
                    mountHalf = renderer.sprite.bounds.extents.x;
            }

            return propRight > CaravanX - mountHalf && propLeft < CaravanX + mountHalf;
        }

        PropPiece TakeProp(PropLane lane)
        {
            PropPiece piece;
            int last = lane.pool.Count - 1;
            if (last >= 0)
            {
                piece = lane.pool[last];
                lane.pool.RemoveAt(last);
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

            lane.pieces.Add(piece);
            return piece;
        }

        void PlaceProps(PropLane lane, float distance, float half, Color day, Color sky, float alpha)
        {
            float travel = distance * lane.speed;
            for (int i = lane.pieces.Count - 1; i >= 0; i--)
            {
                PropPiece piece = lane.pieces[i];
                float worldX = piece.stripX - travel;
                if (worldX + piece.halfWidth < -half)
                {
                    ReleaseProp(lane, i);
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
            var renderer = caravan.GetComponent<SpriteRenderer>();
            Sprite sprite = renderer != null ? renderer.sprite : null;
            float height = sprite != null ? sprite.bounds.size.y : 2.58f;
            WalkLane = SandTop - 0.12f * height;
            mountY = WalkLane;
            caravan.position = new Vector3(CaravanX, mountY, 0f);
        }

        void TintCaravan(Color day, float distance, float scroll)
        {
            if (caravan == null)
                return;
            float bob = scroll > 0.01f ? Mathf.Sin(distance * BobRate) * BobHeight : 0f;
            caravan.position = new Vector3(CaravanX, mountY + bob, 0f);
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

        void DropPropLanes(TravelDemoChart.Mix mix)
        {
            for (int i = propLanes.Count - 1; i >= 0; i--)
            {
                string terrain = propLanes[i].terrain;
                if (terrain == mix.a || terrain == mix.b)
                    continue;
                ClearPropPieces(propLanes[i]);
                propLanes.RemoveAt(i);
            }
        }

        void ClearStreams()
        {
            for (int i = 0; i < streams.Count; i++)
                ClearPieces(streams[i]);
            streams.Clear();
            ClearPropLanes();
        }

        void ClearPropLanes()
        {
            for (int i = 0; i < propLanes.Count; i++)
                ClearPropPieces(propLanes[i]);
            propLanes.Clear();
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

        void ReleaseProp(PropLane lane, int index)
        {
            PropPiece piece = lane.pieces[index];
            lane.pieces.RemoveAt(index);
            if (piece.root == null)
                return;
            piece.root.gameObject.SetActive(false);
            lane.pool.Add(piece);
        }

        void ClearPropPieces(PropLane lane)
        {
            for (int i = 0; i < lane.pieces.Count; i++)
            {
                PropPiece piece = lane.pieces[i];
                if (piece.root != null)
                    Destroy(piece.root.gameObject);
            }

            for (int i = 0; i < lane.pool.Count; i++)
            {
                PropPiece piece = lane.pool[i];
                if (piece.root != null)
                    Destroy(piece.root.gameObject);
            }

            lane.pieces.Clear();
            lane.pool.Clear();
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
