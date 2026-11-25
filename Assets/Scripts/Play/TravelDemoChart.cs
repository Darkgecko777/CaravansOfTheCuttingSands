using System;
using System.Collections.Generic;
using UnityEngine;

namespace Caravans.Play
{
    // The sandbox route. Live RoadGraph hours and RoadPace stay untouched.
    public sealed class TravelDemoChart
    {
        public const string DefaultRouteId = "brineford-moraq";
        const float Fade = 0.08f;

        public struct Mix
        {
            public string a;
            public string b;
            public float bWeight;
        }

        public struct PlacedProp
        {
            public string id;
            public float routeX;
            public string terrain;
        }

        readonly List<TravelSegmentsFile.Segment> order = new List<TravelSegmentsFile.Segment>();
        readonly List<PlacedProp> props = new List<PlacedProp>();

        public TravelSegmentsFile.Route[] Routes { get; private set; }
        public TravelKitsFile Kits { get; private set; }
        public int RouteIndex { get; private set; }
        public bool Forward { get; private set; }
        public string TerrainOverride { get; private set; }
        public int Seed { get; private set; }

        public TravelSegmentsFile.Route Route
        {
            get { return Routes[RouteIndex]; }
        }

        public float WorldLength
        {
            get { return Mathf.Max(12f, Route.hoursClearSteady * 8f); }
        }

        public static TravelDemoChart Load()
        {
            TravelSegmentsFile file = GameConfig.ReadFile<TravelSegmentsFile>(
                "travel_segments.json", "schemaVersion", "routes");
            TravelKitsFile kits = GameConfig.ReadFile<TravelKitsFile>(
                "travel_kits.json", "terrains");
            if (file.schemaVersion != 2)
                Debug.LogWarning("Travel demo expected schemaVersion 2, read " + file.schemaVersion + ".");
            var chart = new TravelDemoChart();
            chart.Kits = kits;
            chart.Routes = Playable(file);
            if (chart.Routes.Length == 0)
                throw new InvalidOperationException("travel_segments.json has no travel scene.");
            chart.Seed = 112;
            chart.Forward = true;
            chart.SelectRoute(DefaultRouteId);
            return chart;
        }

        static TravelSegmentsFile.Route[] Playable(TravelSegmentsFile file)
        {
            if (file.routes == null)
                return new TravelSegmentsFile.Route[0];
            var kept = new List<TravelSegmentsFile.Route>();
            for (int i = 0; i < file.routes.Length; i++)
            {
                TravelSegmentsFile.Route route = file.routes[i];
                if (route == null || !route.travelScene)
                    continue;
                if (route.segments == null || route.segments.Length == 0)
                    continue;
                kept.Add(route);
            }

            return kept.ToArray();
        }

        public void SelectRoute(string id)
        {
            for (int i = 0; i < Routes.Length; i++)
            {
                if (Routes[i].id == id)
                {
                    SelectRoute(i);
                    return;
                }
            }

            SelectRoute(0);
        }

        public void SelectRoute(int index)
        {
            if (index < 0 || index >= Routes.Length)
                index = 0;
            RouteIndex = index;
            RebuildProps();
        }

        public void SetForward(bool forward)
        {
            if (Forward == forward)
                return;
            Forward = forward;
            RebuildProps();
        }

        public void SetOverride(string terrain)
        {
            if (terrain == "Chart")
                terrain = null;
            TerrainOverride = string.IsNullOrEmpty(terrain) ? null : terrain;
        }

        public void SetSeed(int seed)
        {
            Seed = seed;
        }

        public string RouteLabel(int index)
        {
            TravelSegmentsFile.Route route = Routes[index];
            string from = string.IsNullOrEmpty(route.fromName) ? route.from : route.fromName;
            string to = string.IsNullOrEmpty(route.toName) ? route.to : route.toName;
            return from + " to " + to;
        }

        public string DirectionLabel(bool forward)
        {
            string from = string.IsNullOrEmpty(Route.fromName) ? Route.from : Route.fromName;
            string to = string.IsNullOrEmpty(Route.toName) ? Route.to : Route.toName;
            if (!forward)
            {
                string swap = from;
                from = to;
                to = swap;
            }

            return from + " to " + to;
        }

