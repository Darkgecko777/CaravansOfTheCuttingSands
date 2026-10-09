using UnityEngine;

namespace Caravans.FrontDoor
{
    // Smooth value noise in about [-1, 1]. Stands in for the Godot simplex gusts.
    public static class SmoothNoise
    {
        public static float Sample(float x, float y, int seed)
        {
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float tx = x - x0;
            float ty = y - y0;
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);
            float a = Hash(x0, y0, seed);
            float b = Hash(x0 + 1, y0, seed);
            float c = Hash(x0, y0 + 1, seed);
            float d = Hash(x0 + 1, y0 + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }

        public static float Sample1D(float x, int seed)
        {
            return Sample(x, 17.2f, seed);
        }

        static float Hash(int x, int y, int seed)
        {
            int n = x * 374761393 + y * 668265263 + seed * 1442695041;
            n = (n ^ (n >> 13)) * 1274126177;
            n ^= n >> 16;
            return (n & 0x7fffffff) / (float)0x7fffffff * 2f - 1f;
        }
    }
}
