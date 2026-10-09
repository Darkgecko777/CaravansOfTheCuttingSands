using System.Collections.Generic;
using UnityEngine;

namespace Caravans.Play
{
    // Painted roads on upscale_map.png. y is from the top. Length is the same
    // uncorrected normalized space as RoadPath. The Kharûn–Draven points are
    // that file's samples. A dotted gap is a straight chord and adds no extra
    // time. A sea opening has no line and no hours, and it still counts as one hop.
    public static class RoadGraph
    {
        struct Edge
        {
            public PlaceId A;
            public PlaceId B;
            public bool Sea;
            public float Length;
            public Vector2[] Points;
            public float[] Prefix;
        }

        static readonly List<Edge> Edges = new List<Edge>();
        static readonly List<PlaceId>[] NeighborsOf = new List<PlaceId>[WorldIds.PlaceCount];
        static readonly Vector2[] Docks = new Vector2[WorldIds.PlaceCount];

        static RoadGraph()
        {
            for (int i = 0; i < NeighborsOf.Length; i++)
                NeighborsOf[i] = new List<PlaceId>();

            Dock(PlaceId.Kharun, 1408, 856);
            Dock(PlaceId.Draven, 1443, 384);
            Dock(PlaceId.Zamath, 798, 1566);
            Dock(PlaceId.Thalor, 2926, 262);
            Dock(PlaceId.Veythar, 3405, 1955);
            Dock(PlaceId.Ghorath, 2795, 1691);
            Dock(PlaceId.Ashar, 3649, 904);
            Dock(PlaceId.Moraq, 2502, 1211);
            Dock(PlaceId.Torvern, 2954, 726);
            Dock(PlaceId.Sorel, 554, 2062);
            Dock(PlaceId.Kethra, 150, 766);
            Dock(PlaceId.Neth, 102, 1230);
            Dock(PlaceId.Rukh, 475, 233);
            Dock(PlaceId.Ghul, 1462, 1883);
            Dock(PlaceId.Westmark, 366, 702);
            Dock(PlaceId.Kaleth, 1299, 1338);
            Dock(PlaceId.Southmark, 918, 1874);
            Dock(PlaceId.Highmark, 1602, 154);
            Dock(PlaceId.Brineford, 2474, 806);
            Dock(PlaceId.Ridgewatch, 3326, 918);

            Add(PlaceId.Kharun, PlaceId.Draven,
                1408, 856, 1412, 848, 1479, 840, 1488, 830, 1494, 804, 1498, 756,
                1494, 732, 1485, 708, 1473, 672, 1455, 636, 1431, 564, 1409, 528,
                1428, 480, 1440, 420, 1443, 384);
            Add(PlaceId.Kharun, PlaceId.Kaleth,
                1408, 856, 1343, 939, 1250, 1052, 1254, 1174, 1296, 1275, 1299, 1338);
            Add(PlaceId.Kharun, PlaceId.Brineford,
                1408, 856, 1466, 906, 1570, 902, 1654, 870, 1718, 806, 1798, 794,
                1854, 742, 1990, 742, 2074, 778, 2162, 786, 2210, 806, 2278, 814,
                2350, 850, 2406, 858, 2466, 802, 2474, 806);
            Add(PlaceId.Kharun, PlaceId.Westmark,
                1408, 856, 1346, 864, 1164, 848, 1051, 864, 966, 875, 849, 856, 495, 782, 366, 702);
            Add(PlaceId.Kaleth, PlaceId.Zamath,
                1299, 1338, 1202, 1466, 1106, 1498, 906, 1478, 798, 1566);
            Add(PlaceId.Zamath, PlaceId.Southmark,
                798, 1566, 814, 1594, 826, 1658, 902, 1734, 890, 1750, 918, 1770, 930, 1842, 918, 1874);
            Add(PlaceId.Brineford, PlaceId.Torvern,
                2474, 806, 2506, 790, 2582, 850, 2626, 794, 2670, 774, 2754, 686,
                2802, 698, 2834, 722, 2946, 714, 2954, 726);
            Add(PlaceId.Torvern, PlaceId.Thalor,
                2954, 726, 2946, 714, 2870, 722, 2866, 618, 2834, 598, 2798, 602,
                2786, 562, 2746, 502, 2758, 442, 2742, 390, 2790, 318, 2806, 226,
                2826, 226, 2850, 262, 2894, 254, 2926, 262);
            Add(PlaceId.Ghorath, PlaceId.Veythar,
                2795, 1691, 2876, 1719, 2992, 1761, 3118, 1802, 3214, 1854, 3405, 1955);

            Add(PlaceId.Draven, PlaceId.Highmark,
                1443, 384, 1454, 370, 1426, 338, 1422, 306, 1442, 294, 1442, 266, 1578, 150, 1602, 154);
            Add(PlaceId.Westmark, PlaceId.Kethra,
                366, 702, 314, 702, 290, 682, 226, 702, 166, 702, 130, 734, 150, 766);
            Add(PlaceId.Torvern, PlaceId.Ridgewatch,
                2954, 726, 2910, 746, 2938, 774, 2962, 826, 3050, 926, 3090, 906,
                3122, 946, 3154, 946, 3230, 906, 3306, 902, 3326, 918);
            Add(PlaceId.Ridgewatch, PlaceId.Ashar,
                3326, 918, 3458, 920, 3649, 904);
            Add(PlaceId.Brineford, PlaceId.Moraq,
                2474, 806, 2474, 889, 2432, 1074, 2502, 1211);
            Add(PlaceId.Moraq, PlaceId.Ghorath,
                2502, 1211, 2556, 1228, 2669, 1634, 2795, 1691);
            Add(PlaceId.Southmark, PlaceId.Ghul,
                918, 1874, 1094, 1872, 1363, 1871, 1462, 1883);
            Add(PlaceId.Southmark, PlaceId.Sorel,
                918, 1874, 910, 1854, 866, 1854, 842, 1866, 814, 1850, 770, 1870,
                718, 1914, 626, 1930, 574, 1974, 530, 1994, 526, 2030, 554, 2062);

            AddSea(PlaceId.Zamath, PlaceId.Neth);
            AddSea(PlaceId.Westmark, PlaceId.Rukh);

            for (int i = 0; i < NeighborsOf.Length; i++)
                NeighborsOf[i].Sort((left, right) => ((int)left).CompareTo((int)right));
        }

