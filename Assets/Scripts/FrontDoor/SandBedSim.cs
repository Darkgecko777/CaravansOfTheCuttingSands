using System;
using UnityEngine;

namespace Caravans.FrontDoor
{
    // Five recycled grain layers. Positions stay in the Godot frame: x right, y down,
    // floor just below a 1920x1080 view. The renderer flips y when it draws.
    public sealed class SandBedSim
    {
        public const float ViewW = 1920f;
        public const float ViewH = 1080f;
        const float FloorY = 1180f;
        const float BedTop = FloorY - 24f;
        const float MacroHoldMin = 2.2f;
        const float MacroHoldMax = 4f;
        const float MacroLerpTime = 1f;
        const float MacroTargetMin = 0.18f;
        const float MacroTargetMax = 0.72f;
        const float MacroDeepCalmChance = 0.10f;
        const float MacroDeepCalmMax = 0.06f;
        const float MicroAmp = 0.12f;
        const float MicroNoiseFreq = 0.11f;
        const float MacroStartRamp = 2.5f;
        const float BaseGravity = 28f;
        const float BaseWind = 980f;
        const float BaseLift = 900f;
        const float BaseUnstick = 360f;
        const float BaseSwirl = 520f;

        public float Gust { get; private set; }
        public float TimeSeconds { get; private set; }
        public int GrainCount { get; private set; }

        readonly Layer[] layers;
        readonly System.Random random;
        int microSeed;
        int shimmerSeed;
        float macroCurrent;
        float macroTarget;
        float macroFrom;
        float macroHoldLeft;
        float macroLerpT;

        public SandBedSim(int seed)
        {
            random = new System.Random(seed);
            microSeed = seed + 3;
            shimmerSeed = 91;
            layers = new[]
            {
                Layer.Make(180, 0.55f, 0.35f, 1.55f, 0.70f, 0.65f, 6.50f, new Color(1f, 0.92f, 0.35f, 1f), 0.18f, 0.75f, 80f, false, 820f, 1180f, seed + 11),
                Layer.Make(170, 0.72f, 0.55f, 1.30f, 0.95f, 0.80f, 5.75f, new Color(1f, 0.93f, 0.38f, 1f), 0.14f, 0.55f, 40f, false, 620f, 1000f, seed + 19),
                Layer.Make(220, 1.00f, 0.70f, 1.05f, 1.25f, 1.00f, 4.75f, new Color(1f, 0.94f, 0.40f, 1f), 0.11f, 0.40f, 0f, true, 400f, 860f, seed + 29),
                Layer.Make(180, 1.10f, 0.90f, 0.90f, 1.55f, 1.05f, 4.25f, new Color(1f, 0.95f, 0.42f, 1f), 0.09f, 0.30f, -30f, false, 260f, 700f, seed + 37),
                Layer.Make(190, 1.20f, 1.10f, 0.70f, 2.10f, 1.10f, 4.00f, new Color(1f, 0.96f, 0.45f, 1f), 0.07f, 0.18f, -50f, false, 100f, 580f, seed + 47),
            };
            int count = 0;
            for (int i = 0; i < layers.Length; i++)
                count += layers[i].Count;
            GrainCount = count;
            ResetMotion();
        }

        public void ResetMotion()
        {
            TimeSeconds = 0f;
            Gust = 0f;
            macroFrom = 0f;
            macroCurrent = 0f;
            macroTarget = PickMacro();
            macroLerpT = 0f;
            macroHoldLeft = Range(MacroHoldMin, MacroHoldMax);
            for (int i = 0; i < layers.Length; i++)
                SeedLayer(layers[i]);
        }

        public float Tick(float delta)
        {
            delta = Mathf.Max(0f, delta);
            TimeSeconds += delta;
            UpdateMacro(delta);
            Gust = GustStrength();
            for (int i = 0; i < layers.Length; i++)
                Simulate(layers[i], delta, Gust);
            return Gust;
        }

