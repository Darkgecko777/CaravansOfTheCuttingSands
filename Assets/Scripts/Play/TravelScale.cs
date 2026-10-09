using UnityEngine;

namespace Caravans.Play
{
    // Purse, leg clock, vision, and weather scale for the running game.
    // Cargo loss from weather stays at zero until a rate is asked for.
    public static class TravelScale
    {
        public const int StartingCoin = 400;
        public const float BaseLegHours = EconomyRates.DayHours;
        public const float RealSeconds = 36f;

        public const float VisionDay = 0.34f;
        public const float VisionNight = 0.18f;
        public const float VisionSandstorm = 0.14f;

        public static float TimeScale(WeatherKind kind)
        {
            switch (kind)
            {
                case WeatherKind.Heat:
                case WeatherKind.Wind:
                    return 1.15f;
                case WeatherKind.Sandstorm:
                    return 1.4f;
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
