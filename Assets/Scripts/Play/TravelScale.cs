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

        public static float DayKit
        {
            get { return GameConfig.Travel.dayKit; }
        }

        public static bool IsDayHour(float hour)
        {
            return hour >= EconomyRates.MorningHour && hour < EconomyRates.NightHour;
        }

        public static void SplitDayNight(float startHour, float span, out float dayHours, out float nightHours)
        {
            dayHours = 0f;
            nightHours = 0f;
            if (span <= 0.00005f)
                return;
            float dayLength = EconomyRates.DayHours;
            if (dayLength <= 0f)
                return;
            float hour = startHour % dayLength;
            if (hour < 0f)
                hour += dayLength;
            float left = span;
            int guard = 0;
            while (left > 0.00005f && guard++ < 16)
            {
                bool day = IsDayHour(hour);
                float boundary = day ? EconomyRates.NightHour : NextMorning(hour, dayLength);
                float step = boundary - hour;
                if (step <= 0.00005f)
                    step = dayLength;
                if (step > left)
                    step = left;
                if (day)
                    dayHours += step;
                else
                    nightHours += step;
                left -= step;
                hour += step;
                if (hour >= dayLength - 0.00005f)
                    hour -= dayLength;
            }
        }

        static float NextMorning(float hour, float dayLength)
        {
            if (hour < EconomyRates.MorningHour)
                return EconomyRates.MorningHour;
            return dayLength + EconomyRates.MorningHour;
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
