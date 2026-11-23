using UnityEngine;

namespace Caravans.FrontDoor
{
    // Six wind voices on the one sand gust. Two beds loop. Four gusts take turns
    // and overlap at most two at a time. Each voice keeps its own hold, noise, and pitch.
    public sealed class TitleWind
    {
        public const float FadeSeconds = 2.2f;
        const float StartRamp = 2.5f;
        const float SumCap = 0.62f;
        const float GapSeconds = 1.4f;
        const int MaxGusts = 2;
        const float SpanDb = 26f;

        readonly struct Spec
        {
            public readonly string Resource;
            public readonly bool Bed;
            public readonly float Follow;
            public readonly float MicroAmp;
            public readonly float MicroFreq;
            public readonly float HoldMin;
            public readonly float HoldMax;
            public readonly float LerpSeconds;
            public readonly float Full;
            public readonly float PitchCalm;
            public readonly float PitchFull;
            public readonly float Gate;
            public readonly float CoolMin;
            public readonly float CoolMax;
            public readonly float DeepCalmChance;

            public Spec(
                string resource, bool bed, float follow, float microAmp, float microFreq,
                float holdMin, float holdMax, float lerpSeconds, float full,
                float pitchCalm, float pitchFull, float gate, float coolMin, float coolMax,
                float deepCalmChance)
            {
                Resource = resource;
                Bed = bed;
                Follow = follow;
                MicroAmp = microAmp;
                MicroFreq = microFreq;
                HoldMin = holdMin;
                HoldMax = holdMax;
                LerpSeconds = lerpSeconds;
                Full = full;
                PitchCalm = pitchCalm;
                PitchFull = pitchFull;
                Gate = gate;
                CoolMin = coolMin;
                CoolMax = coolMax;
                DeepCalmChance = deepCalmChance;
            }
        }

        static readonly Spec[] Specs =
        {
            new Spec("wind-bed-low", true, 0.82f, 0.08f, 0.07f, 2.4f, 4.5f, 1.0f, 0.26f, 0.98f, 1.04f, 0f, 0f, 0f, 0.08f),
            new Spec("wind-bed-grain", true, 0.74f, 0.14f, 0.13f, 1.8f, 3.6f, 0.7f, 0.18f, 1.01f, 1.07f, 0f, 0f, 0f, 0.12f),
            new Spec("wind-gust-a", false, 0.62f, 0.18f, 0.09f, 1.2f, 2.6f, 0.45f, 0.32f, 0.94f, 1.06f, 0.34f, 3.5f, 6f, 0.10f),
            new Spec("wind-gust-b", false, 0.55f, 0.22f, 0.17f, 1.4f, 2.8f, 0.9f, 0.28f, 0.97f, 1.08f, 0.48f, 4f, 7f, 0.16f),
            new Spec("wind-gust-c", false, 0.48f, 0.16f, 0.11f, 1.6f, 3.2f, 0.6f, 0.24f, 1.00f, 1.10f, 0.58f, 5f, 8f, 0.14f),
            new Spec("wind-line", false, 0.40f, 0.28f, 0.21f, 2.0f, 5.0f, 1.3f, 0.14f, 1.04f, 1.18f, 0.72f, 9f, 14f, 0.22f),
        };

        readonly System.Random random;
        readonly int noiseSeed;
        readonly float[] macroCurrent;
        readonly float[] own;
        readonly float[] macroHold;
        readonly float[] local;
        readonly float[] gains;
        readonly float[] cooldown;
        readonly bool[] armed;
        readonly bool[] playing;
        readonly int[] playHold;
        AudioSource[] sources;
        AudioClip[] clips;
        bool running;
        bool leaving;
        float leaveFrom = 1f;
        float leaveAmp = 1f;
        float leaveElapsed;
        float choirGap;
        float time;
        int nextGust;

        public TitleWind(int seed)
        {
            random = new System.Random(seed);
            noiseSeed = seed + 17;
            int n = Specs.Length;
            macroCurrent = new float[n];
            own = new float[n];
            macroHold = new float[n];
            local = new float[n];
            gains = new float[n];
            cooldown = new float[n];
            armed = new bool[n];
            playing = new bool[n];
            playHold = new int[n];
        }

        public void Attach(GameObject host)
        {
            if (sources != null || host == null)
                return;
            sources = new AudioSource[Specs.Length];
            clips = new AudioClip[Specs.Length];
            for (int i = 0; i < Specs.Length; i++)
            {
                clips[i] = Resources.Load<AudioClip>("Audio/" + Specs[i].Resource);
                var source = host.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = Specs[i].Bed;
                source.spatialBlend = 0f;
                source.dopplerLevel = 0f;
                source.volume = 0f;
                sources[i] = source;
            }
        }

        public void Detach()
        {
            sources = null;
            clips = null;
            for (int i = 0; i < playing.Length; i++)
                playing[i] = false;
        }

        public void Resume()
        {
            running = true;
            leaving = false;
            leaveFrom = 1f;
            leaveAmp = 1f;
            leaveElapsed = 0f;
            choirGap = 0f;
            time = 0f;
            nextGust = 0;
            for (int i = 0; i < Specs.Length; i++)
            {
                macroCurrent[i] = 0f;
                own[i] = Pick(Specs[i]);
                macroHold[i] = Range(Specs[i].HoldMin, Specs[i].HoldMax);
                local[i] = 0f;
                cooldown[i] = 0f;
                armed[i] = true;
                playing[i] = false;
                playHold[i] = 0;
                if (sources == null || sources[i] == null)
                    continue;
                sources[i].Stop();
                sources[i].volume = 0f;
                sources[i].pitch = Specs[i].PitchCalm;
                if (!Specs[i].Bed || Clip(i) == null)
                    continue;
                sources[i].clip = clips[i];
                sources[i].loop = true;
                sources[i].Play();
            }
        }

