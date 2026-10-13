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
        static AbundanceTable abundance;

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

        public static AbundanceTable Abundance
        {
            get { EnsureLoaded(); return abundance; }
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
            consumption = Read<ConsumptionFile>("consumption.json",
                "ordinaryDecay", "sweetBlissDecay", "rationEat",
                "waterLitresPerDay", "rationsKgPerDay",
                "canteenLitres", "rationBagKg",
                "waterLitresPerUnit", "rationsKgPerUnit");
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
            abundance = ReadAbundance();
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

        static AbundanceTable ReadAbundance()
        {
            AbundanceFile file = Read<AbundanceFile>("abundance.json",
                "gradeAStartDays", "gradeAMake",
                "gradeBStartDays", "gradeBMake",
                "gradeCStartDays", "gradeCMake",
                "gradeDStartDays", "gradeDMake",
                "seaStart", "seaMake", "seaCap",
                "rows", "place", "food", "water", "sea");
            if (file.rows == null || file.rows.Length != WorldIds.PlaceCount)
                throw new InvalidOperationException("abundance.json needs one row for every place.");
            if (file.seaStart <= 0 || file.seaMake <= 0f || file.seaCap <= 0)
                throw new InvalidOperationException("abundance.json sea supply must be above zero.");

            var table = new AbundanceTable
            {
                FoodDays = new float[WorldIds.PlaceCount],
                FoodMake = new float[WorldIds.PlaceCount],
                WaterDays = new float[WorldIds.PlaceCount],
                WaterMake = new float[WorldIds.PlaceCount],
                Sea = new bool[WorldIds.PlaceCount],
                SeaStart = file.seaStart,
                SeaMake = file.seaMake,
                SeaCap = file.seaCap
            };
            var seen = new bool[WorldIds.PlaceCount];
            for (int i = 0; i < file.rows.Length; i++)
            {
                AbundanceRow row = file.rows[i];
                if (row == null)
                    throw new InvalidOperationException("abundance.json row " + i + " is empty.");
                PlaceId place = Parse<PlaceId>(row.place, "place");
                if (seen[(int)place])
                    throw new InvalidOperationException("abundance.json repeats " + row.place + ".");
                seen[(int)place] = true;
                Grade(file, row.food, out float foodDays, out float foodMake);
                Grade(file, row.water, out float waterDays, out float waterMake);
                table.FoodDays[(int)place] = foodDays;
                table.FoodMake[(int)place] = foodMake;
                table.WaterDays[(int)place] = waterDays;
                table.WaterMake[(int)place] = waterMake;
                table.Sea[(int)place] = row.sea;
            }

            return table;
        }

        static void Grade(AbundanceFile file, string letter, out float days, out float make)
        {
            switch (letter)
            {
                case "A":
                    days = file.gradeAStartDays;
                    make = file.gradeAMake;
                    break;
                case "B":
                    days = file.gradeBStartDays;
                    make = file.gradeBMake;
                    break;
                case "C":
                    days = file.gradeCStartDays;
                    make = file.gradeCMake;
                    break;
                case "D":
                    days = file.gradeDStartDays;
                    make = file.gradeDMake;
                    break;
                default:
                    throw new InvalidOperationException("abundance.json has an unknown grade: " + letter);
            }

            if (days <= 0f || make <= 0f)
                throw new InvalidOperationException("abundance.json grade " + letter + " must be above zero.");
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
        public float waterLitresPerDay;
        public float rationsKgPerDay;
        public float canteenLitres;
        public float rationBagKg;
        public float waterLitresPerUnit;
        public float rationsKgPerUnit;
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

    [Serializable]
    public class AbundanceFile
    {
        public float gradeAStartDays;
        public float gradeAMake;
        public float gradeBStartDays;
        public float gradeBMake;
        public float gradeCStartDays;
        public float gradeCMake;
        public float gradeDStartDays;
        public float gradeDMake;
        public int seaStart;
        public float seaMake;
        public int seaCap;
        public AbundanceRow[] rows;
    }

    [Serializable]
    public class AbundanceRow
    {
        public string place;
        public string food;
        public string water;
        public bool sea;
    }

    public sealed class AbundanceTable
    {
        public float[] FoodDays;
        public float[] FoodMake;
        public float[] WaterDays;
        public float[] WaterMake;
        public bool[] Sea;
        public int SeaStart;
        public float SeaMake;
        public int SeaCap;
    }
}
