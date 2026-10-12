using System;
using System.IO;
using UnityEngine;

namespace Caravans.Play
{
    // Tunable numbers live in data/config. The play assembly reads them from disk.
    public static class GameConfig
    {
        static bool loaded;
        static ConsumptionFile consumption;
        static PriceFile prices;
        static WeatherFile weather;
        static TravelFile travel;
        static RivalFile rivals;
        static Recipe[] workshops;

        public static ConsumptionFile Consumption
        {
            get { EnsureLoaded(); return consumption; }
        }

        public static PriceFile Prices
        {
            get { EnsureLoaded(); return prices; }
        }

        public static WeatherFile Weather
        {
            get { EnsureLoaded(); return weather; }
        }

        public static TravelFile Travel
        {
            get { EnsureLoaded(); return travel; }
        }

        public static RivalFile Rivals
        {
            get { EnsureLoaded(); return rivals; }
        }

        public static Recipe[] Workshops
        {
            get { EnsureLoaded(); return workshops; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            loaded = false;
            EconomyCatalog.ForgetLoaded();
        }

        public static void EnsureLoaded()
        {
            if (loaded)
                return;
            consumption = Read<ConsumptionFile>("consumption.json", "ordinaryDecay", "sweetBlissDecay", "rationEat");
            prices = Read<PriceFile>("prices.json",
                "fullCoverDays", "ordinaryCoverDays", "emptyMultiplier", "fullMultiplier", "productionScale",
                "waterBasePrice", "waterWeight", "waterSpace",
                "rationBasePrice", "rationWeight", "rationSpace",
                "tariffBase", "tariffFloor", "standingMax", "influenceKeep");
            weather = Read<WeatherFile>("weather.json",
                "heatStretch", "windStretch", "sandstormStretch",
                "visionDay", "visionNight", "visionSandstorm");
            travel = Read<TravelFile>("travel.json",
                "startingCoin", "wagonSpace", "wagonWeight",
                "dayHours", "morningHour", "nightHour", "realSeconds", "seaHourFloor", "cardCoin");
            rivals = Read<RivalFile>("rivals.json", "hopLimit", "seed");
            workshops = ReadWorkshops();
            loaded = true;
        }

        static Recipe[] ReadWorkshops()
        {
            WorkshopFile file = Read<WorkshopFile>("workshops.json", "recipes", "place", "output", "inputA", "countA", "inputB", "countB");
            if (file.recipes == null || file.recipes.Length == 0)
                throw new InvalidOperationException("workshops.json has no recipes.");
            var recipes = new Recipe[file.recipes.Length];
            for (int i = 0; i < file.recipes.Length; i++)
            {
                WorkshopRow row = file.recipes[i];
                if (row == null || row.countA <= 0)
                    throw new InvalidOperationException("workshops.json recipe " + i + " has no first input count.");
                recipes[i] = new Recipe(
                    Parse<PlaceId>(row.place, "place"),
                    Parse<GoodId>(row.output, "output"),
                    Parse<GoodId>(row.inputA, "inputA"),
                    row.countA,
                    Parse<GoodId>(row.inputB, "inputB"),
                    row.countB);
            }

            return recipes;
        }

        static TEnum Parse<TEnum>(string name, string field) where TEnum : struct
        {
            if (!string.IsNullOrEmpty(name) && Enum.TryParse(name, false, out TEnum value))
                return value;
            throw new InvalidOperationException("workshops.json has an unknown " + field + ": " + name);
        }

        static T Read<T>(string file, params string[] keys) where T : class
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "data", "config", file));
            if (!File.Exists(path))
                throw new FileNotFoundException("Config was not found.", path);
            string text = File.ReadAllText(path);
            for (int i = 0; i < keys.Length; i++)
            {
                if (text.IndexOf("\"" + keys[i] + "\"", StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException(file + " is missing " + keys[i]);
            }

            T value = JsonUtility.FromJson<T>(text);
            if (value == null)
                throw new InvalidOperationException("Config did not parse: " + file);
            return value;
        }
    }

    [Serializable]
    public class ConsumptionFile
    {
        public float ordinaryDecay;
        public float sweetBlissDecay;
        public float rationEat;
    }

    [Serializable]
    public class PriceFile
    {
        public int fullCoverDays;
        public int ordinaryCoverDays;
        public float emptyMultiplier;
        public float fullMultiplier;
        public float productionScale;
        public int waterBasePrice;
        public int waterWeight;
        public int waterSpace;
        public int rationBasePrice;
        public int rationWeight;
        public int rationSpace;
        public int tariffBase;
        public int tariffFloor;
        public int standingMax;
        public float influenceKeep;
    }

    [Serializable]
    public class WeatherFile
    {
        public float heatStretch;
        public float windStretch;
        public float sandstormStretch;
        public float visionDay;
        public float visionNight;
        public float visionSandstorm;
    }

    [Serializable]
    public class TravelFile
    {
        public int startingCoin;
        public int wagonSpace;
        public int wagonWeight;
        public float dayHours;
        public int morningHour;
        public int nightHour;
        public float realSeconds;
        public float seaHourFloor;
        public int cardCoin;
    }

    [Serializable]
    public class RivalFile
    {
        public int hopLimit;
        public int seed;
    }

    [Serializable]
    public class WorkshopFile
    {
        public WorkshopRow[] recipes;
    }

    [Serializable]
    public class WorkshopRow
    {
        public string place;
        public string output;
        public string inputA;
        public int countA;
        public string inputB;
        public int countB;
    }
}