        public void BeginFade()
        {
            if (!running || leaving)
                return;
            leaving = true;
            leaveFrom = leaveAmp;
            leaveElapsed = 0f;
        }

        public void Halt()
        {
            running = false;
            leaving = false;
            leaveAmp = 0f;
            if (sources == null)
                return;
            for (int i = 0; i < sources.Length; i++)
            {
                playing[i] = false;
                if (sources[i] == null)
                    continue;
                sources[i].Stop();
                sources[i].volume = 0f;
            }
        }

        public void Tick(float delta, float sharedGust, float sfx)
        {
            if (!running || sources == null)
                return;
            delta = Mathf.Clamp(delta, 0f, 0.1f);
            time += delta;
            if (choirGap > 0f)
                choirGap -= delta;

            if (leaving)
            {
                leaveElapsed += delta;
                float u = Mathf.Clamp01(leaveElapsed / FadeSeconds);
                float shaped = u * u * (3f - 2f * u);
                leaveAmp = Mathf.Lerp(leaveFrom, 0f, shaped);
                if (u >= 1f)
                {
                    Halt();
                    return;
                }
            }

            float shared = Mathf.Clamp01(sharedGust);
            for (int i = 0; i < Specs.Length; i++)
                Advance(i, delta, shared);

            if (!leaving)
                FireGust();

            float level = Mathf.Clamp01(sfx) * leaveAmp;
            float sum = 0f;
            for (int i = 0; i < Specs.Length; i++)
            {
                bool audible = Specs[i].Bed || playing[i];
                float factor = Mathf.Pow(10f, Mathf.Lerp(-SpanDb, 0f, local[i]) / 20f);
                gains[i] = audible ? Specs[i].Full * factor * level : 0f;
            }

            float push = local[0] - local[1];
            gains[0] *= 0.72f + 0.28f * (1f - Mathf.Clamp01(push));
            gains[1] *= 0.72f + 0.28f * (1f - Mathf.Clamp01(-push));
            for (int i = 0; i < gains.Length; i++)
                sum += gains[i];
            float trim = sum > SumCap * level && sum > 0f ? (SumCap * level) / sum : 1f;

            for (int i = 0; i < Specs.Length; i++)
            {
                AudioSource source = sources[i];
                if (source == null)
                    continue;
                source.volume = gains[i] * trim;
                source.pitch = Mathf.Lerp(Specs[i].PitchCalm, Specs[i].PitchFull, local[i]);
                if (Specs[i].Bed || !playing[i])
                    continue;
                if (playHold[i] > 0)
                {
                    playHold[i]--;
                    continue;
                }
                if (source.isPlaying)
                    continue;
                playing[i] = false;
                cooldown[i] = Range(Specs[i].CoolMin, Specs[i].CoolMax);
            }
        }

        void Advance(int i, float delta, float shared)
        {
            Spec spec = Specs[i];
            macroHold[i] -= delta;
            if (macroHold[i] <= 0f)
            {
                own[i] = Pick(spec);
                macroHold[i] = Range(spec.HoldMin, spec.HoldMax);
            }

            float desired = Mathf.Clamp01(shared * spec.Follow + own[i] * (1f - spec.Follow));
            float dur = time < StartRamp ? StartRamp : spec.LerpSeconds;
            macroCurrent[i] = Mathf.MoveTowards(macroCurrent[i], desired, delta / Mathf.Max(0.05f, dur));

            float micro = SmoothNoise.Sample1D(time * 8f * spec.MicroFreq, noiseSeed + i * 13);
            local[i] = Mathf.Clamp(macroCurrent[i] * (1f + micro * spec.MicroAmp), 0f, 1f);
            if (cooldown[i] > 0f)
                cooldown[i] -= delta;
            if (!spec.Bed && local[i] < spec.Gate * 0.72f)
                armed[i] = true;
        }

        void FireGust()
        {
            if (choirGap > 0f)
                return;
            int playingCount = 0;
            for (int i = 2; i < Specs.Length; i++)
            {
                if (playing[i])
                    playingCount++;
            }
            if (playingCount >= MaxGusts)
                return;

            int gusts = Specs.Length - 2;
            for (int k = 0; k < gusts; k++)
            {
                int i = 2 + (nextGust + k) % gusts;
                if (!armed[i] || playing[i] || cooldown[i] > 0f || local[i] < Specs[i].Gate)
                    continue;
                if (Clip(i) == null || sources[i] == null)
                    continue;
                sources[i].clip = clips[i];
                sources[i].loop = false;
                sources[i].Play();
                playing[i] = true;
                playHold[i] = 2;
                armed[i] = false;
                choirGap = GapSeconds;
                nextGust = (nextGust + k + 1) % gusts;
                return;
            }
        }

        AudioClip Clip(int i)
        {
            if (clips == null)
                return null;
            if (clips[i] == null)
                clips[i] = Resources.Load<AudioClip>("Audio/" + Specs[i].Resource);
            return clips[i];
        }

        float Pick(Spec spec)
        {
            if (Next01() < spec.DeepCalmChance)
                return Range(0f, 0.06f);
            float a = Range(0.18f, 0.72f);
            float b = Range(0.18f, 0.72f);
            return (a + b) * 0.5f;
        }

        float Next01()
        {
            return (float)random.NextDouble();
        }

        float Range(float min, float max)
        {
            return min + (max - min) * Next01();
        }
    }
}
