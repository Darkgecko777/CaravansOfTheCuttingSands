using UnityEngine;

namespace Caravans.JarsRun
{
    // Weather, vision, and the lesson clock. Shelf prices live in EconomyCatalog.
    // Cargo loss from weather stays at zero until a rate is asked for.
    public static class ThrowawayEconomy
    {
        public const int StartingCoin = 400;
        public const int CardCoin = 8;
        public const float BaseLegHours = EconomyRates.DayHours;
        public const float RealSeconds = 36f;

        public const float VisionDay = 0.34f;
        public const float VisionNight = 0.18f;
        public const float VisionSandstorm = 0.14f;

        public const int JarBase = 40;
        public const int FungusBase = 30;
        public const int WaterBase = 6;
        public const int RationBase = 5;

        public static void ApplyStart(Shelf[,] shelves)
        {
            Set(shelves, PlaceId.Kharun, GoodId.ShimmersteelJars, 8, 8, JarBase);
            Set(shelves, PlaceId.Kharun, GoodId.FingerFungus, 0, 6, FungusBase);
            Set(shelves, PlaceId.Kharun, GoodId.Water, 10, 10, WaterBase);
            Set(shelves, PlaceId.Kharun, GoodId.Rations, 8, 8, RationBase);
            Set(shelves, PlaceId.Draven, GoodId.ShimmersteelJars, 1, 6, JarBase);
            Set(shelves, PlaceId.Draven, GoodId.FingerFungus, 8, 8, FungusBase);
            Set(shelves, PlaceId.Draven, GoodId.Water, 6, 8, WaterBase);
            Set(shelves, PlaceId.Draven, GoodId.Rations, 6, 8, RationBase);
        }

        public static int Price(int basePrice, int stock, int cap)
        {
            float fullness = cap <= 0 ? 1f : Mathf.Clamp01(stock / (float)cap);
            float multiplier = Mathf.Lerp(1.6f, 0.7f, fullness);
            return Mathf.Max(1, Mathf.RoundToInt(basePrice * multiplier));
        }

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

        static void Set(Shelf[,] shelves, PlaceId place, GoodId good, int stock, int cap, int basePrice)
        {
            shelves[(int)place, (int)good] = new Shelf
            {
                Stock = stock,
                Cap = cap,
                BasePrice = basePrice
            };
        }
    }

    public struct Shelf
    {
        public int Stock;
        public int Cap;
        public int BasePrice;
    }
}