        public void Write(Vector3[] vertices, Color32[] colors, Vector3 origin, float halfWidth, float halfHeight, float fade)
        {
            float width = halfWidth * 2f;
            float height = halfHeight * 2f;
            float alphaScale = Mathf.Clamp01(fade);
            int vertex = 0;
            for (int layerIndex = 0; layerIndex < layers.Length; layerIndex++)
            {
                Layer layer = layers[layerIndex];
                float half = layer.QuadSize * 0.5f;
                Color rgb = layer.Modulate;
                rgb.r *= layer.Modulate.r;
                rgb.g *= layer.Modulate.g;
                rgb.b *= layer.Modulate.b;
                for (int i = 0; i < layer.Count; i++)
                {
                    Vector2 p = layer.Pos[i];
                    float x = origin.x + (p.x / ViewW) * width;
                    float y = origin.y + ((ViewH - p.y) / ViewH) * height;
                    float a = alphaScale;
                    if (layer.Shimmer)
                    {
                        float shimmer = SmoothNoise.Sample(p.x * 0.04f * 0.045f, (p.y * 0.03f + TimeSeconds * 0.7f) * 0.045f, shimmerSeed);
                        a *= Mathf.Clamp(0.55f + shimmer * 0.45f, 0.35f, 1f);
                    }

                    var tint = new Color32(
                        (byte)(Mathf.Clamp01(rgb.r) * 255f),
                        (byte)(Mathf.Clamp01(rgb.g) * 255f),
                        (byte)(Mathf.Clamp01(rgb.b) * 255f),
                        (byte)(Mathf.Clamp01(a) * 255f));
                    vertices[vertex] = new Vector3(x - half, y - half, origin.z);
                    vertices[vertex + 1] = new Vector3(x + half, y - half, origin.z);
                    vertices[vertex + 2] = new Vector3(x + half, y + half, origin.z);
                    vertices[vertex + 3] = new Vector3(x - half, y + half, origin.z);
                    colors[vertex] = tint;
                    colors[vertex + 1] = tint;
                    colors[vertex + 2] = tint;
                    colors[vertex + 3] = tint;
                    vertex += 4;
                }
            }
        }

        void SeedLayer(Layer layer)
        {
            int bedCount = (int)(layer.Count * layer.BedFraction);
            for (int i = 0; i < layer.Count; i++)
            {
                if (i < bedCount)
                {
                    layer.Pos[i] = new Vector2(Next01() * ViewW, Range(BedTop, FloorY - 1f));
                    layer.Vel[i] = new Vector2(Range(-6f, 6f), 0f);
                }
                else
                {
                    float y = Mathf.Clamp(Range(120f, 1000f) + layer.HeightBias, 60f, FloorY - 10f);
                    layer.Pos[i] = new Vector2(Next01() * ViewW, y);
                    layer.Vel[i] = new Vector2(Range(40f, 180f) * layer.WindMul, Range(-40f, 20f));
                }
            }
        }

        void Simulate(Layer layer, float delta, float gust)
        {
            float spatialT = TimeSeconds * 0.45f;
            float windMax = BaseWind * layer.WindMul;
            float liftMax = BaseLift * layer.LiftMul;
            float unstick = BaseUnstick * layer.LiftMul;
            float swirlMax = BaseSwirl * layer.SwirlMul;
            float gravity = BaseGravity * layer.GravityMul;
            float layerGust = Mathf.Clamp(gust * layer.GustMul, 0f, 1f);
            for (int i = 0; i < layer.Count; i++)
            {
                Vector2 p = layer.Pos[i];
                Vector2 v = layer.Vel[i];
                float height01 = Mathf.Clamp((FloorY - p.y) / Mathf.Max(FloorY - 700f, 1f), 0f, 1f);
                bool onFloor = p.y >= FloorY - 2.5f;
                v.y += gravity * (1f + height01) * delta;
                float local = Detail(layer, p.x * 0.012f, spatialT);
                local = local * 0.5f + 0.5f;
                float localGust = layerGust * (0.55f + 0.45f * local);
                float bandT = Mathf.Clamp((p.y - 200f) / 900f, 0f, 1f);
                v.x += windMax * localGust * (0.7f + 0.3f * (1f - bandT)) * delta;
                if (onFloor)
                {
                    if (Next01() < localGust * 0.55f * delta * 8f)
                    {
                        v.y -= unstick * (0.6f + Next01() * 0.8f);
                        v.x += Range(40f, 120f) * layer.WindMul;
                    }
                }
                else
                {
                    v.y -= liftMax * localGust * (0.4f + 0.6f * height01) * delta;
                }

                float eddy = Detail(layer, p.x * 0.028f - 17f, p.y * 0.024f + spatialT * 1.2f);
                float turb = Detail(layer, p.x * 0.041f + 9f, p.y * 0.033f - spatialT * 0.9f);
                float swirl = eddy * 0.72f + turb * 0.28f;
                float swirlForce = swirlMax * localGust * (0.65f + 0.45f * bandT);
                v.x += swirl * swirlForce * 0.70f * delta;
                v.y += swirl * swirlForce * 1.25f * delta;
                v.x += turb * swirlForce * 0.35f * delta;
                v.y -= eddy * swirlForce * 0.40f * delta;
                if (p.y < layer.YMin)
                    v.y += 220f * delta;
                else if (p.y > layer.YMax)
                    v.y -= 55f * delta;

                float drag = layer.AirDrag;
                if (onFloor && localGust < 0.1f)
                    drag = 7.5f;
                else if (onFloor)
                    drag = 0.35f;
                v *= Mathf.Max(0f, 1f - drag * delta);
                p += v * delta;
                if (p.y > FloorY)
                {
                    p.y = FloorY;
                    if (v.y > 0f)
                        v.y = 0f;
                    if (localGust < 0.1f)
                        v.x *= 0.88f;
                }

                if (p.y < 40f)
                    v.y += 350f * delta;
                if (p.x > ViewW + 6f)
                {
                    p.x = Range(-6f, 20f);
                    p.y = Mathf.Clamp(p.y + Range(-140f, 140f), layer.YMin, layer.YMax);
                    v.x = Mathf.Max(Mathf.Abs(v.x) * 0.75f, 80f * layer.WindMul);
                    v.y = v.y * 0.7f + Range(-50f, 50f);
                }
                else if (p.x < -50f)
                {
                    p.x = ViewW + Range(-20f, 6f);
                    p.y = Mathf.Clamp(p.y + Range(-140f, 140f), layer.YMin, layer.YMax);
                    v.x = -Mathf.Max(Mathf.Abs(v.x) * 0.75f, 80f * layer.WindMul);
                    v.y = v.y * 0.7f + Range(-50f, 50f);
                }

                if (float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(v.x) || float.IsNaN(v.y))
                {
                    p = new Vector2(Next01() * ViewW, Range(60f, ViewH));
                    v = Vector2.zero;
                }

                layer.Pos[i] = p;
                layer.Vel[i] = v;
            }
        }

