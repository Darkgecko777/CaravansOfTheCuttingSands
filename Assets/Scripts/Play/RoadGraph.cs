using System.Collections.Generic;
using UnityEngine;

namespace Caravans.Play
{
    // Painted roads on upscale_map.png. y is from the top. Length is the
    // uncorrected normalized length of the traced stroke, solid and dotted
    // alike. Hours compare that length with the Kharûn–Draven road, which is
    // one clear day. Weather stretches a leg. A sea opening has no line and
    // no hours, and it still counts as one hop.
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

            Dock(PlaceId.Kharun, 1256, 827);
            Dock(PlaceId.Draven, 1403, 289);
            Dock(PlaceId.Zamath, 672, 1413);
            Dock(PlaceId.Thalor, 2820, 131);
            Dock(PlaceId.Veythar, 3311, 1865);
            Dock(PlaceId.Ghorath, 2410, 1511);
            Dock(PlaceId.Ashar, 3486, 956);
            Dock(PlaceId.Moraq, 2449, 1116);
            Dock(PlaceId.Torvern, 2825, 636);
            Dock(PlaceId.Sorel, 480, 2005);
            Dock(PlaceId.Kethra, 51, 781);
            Dock(PlaceId.Neth, 105, 1233);
            Dock(PlaceId.Rukh, 479, 236);
            Dock(PlaceId.Ghul, 1415, 1778);
            Dock(PlaceId.Westmark, 349, 602);
            Dock(PlaceId.Kaleth, 1237, 1141);
            Dock(PlaceId.Southmark, 932, 1768);
            Dock(PlaceId.Highmark, 1654, 81);
            Dock(PlaceId.Brineford, 2299, 809);
            Dock(PlaceId.Ridgewatch, 3076, 953);

            Add(PlaceId.Kharun, PlaceId.Draven,
                1256, 827, 1240, 838, 1256, 854, 1274, 840, 1298, 840, 1304, 844,
                1316, 844, 1338, 858, 1390, 858, 1404, 854, 1408, 846, 1418, 838,
                1472, 838, 1482, 832, 1488, 818, 1494, 756, 1490, 734, 1480, 706,
                1480, 682, 1450, 618, 1440, 574, 1420, 544, 1412, 526, 1414, 502,
                1424, 488, 1424, 480, 1436, 456, 1436, 406, 1442, 394, 1444, 372,
                1432, 354, 1432, 344, 1426, 334, 1426, 326, 1420, 314, 1410, 304,
                1408, 288, 1403, 289);
            Add(PlaceId.Kharun, PlaceId.Kaleth,
                1256, 827, 1240, 838, 1238, 844, 1228, 846, 1256, 860, 1258, 886,
                1248, 900, 1248, 912, 1236, 934, 1236, 958, 1248, 998, 1248, 1106,
                1238, 1126, 1236, 1142, 1237, 1141);
            Add(PlaceId.Kharun, PlaceId.Brineford,
                1256, 827, 1240, 838, 1256, 854, 1274, 840, 1298, 840, 1304, 844,
                1316, 844, 1332, 856, 1352, 860, 1392, 858, 1404, 854, 1408, 846,
                1414, 846, 1416, 866, 1438, 888, 1454, 898, 1472, 902, 1558, 900,
                1586, 890, 1602, 890, 1660, 870, 1676, 854, 1696, 826, 1712, 812,
                1734, 804, 1776, 802, 1788, 798, 1802, 790, 1838, 752, 1854, 744,
                1876, 744, 1896, 748, 1980, 744, 2020, 756, 2034, 756, 2074, 770,
                2140, 778, 2148, 786, 2202, 804, 2236, 804, 2300, 816, 2299, 809);
            Add(PlaceId.Kharun, PlaceId.Westmark,
                1256, 827, 1240, 838, 1228, 838, 1204, 846, 1160, 844, 1120, 860,
                1088, 868, 1066, 866, 1036, 854, 1006, 854, 954, 876, 936, 880,
                916, 878, 896, 868, 880, 868, 864, 862, 844, 860, 818, 868, 804, 868,
                776, 884, 738, 896, 702, 914, 674, 916, 662, 912, 648, 902, 634, 884,
                612, 864, 556, 854, 538, 846, 526, 832, 514, 804, 424, 716, 352, 640,
                352, 618, 349, 602);
            Add(PlaceId.Kaleth, PlaceId.Zamath,
                1237, 1141, 1236, 1142, 1238, 1148, 1246, 1150, 1254, 1160, 1260, 1180,
                1260, 1192, 1276, 1208, 1284, 1224, 1288, 1262, 1300, 1300, 1300, 1326,
                1294, 1348, 1296, 1386, 1294, 1398, 1286, 1412, 1268, 1420, 1246, 1422,
                1230, 1428, 1196, 1464, 1188, 1468, 1154, 1470, 1114, 1488, 1090, 1490,
                1078, 1486, 1068, 1478, 1058, 1478, 1040, 1462, 1024, 1456, 1000, 1458,
                974, 1468, 918, 1468, 902, 1472, 854, 1468, 830, 1456, 816, 1456,
                766, 1466, 738, 1454, 720, 1438, 694, 1434, 672, 1413);
            Add(PlaceId.Zamath, PlaceId.Southmark,
                672, 1413, 694, 1434, 728, 1474, 738, 1500, 752, 1516, 752, 1526,
                786, 1556, 806, 1590, 816, 1600, 822, 1640, 836, 1670, 852, 1686,
                892, 1716, 930, 1768, 932, 1768);
            Add(PlaceId.Brineford, PlaceId.Torvern,
                2299, 809, 2300, 816, 2324, 840, 2340, 840, 2358, 848, 2396, 850,
                2410, 856, 2420, 864, 2436, 886, 2456, 902, 2480, 902, 2512, 886,
                2550, 880, 2566, 868, 2584, 844, 2594, 822, 2624, 792, 2662, 776,
                2708, 726, 2730, 710, 2756, 670, 2770, 662, 2804, 654, 2814, 640,
                2824, 636, 2825, 636);
            Add(PlaceId.Torvern, PlaceId.Thalor,
                2825, 636, 2824, 636, 2800, 612, 2788, 566, 2762, 532, 2752, 514,
                2750, 492, 2760, 464, 2760, 450, 2746, 402, 2746, 384, 2758, 360,
                2758, 350, 2774, 332, 2784, 312, 2788, 290, 2786, 222, 2802, 168,
                2812, 148, 2820, 131);
            Add(PlaceId.Ghorath, PlaceId.Veythar,
                2410, 1511, 2422, 1510, 2428, 1512, 2426, 1522, 2434, 1540, 2464, 1550,
                2490, 1564, 2550, 1576, 2574, 1584, 2630, 1622, 2696, 1634, 2714, 1640,
                2728, 1648, 2748, 1668, 2764, 1678, 2782, 1684, 2848, 1696, 2896, 1728,
                2976, 1748, 3016, 1768, 3070, 1776, 3098, 1784, 3122, 1800, 3150, 1830,
                3168, 1840, 3188, 1846, 3246, 1854, 3266, 1864, 3306, 1876, 3311, 1865);

