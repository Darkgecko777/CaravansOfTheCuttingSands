using System;
using System.Collections.Generic;

namespace Caravans.Play
{
    // Sprite paths and layout for the travel sandbox. Art can replace a path
    // without a code change. A missing file falls back to a placeholder.
    [Serializable]
    public class TravelKitsFile
    {
        public string caravan;
        public string haze;
        public string propFolder;
        public Terrain[] terrains;

        [Serializable]
        public class Rgb
        {
            public float r;
            public float g;
            public float b;
        }

        [Serializable]
        public class PropSpec
        {
            public string path;
            public float ppu;
            public bool mid;
        }

        [Serializable]
        public class Terrain
        {
            public string name;
            public string sky;
            public string skyStrip;
            public string back;
            public float backSpeed;
            public string ground;
            public string[] far;
            public float farSpeed;
            public string[] mid;
            public float midSpeed;
            public string[] near;
            public float nearSpeed;
            public bool nearTiles;
            public string[] props;
            public float propSpeedMin;
            public float propSpeedMax;
            public float nearScale;
            public float nearOverlapPx;
            public float midScale;
            public float midScaleMax;
            public float midBottom;
            public float farScale;
            public float farScaleMax;
            public float farBottom;
            public float farFade;
            public float farGapPx;
            public float farOverlapPx;
            public float farOverlapBelowPx;
            public bool pieceSpacing;
            public float midPropMin;
            public float midPropMax;
            public Rgb nearFill;
            public Rgb backFill;
            public string[] oncePerScreen;
            public PropSpec[] propSpecs;

            public bool HasFill(Rgb rgb)
            {
                return rgb != null && (rgb.r > 0.5f || rgb.g > 0.5f || rgb.b > 0.5f);
            }

            public void SplitProps(out string[] nearLane, out string[] midLane)
            {
                if (propSpecs == null || propSpecs.Length == 0)
                {
                    nearLane = props;
                    midLane = props;
                    return;
                }

                var nearList = new List<string>();
                var midList = new List<string>();
                for (int i = 0; i < propSpecs.Length; i++)
                {
                    PropSpec spec = propSpecs[i];
                    if (spec == null || string.IsNullOrEmpty(spec.path))
                        continue;
                    if (spec.mid)
                        midList.Add(spec.path);
                    else
                        nearList.Add(spec.path);
                }

                nearLane = nearList.Count > 0 ? nearList.ToArray() : props;
                midLane = midList.Count > 0 ? midList.ToArray() : props;
            }
        }

        public Terrain Find(string name)
        {
            if (terrains == null || string.IsNullOrEmpty(name))
                return null;
            for (int i = 0; i < terrains.Length; i++)
            {
                if (terrains[i] != null && terrains[i].name == name)
                    return terrains[i];
            }

            return null;
        }
    }
}
