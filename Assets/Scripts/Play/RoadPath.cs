using System.Collections.Generic;
using UnityEngine;

namespace Caravans.Play
{
    // Traced stroke of the painted Kharûn–Draven road on upscale_map.png
    // (3840×2160). y is from the top of the image. The first point is the
    // Kharûn dock and the last is the Draven dock. These samples match
    // RoadGraph. Retrace both if the map art changes.
    public static class RoadPath
    {
        public const float MapWidth = 3840f;
        public const float MapHeight = 2160f;
        public static readonly float Aspect = MapWidth / MapHeight;

        static readonly int[] Pixels =
        {
            1256, 827, 1240, 838, 1256, 854, 1274, 840, 1298, 840, 1304, 844,
            1316, 844, 1338, 858, 1390, 858, 1404, 854, 1408, 846, 1418, 838,
            1472, 838, 1482, 832, 1488, 818, 1494, 756, 1490, 734, 1480, 706,
            1480, 682, 1450, 618, 1440, 574, 1420, 544, 1412, 526, 1414, 502,
            1424, 488, 1424, 480, 1436, 456, 1436, 406, 1442, 394, 1444, 372,
            1432, 354, 1432, 344, 1426, 334, 1426, 326, 1420, 314, 1410, 304,
            1408, 288, 1403, 289
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