            Add(PlaceId.Draven, PlaceId.Highmark,
                1403, 289, 1408, 288, 1420, 280, 1432, 264, 1472, 242, 1484, 230,
                1490, 230, 1496, 224, 1500, 224, 1524, 200, 1548, 186, 1596, 136,
                1646, 110, 1658, 96, 1654, 81);
            Add(PlaceId.Westmark, PlaceId.Kethra,
                349, 602, 352, 618, 320, 646, 288, 654, 268, 668, 262, 668, 240, 688,
                226, 696, 214, 698, 210, 704, 166, 706, 138, 726, 118, 750, 58, 782,
                51, 781);
            Add(PlaceId.Torvern, PlaceId.Ridgewatch,
                2825, 636, 2824, 636, 2862, 674, 2868, 700, 2882, 724, 2882, 732,
                2928, 770, 2940, 796, 2958, 818, 3020, 880, 3034, 912, 3058, 932,
                3062, 944, 3082, 962, 3076, 953);
            Add(PlaceId.Ridgewatch, PlaceId.Ashar,
                3076, 953, 3082, 962, 3086, 964, 3096, 960, 3120, 946, 3154, 948,
                3172, 942, 3216, 912, 3228, 908, 3260, 910, 3290, 902, 3302, 902,
                3314, 914, 3318, 912, 3340, 922, 3392, 928, 3418, 938, 3438, 938,
                3468, 952, 3470, 950, 3482, 958, 3486, 956);
            Add(PlaceId.Brineford, PlaceId.Moraq,
                2299, 809, 2300, 816, 2330, 846, 2340, 882, 2348, 892, 2350, 908,
                2370, 948, 2378, 952, 2386, 982, 2402, 1004, 2402, 1008, 2410, 1014,
                2422, 1032, 2428, 1046, 2430, 1072, 2434, 1074, 2428, 1104, 2454, 1114,
                2452, 1116, 2449, 1116);
            Add(PlaceId.Moraq, PlaceId.Ghorath,
                2449, 1116, 2452, 1116, 2456, 1120, 2438, 1136, 2450, 1164, 2450, 1212,
                2432, 1244, 2426, 1270, 2426, 1288, 2438, 1346, 2438, 1368, 2434, 1378,
                2430, 1410, 2432, 1490, 2426, 1496, 2426, 1504, 2422, 1510, 2410, 1511);
            Add(PlaceId.Southmark, PlaceId.Ghul,
                932, 1768, 930, 1768, 934, 1774, 958, 1780, 978, 1780, 1006, 1796,
                1030, 1798, 1074, 1786, 1106, 1790, 1108, 1788, 1138, 1798, 1174, 1794,
                1186, 1788, 1220, 1788, 1234, 1784, 1264, 1768, 1276, 1766, 1294, 1768,
                1316, 1778, 1324, 1778, 1346, 1790, 1366, 1790, 1394, 1780, 1398, 1782,
                1416, 1778, 1415, 1778);
            Add(PlaceId.Southmark, PlaceId.Sorel,
                932, 1768, 930, 1768, 920, 1790, 906, 1800, 896, 1802, 892, 1808,
                884, 1810, 884, 1814, 878, 1814, 868, 1822, 850, 1826, 814, 1848,
                808, 1848, 794, 1860, 780, 1866, 758, 1886, 740, 1894, 734, 1900,
                732, 1898, 720, 1906, 706, 1912, 704, 1910, 698, 1916, 666, 1918,
                628, 1930, 598, 1954, 590, 1964, 572, 1972, 570, 1970, 564, 1976,
                542, 1984, 536, 1990, 534, 1988, 506, 2004, 488, 2010, 480, 2005);

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
