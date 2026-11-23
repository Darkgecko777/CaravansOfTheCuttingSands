using System;

namespace Caravans.Play
{
    [Serializable]
    public class WeatherLookFile
    {
        public int schemaVersion;
        public float fadeSeconds;
        public bool showMasses;
        public string sandstormGrade;
        public float sandstormGradeStrength;
        public string sandstormHaze;
        public float sandstormHazeAlpha;
        public string heatGrade;
        public float heatGradeStrength;
        public string heatHaze;
        public float heatHazeAlpha;
        public string windGrade;
        public float windGradeStrength;
        public string windHaze;
        public float windHazeAlpha;
        public float hazeCenterRatio;
        public string massSandstorm;
        public float massSandstormAlpha;
        public string massHeat;
        public float massHeatAlpha;
        public string massWind;
        public float massWindAlpha;
        public float massCore;
        public float noiseScale;
        public float noiseStrength;
        public int noiseSeed;
    }
}
