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
            public string ground;
            public string[] far;
            public string[] mid;
            public string[] near;
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