        public Mix MixAt(float progress)
        {
            Mix mix;
            if (!string.IsNullOrEmpty(TerrainOverride))
            {
                mix.a = TerrainOverride;
                mix.b = TerrainOverride;
                mix.bWeight = 0f;
                return mix;
            }

            FillOrder();
            if (order.Count == 0)
            {
                mix.a = "Dune Sea";
                mix.b = "Dune Sea";
                mix.bWeight = 0f;
                return mix;
            }

            float total = ShareTotal();
            float p = Mathf.Repeat(progress, 1f);
            float cursor = 0f;
            int index = order.Count - 1;
            float start = 0f;
            float span = 1f;
            for (int i = 0; i < order.Count; i++)
            {
                float next = cursor + order[i].sharePercent / total;
                if (p < next || i == order.Count - 1)
                {
                    index = i;
                    start = cursor;
                    span = Mathf.Max(0.0001f, next - cursor);
                    break;
                }

                cursor = next;
            }

            string here = order[index].terrain;
            float along = (p - start) / span;
            int prev = index == 0 ? order.Count - 1 : index - 1;
            int nextIndex = index + 1 >= order.Count ? 0 : index + 1;
            if (along < Fade)
            {
                mix.a = order[prev].terrain;
                mix.b = here;
                mix.bWeight = Mathf.Clamp01(along / Fade);
                return mix;
            }

            if (along > 1f - Fade)
            {
                mix.a = here;
                mix.b = order[nextIndex].terrain;
                mix.bWeight = Mathf.Clamp01((along - (1f - Fade)) / Fade);
                return mix;
            }

            mix.a = here;
            mix.b = here;
            mix.bWeight = 0f;
            return mix;
        }

        public IReadOnlyList<PlacedProp> Props
        {
            get { return props; }
        }

        void RebuildProps()
        {
            props.Clear();
            FillOrder();
            float total = ShareTotal();
            float length = WorldLength;
            string arrival = Forward ? Route.to : Route.from;
            float cursor = 0f;
            for (int i = 0; i < order.Count; i++)
            {
                TravelSegmentsFile.Segment segment = order[i];
                float span = segment.sharePercent / total * length;
                TravelSegmentsFile.Prop[] marks = segment.props;
                if (marks != null)
                {
                    int slot = 0;
                    for (int p = 0; p < marks.Length; p++)
                    {
                        TravelSegmentsFile.Prop prop = marks[p];
                        if (prop == null || string.IsNullOrEmpty(prop.id))
                            continue;
                        bool atArrival = prop.near == arrival;
                        float margin = Mathf.Max(1.2f, span * 0.16f);
                        float step = margin * 0.55f;
                        float along = margin * 0.35f + slot * step;
                        float x = atArrival ? cursor + span - along : cursor + along;
                        x = Mathf.Clamp(x, cursor + 0.4f, cursor + span - 0.4f);
                        PlacedProp placed;
                        placed.id = prop.id;
                        placed.routeX = x;
                        placed.terrain = segment.terrain;
                        props.Add(placed);
                        slot++;
                    }
                }

                cursor += span;
            }
        }

        void FillOrder()
        {
            order.Clear();
            TravelSegmentsFile.Segment[] segments = Route.segments;
            if (segments == null)
                return;
            if (Forward)
            {
                for (int i = 0; i < segments.Length; i++)
                {
                    if (segments[i] != null)
                        order.Add(segments[i]);
                }
            }
            else
            {
                for (int i = segments.Length - 1; i >= 0; i--)
                {
                    if (segments[i] != null)
                        order.Add(segments[i]);
                }
            }
        }

        float ShareTotal()
        {
            float total = 0f;
            for (int i = 0; i < order.Count; i++)
                total += Mathf.Max(0, order[i].sharePercent);
            if (total <= 0f)
                total = Mathf.Max(1, order.Count);
            return total;
        }
    }
}