        public static Vector2 DockPoint(PlaceId place)
        {
            return Docks[(int)place];
        }

        public static IReadOnlyList<PlaceId> Neighbors(PlaceId place)
        {
            return NeighborsOf[(int)place];
        }

        public static bool Connects(PlaceId a, PlaceId b)
        {
            return TryFind(a, b, out _);
        }

        public static bool IsSea(PlaceId a, PlaceId b)
        {
            return TryFind(a, b, out Edge edge) && edge.Sea;
        }

        public static float Length(PlaceId a, PlaceId b)
        {
            if (!TryFind(a, b, out Edge edge))
                return 0f;
            return edge.Length;
        }

        public static float Hours(PlaceId a, PlaceId b)
        {
            float length = Length(a, b);
            if (length <= 0f || RoadPath.Length <= 0f)
                return 0f;
            return length / RoadPath.Length * EconomyRates.DayHours;
        }

        public static int Hops(PlaceId from, PlaceId to)
        {
            if (from == to)
                return 0;
            int[] dist = Distances(from);
            int hops = dist[(int)to];
            return hops < 0 ? 99 : hops;
        }

        public static List<PlaceId> Path(PlaceId from, PlaceId to, int maxHops)
        {
            var path = new List<PlaceId>();
            if (from == to)
            {
                path.Add(from);
                return path;
            }

            var parent = new int[WorldIds.PlaceCount];
            var dist = new int[WorldIds.PlaceCount];
            for (int i = 0; i < dist.Length; i++)
            {
                parent[i] = -1;
                dist[i] = -1;
            }

            var queue = new Queue<PlaceId>();
            dist[(int)from] = 0;
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                PlaceId current = queue.Dequeue();
                if (dist[(int)current] >= maxHops)
                    continue;
                IReadOnlyList<PlaceId> next = NeighborsOf[(int)current];
                for (int i = 0; i < next.Count; i++)
                {
                    int id = (int)next[i];
                    if (dist[id] >= 0)
                        continue;
                    dist[id] = dist[(int)current] + 1;
                    parent[id] = (int)current;
                    queue.Enqueue(next[i]);
                }
            }

            if (dist[(int)to] < 0)
                return path;

