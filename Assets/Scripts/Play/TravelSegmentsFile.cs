using System;

namespace Caravans.Play
{
    // Fields the travel sandbox reads from data/config/travel_segments.json.
    // JsonUtility ignores the rest of the chart.
    [Serializable]
    public class TravelSegmentsFile
    {
        public int schemaVersion;
        public Route[] routes;

        [Serializable]
        public class Route
        {
            public string id;
            public string from;
            public string to;
            public string fromName;
            public string toName;
            public string kind;
            public bool travelScene;
            public float hoursClearSteady;
            public Segment[] segments;
        }

        [Serializable]
        public class Segment
        {
            public int index;
            public int sharePercent;
            public string terrain;
            public string near;
            public Prop[] props;
        }

        [Serializable]
        public class Prop
        {
            public string id;
            public string near;
            public string text;
        }
    }
}