        void UpdateMacro(float delta)
        {
            if (macroLerpT < 1f)
            {
                float dur = macroFrom <= 0.001f ? MacroStartRamp : MacroLerpTime;
                macroLerpT = Mathf.Min(1f, macroLerpT + delta / dur);
                float u = macroLerpT;
                u = u * u * (3f - 2f * u);
                macroCurrent = Mathf.Lerp(macroFrom, macroTarget, u);
                return;
            }

            macroHoldLeft -= delta;
            if (macroHoldLeft > 0f)
                return;
            macroFrom = macroCurrent;
            macroTarget = PickMacro();
            macroLerpT = 0f;
            macroHoldLeft = Range(MacroHoldMin, MacroHoldMax);
        }

        float PickMacro()
        {
            if (Next01() < MacroDeepCalmChance)
                return Range(0f, MacroDeepCalmMax);
            float a = Range(MacroTargetMin, MacroTargetMax);
            float b = Range(MacroTargetMin, MacroTargetMax);
            return (a + b) * 0.5f;
        }

        float GustStrength()
        {
            float micro = SmoothNoise.Sample1D(TimeSeconds * 8f * MicroNoiseFreq, microSeed);
            return Mathf.Clamp(macroCurrent * (1f + micro * MicroAmp), 0f, 1f);
        }

        static float Detail(Layer layer, float x, float y)
        {
            return SmoothNoise.Sample(x * 0.28f, y * 0.28f, layer.Seed);
        }

        float Next01()
        {
            return (float)random.NextDouble();
        }

        float Range(float min, float max)
        {
            return min + (max - min) * Next01();
        }

        sealed class Layer
        {
            public int Count;
            public Vector2[] Pos;
            public Vector2[] Vel;
            public int Seed;
            public float WindMul;
            public float LiftMul;
            public float GravityMul;
            public float SwirlMul;
            public float GustMul;
            public float QuadSize;
            public Color Modulate;
            public float AirDrag;
            public float BedFraction;
            public float HeightBias;
            public bool Shimmer;
            public float YMin;
            public float YMax;

            public static Layer Make(
                int count, float wind, float lift, float gravity, float swirl, float gust,
                float size, Color modulate, float drag, float bed, float heightBias, bool shimmer,
                float yMin, float yMax, int seed)
            {
                return new Layer
                {
                    Count = count,
                    Pos = new Vector2[count],
                    Vel = new Vector2[count],
                    Seed = seed,
                    WindMul = wind,
                    LiftMul = lift,
                    GravityMul = gravity,
                    SwirlMul = swirl,
                    GustMul = gust,
                    QuadSize = size,
                    Modulate = modulate,
                    AirDrag = drag,
                    BedFraction = bed,
                    HeightBias = heightBias,
                    Shimmer = shimmer,
                    YMin = yMin,
                    YMax = yMax,
                };
            }
        }
    }
}
