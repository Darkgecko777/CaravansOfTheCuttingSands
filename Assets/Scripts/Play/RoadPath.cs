using System.Collections.Generic;
using UnityEngine;

namespace Caravans.Play
{
    // Centers of the painted Kharûn–Draven road on upscale_map.png (3840×2160).
    // y is from the top of the image. The first point is the Kharûn dock,
    // on the road just east of the city. The last is the Draven dock, on the
    // road under the tent. Retrace these if the map art changes.
    public static class RoadPath
    {
        public const float MapWidth = 3840f;
        public const float MapHeight = 2160f;
        public static readonly float Aspect = MapWidth / MapHeight;

        static readonly int[] Pixels =
        {
            1408, 856,
            1412, 848,
            1479, 840,
            1488, 830,
            1494, 804,
            1498, 756,
            1494, 732,
            1485, 708,
            1473, 672,
            1455, 636,
            1431, 564,
            1409, 528,
            1428, 480,
            1440, 420,
            1443, 384
        };

        static readonly Vector2[] Points;
        static readonly float[] Prefix;

        static RoadPath()
        {
            int count = Pixels.Length / 2;
            Points = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                Points[i] = new Vector2(Pixels[i * 2] / MapWidth, Pixels[i * 2 + 1] / MapHeight);
            }

            Prefix = new float[count];
            for (int i = 1; i < count; i++)
                Prefix[i] = Prefix[i - 1] + Vector2.Distance(Points[i - 1], Points[i]);
        }

        public static Vector2 Kharun
        {
            get { return Points[0]; }
        }

        public static Vector2 Draven
        {
            get { return Points[Points.Length - 1]; }
        }

        public static float Length
        {
            get { return Prefix[Prefix.Length - 1]; }
        }

        public static IReadOnlyList<Vector2> Samples
        {
            get { return Points; }
        }

        public static Vector2 PointAtDistance(float distance)
        {
            distance = Mathf.Clamp(distance, 0f, Length);
            for (int i = 1; i < Points.Length; i++)
            {
                if (distance > Prefix[i])
                    continue;
                float span = Prefix[i] - Prefix[i - 1];
                float t = span <= 0.0001f ? 0f : (distance - Prefix[i - 1]) / span;
                return Vector2.Lerp(Points[i - 1], Points[i], t);
            }

            return Draven;
        }

        public static float MapDistance(Vector2 a, Vector2 b)
        {
            float dx = (a.x - b.x) * Aspect;
            float dy = a.y - b.y;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }
    }
}
