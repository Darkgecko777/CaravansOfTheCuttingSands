#if UNITY_EDITOR
using UnityEngine;

namespace Caravans.FrontDoor
{
    // Fixed 1/60 s clock for the trailer capture. The play-mode domain reload
    // wipes statics, so Begin publishes the seed in an environment variable and
    // SubsystemRegistration reads it back before the title sand is seeded.
    public static class TrailerClock
    {
        const string Variable = "CARAVANS_TRAILER_CLOCK";

        public static bool Active;
        public static float Step = 1f / 60f;
        public static int StormSeed = 4765690;
        public static float Now;
        public static int Frames;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Restore()
        {
            Active = false;
            Now = 0f;
            Frames = 0;
            Step = 1f / 60f;
            StormSeed = 4765690;
            string value = System.Environment.GetEnvironmentVariable(Variable);
            int seed;
            if (!string.IsNullOrEmpty(value) && int.TryParse(value, out seed))
            {
                Active = true;
                StormSeed = seed;
            }
        }

        public static void Begin(int stormSeed)
        {
            StormSeed = stormSeed;
            Now = 0f;
            Frames = 0;
            Step = 1f / 60f;
            Active = true;
            System.Environment.SetEnvironmentVariable(Variable, stormSeed.ToString());
        }

        public static void End()
        {
            Active = false;
            Now = 0f;
            Frames = 0;
            System.Environment.SetEnvironmentVariable(Variable, null);
        }

        public static void ResetFrames()
        {
            Now = 0f;
            Frames = 0;
        }

        public static void Advance()
        {
            if (!Active)
                return;
            Now += Step;
            Frames++;
        }

        public static float Delta(float unscaled)
        {
            return Active ? Step : unscaled;
        }

        public static float Time(float unscaled)
        {
            return Active ? Now : unscaled;
        }
    }
}
#endif