            var stack = new List<PlaceId>();
            int walk = (int)to;
            stack.Add(to);
            while (walk != (int)from && parent[walk] >= 0)
            {
                walk = parent[walk];
                stack.Add((PlaceId)walk);
            }

            for (int i = stack.Count - 1; i >= 0; i--)
                path.Add(stack[i]);
            return path;
        }

        public static Vector2 PointAlong(PlaceId from, PlaceId to, float distance)
        {
            if (!TryFind(from, to, out Edge edge) || edge.Points == null || edge.Points.Length == 0)
                return from == to ? DockPoint(from) : DockPoint(to);

            float along = Mathf.Clamp(distance, 0f, edge.Length);
            if (edge.A != from)
                along = edge.Length - along;
            return PointAt(edge, along);
        }

        public static IReadOnlyList<Vector2> Samples(PlaceId a, PlaceId b)
        {
            if (!TryFind(a, b, out Edge edge) || edge.Points == null)
                return new Vector2[] { DockPoint(a), DockPoint(b) };
            if (edge.A == a)
                return edge.Points;
            var reversed = new Vector2[edge.Points.Length];
            for (int i = 0; i < reversed.Length; i++)
                reversed[i] = edge.Points[reversed.Length - 1 - i];
            return reversed;
        }

        static int[] Distances(PlaceId from)
        {
            var dist = new int[WorldIds.PlaceCount];
            for (int i = 0; i < dist.Length; i++)
                dist[i] = -1;
            var queue = new Queue<PlaceId>();
            dist[(int)from] = 0;
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                PlaceId current = queue.Dequeue();
                IReadOnlyList<PlaceId> next = NeighborsOf[(int)current];
                for (int i = 0; i < next.Count; i++)
                {
                    int id = (int)next[i];
                    if (dist[id] >= 0)
                        continue;
                    dist[id] = dist[(int)current] + 1;
                    queue.Enqueue(next[i]);
                }
            }

            return dist;
        }

        static void Dock(PlaceId place, int x, int y)
        {
            Docks[(int)place] = new Vector2(x / RoadPath.MapWidth, y / RoadPath.MapHeight);
        }

        static void AddSea(PlaceId a, PlaceId b)
        {
            var edge = new Edge
            {
                A = a,
                B = b,
                Sea = true,
                Length = 0f,
                Points = new Vector2[0],
                Prefix = new float[0]
            };
            Edges.Add(edge);
            NeighborsOf[(int)a].Add(b);
            NeighborsOf[(int)b].Add(a);
        }

        static void Add(PlaceId a, PlaceId b, params int[] pixels)
        {
            int count = pixels.Length / 2;
            var points = new Vector2[count];
            var prefix = new float[count];
            for (int i = 0; i < count; i++)
            {
                points[i] = new Vector2(pixels[i * 2] / RoadPath.MapWidth, pixels[i * 2 + 1] / RoadPath.MapHeight);
                if (i > 0)
                    prefix[i] = prefix[i - 1] + Vector2.Distance(points[i - 1], points[i]);
            }

            Edges.Add(new Edge
            {
                A = a,
                B = b,
                Sea = false,
                Length = count == 0 ? 0f : prefix[count - 1],
                Points = points,
                Prefix = prefix
            });
            NeighborsOf[(int)a].Add(b);
            NeighborsOf[(int)b].Add(a);
        }

        static bool TryFind(PlaceId a, PlaceId b, out Edge edge)
        {
            for (int i = 0; i < Edges.Count; i++)
            {
                Edge candidate = Edges[i];
                if ((candidate.A == a && candidate.B == b) || (candidate.A == b && candidate.B == a))
                {
                    edge = candidate;
                    return true;
                }
            }

            edge = default;
            return false;
        }

        static Vector2 PointAt(Edge edge, float distance)
        {
            if (edge.Points == null || edge.Points.Length == 0)
                return DockPoint(edge.A);
            distance = Mathf.Clamp(distance, 0f, edge.Length);
            for (int i = 1; i < edge.Points.Length; i++)
            {
                if (distance > edge.Prefix[i])
                    continue;
                float span = edge.Prefix[i] - edge.Prefix[i - 1];
                float t = span <= 0.0001f ? 0f : (distance - edge.Prefix[i - 1]) / span;
                return Vector2.Lerp(edge.Points[i - 1], edge.Points[i], t);
            }

            return edge.Points[edge.Points.Length - 1];
        }
    }
}
