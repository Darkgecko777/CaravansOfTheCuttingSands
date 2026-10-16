using UnityEngine;

namespace Caravans.Play
{
    // Purse, leg clock, vision, and weather scale for the running game.
    // Cargo loss from weather stays at zero until a rate is asked for.
    public static class TravelScale
    {
        public static int StartingCoin
        {
            get { return GameConfig.Travel.startingCoin; }
        }

        public static float BaseLegHours
        {
            get { return EconomyRates.DayHours; }
        }

        public static float RealSeconds
        {
            get { return GameConfig.Travel.realSeconds; }
        }

        public static float WindowHours
        {
            get { return GameConfig.Travel.travelWindowHours; }
        }

        public static float ReserveHours
        {
            get { return GameConfig.Travel.reserveHours; }
        }

        public static float TownRestHours
        {
            get { return GameConfig.Travel.townRestHours; }
        }

        public static float CampHours
        {
            get { return GameConfig.Travel.campHours; }
        }

        public static float Speed(RoadPace pace)
        {
            switch (pace)
            {
                case RoadPace.Rush:
                    return GameConfig.Travel.rushSpeed;
                case RoadPace.Careful:
                    return GameConfig.Travel.carefulSpeed;
                default:
                    return GameConfig.Travel.steadySpeed;
            }
        }

        public static float Kit(RoadPace pace)
        {
            switch (pace)
            {
                case RoadPace.Rush:
                    return GameConfig.Travel.rushKit;
                case RoadPace.Careful:
                    return GameConfig.Travel.carefulKit;
                default:
                    return GameConfig.Travel.steadyKit;
            }
        }

        public static float VisionDay
        {
            get { return GameConfig.Weather.visionDay; }
        }

        public static float VisionNight
        {
            get { return GameConfig.Weather.visionNight; }
        }

        public static float VisionSandstorm
        {
            get { return GameConfig.Weather.visionSandstorm; }
        }

        public static float TimeScale(WeatherKind kind)
        {
            switch (kind)
            {
                case WeatherKind.Heat:
                    return GameConfig.Weather.heatStretch;
                case WeatherKind.Wind:
                    return GameConfig.Weather.windStretch;
                case WeatherKind.Sandstorm:
                    return GameConfig.Weather.sandstormStretch;
                default:
                    return 1f;
            }
        }

        public static int Severity(WeatherKind kind)
        {
            switch (kind)
            {
                case WeatherKind.Sandstorm:
                    return 3;
                case WeatherKind.Heat:
                case WeatherKind.Wind:
                    return 2;
                default:
                    return 0;
            }
        }

        public static float Sunlight(float hour)
        {
            float angle = (hour - 12f) / 12f * Mathf.PI;
            return Mathf.Clamp01(Mathf.Cos(angle) * 0.5f + 0.5f);
        }

        public static float VisionRadius(float hour, WeatherKind local)
        {
            if (local == WeatherKind.Sandstorm)
                return VisionSandstorm;
            return Mathf.Lerp(VisionNight, VisionDay, Sunlight(hour));
        }
    }
}
