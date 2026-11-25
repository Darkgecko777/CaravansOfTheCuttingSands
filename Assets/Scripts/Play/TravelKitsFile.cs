using System;

namespace Caravans.Play
{
    // Sprite resource paths for the travel sandbox. Art can replace a path
    // without a code change. A missing file falls back to a placeholder.
    [Serializable]
    public class TravelKitsFile
    {
        public string caravan;
        public string haze;
        public string propFolder;
        public Terrain[] terrains;

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
