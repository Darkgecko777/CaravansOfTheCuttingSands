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
        static EncounterFile encounters;
        static Recipe[] workshops;
        static AbundanceTable abundance;
        static KitFile kit;
        static SoftResetFile softReset;
        static RescueFile rescue;
        static MapPieceFile mapPieces;
        static EndScreenFile endScreen;
        static AgentFile agents;
        static WeatherLookFile weatherLook;
        static MapTokenFile mapTokens;

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

        public static EncounterFile Encounters
        {
            get { EnsureLoaded(); return encounters; }
        }

        public static Recipe[] Workshops
        {
            get { EnsureLoaded(); return workshops; }
        }

        public static AbundanceTable Abundance
        {
            get { EnsureLoaded(); return abundance; }
        }

        public static KitFile Kit
        {
            get { EnsureLoaded(); return kit; }
        }

        public static SoftResetFile SoftReset
        {
            get { EnsureLoaded(); return softReset; }
        }

        public static RescueFile Rescue
        {
            get { EnsureLoaded(); return rescue; }
        }

        public static MapPieceFile MapPieces
        {
            get { EnsureLoaded(); return mapPieces; }
        }

        public static EndScreenFile EndScreen
        {
            get { EnsureLoaded(); return endScreen; }
        }

        public static AgentFile Agents
        {
            get { EnsureLoaded(); return agents; }
        }

        public static WeatherLookFile WeatherLook
        {
            get { EnsureLoaded(); return weatherLook; }
        }

        public static MapTokenFile MapTokens
        {
            get { EnsureLoaded(); return mapTokens; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            loaded = false;
            mapPieces = null;
            endScreen = null;
            agents = null;
            weatherLook = null;
            mapTokens = null;
            global::Caravans.Play.WeatherLook.Forget();
            EconomyCatalog.ForgetLoaded();
            EncounterCards.ForgetLoaded();
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
                "fullCoverDays", "ordinaryCoverDays", "emptyMultiplier", "fullMultiplier", "merchantSpread", "productionScale",
                "waterBasePrice", "waterWeight", "waterSpace",
                "rationBasePrice", "rationWeight", "rationSpace",
                "tariffBase", "standingCap", "standingPointsPerRank",
                "standingCoinPerPointBase", "standingCoinPerPointStep", "standingFlipBonus",
                "influenceKeep");
            weather = Read<WeatherFile>("weather.json",
                "heatStretch", "windStretch", "sandstormStretch",
                "visionDay", "visionNight", "visionSandstorm");
            travel = Read<TravelFile>("travel.json",
                "startingCoin", "wagonSpace", "wagonWeight",
                "dayHours", "morningHour", "nightHour", "realSeconds", "seaHourFloor",
                "travelWindowHours", "reserveHours", "townRestHours", "campHours",
                "rushSpeed", "rushKit", "steadySpeed", "steadyKit", "carefulSpeed", "carefulKit",
                "dayKit");
            rivals = Read<RivalFile>("rivals.json", "hopLimit");
            encounters = Read<EncounterFile>("encounters.json",
                "baseChance", "carefulOffset", "rushOffset", "floorChance",
                "fireHourMin", "fireHourMax");
            workshops = ReadWorkshops();
            abundance = ReadAbundance();
            kit = TryRead<KitFile>("kit_consumption.json",
                "schemaVersion", "merchant", "tiers", "defaultCaravan", "units", "autoPour", "warning", "departureCheck");
            softReset = Read<SoftResetFile>("soft_reset.json",
                "schemaVersion", "carriedCoinFraction", "paupersPurse", "pauperFloorCoin", "kitOnWakingDays", "returnTo");
            rescue = Read<RescueFile>("rescue_texts.json",
                "schemaVersion", "title", "lead", "regions", "rescueDays", "texts", "roads");
            mapPieces = Read<MapPieceFile>("map_pieces.json",
                "schemaVersion", "piecesRequired", "sellerPlace", "leadPercent", "leadBankMax", "pieceText",
                "firstAchievement", "embarkAchievement", "pieces", "id", "unlockRank", "price", "name",
                "gatePlace", "destinationLandmark", "gateSteadyHours", "finalSteadyHours",
                "arrivalId", "arrivalTitle", "arrivalScene", "embarkLabel", "embarkResult", "returnLabel", "returnResult",
                "finalCards", "title", "line", "fraction");
            endScreen = Read<EndScreenFile>("end_screen.json",
                "schemaVersion", "title", "lines", "footer", "statsHeading", "stats", "key", "label", "buttons", "action");
            agents = Read<AgentFile>("agents.json",
                "scoutHire", "scoutWage", "hagglerHire", "hagglerWage",
                "guardHire", "guardWage", "hagglerSpread", "rushCargoLossChance");
            weatherLook = Read<WeatherLookFile>("weather_look.json",
                "schemaVersion", "fadeSeconds", "showMasses",
                "sandstormGrade", "sandstormGradeStrength", "sandstormHaze", "sandstormHazeAlpha",
                "heatGrade", "heatGradeStrength", "heatHaze", "heatHazeAlpha",
                "windGrade", "windGradeStrength", "windHaze", "windHazeAlpha",
                "hazeCenterRatio",
                "massSandstorm", "massSandstormAlpha", "massHeat", "massHeatAlpha", "massWind", "massWindAlpha",
                "massCore", "noiseScale", "noiseStrength", "noiseSeed");
            global::Caravans.Play.WeatherLook.Validate(weatherLook);
            mapTokens = TryRead<MapTokenFile>("map_tokens.json", "schemaVersion", "pixelSize");
            EncounterCards.EnsureLoaded();
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

        static T TryRead<T>(string file, params string[] keys) where T : class
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "data", "config", file));
            if (!File.Exists(path))
                return null;
            return Read<T>(file, keys);
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
        public float merchantSpread;
        public float productionScale;
        public int waterBasePrice;
        public int waterWeight;
        public int waterSpace;
        public int rationBasePrice;
        public int rationWeight;
        public int rationSpace;
        public int tariffBase;
        public int standingCap;
        public int standingPointsPerRank;
        public int standingCoinPerPointBase;
        public int standingCoinPerPointStep;
        public int standingFlipBonus;
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
        public float travelWindowHours;
        public float reserveHours;
        public float townRestHours;
        public float campHours;
        public float rushSpeed;
        public float rushKit;
        public float steadySpeed;
        public float steadyKit;
        public float carefulSpeed;
        public float carefulKit;
        public float dayKit;
    }

    [Serializable]
    public class RivalFile
    {
        public int hopLimit;
    }

    [Serializable]
    public class EncounterFile
    {
        public float baseChance;
        public float carefulOffset;
        public float rushOffset;
        public float floorChance;
        public int fireHourMin;
        public int fireHourMax;
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

    [Serializable]
    public class KitFile
    {
        public int schemaVersion;
        public KitPerson merchant;
        public KitPerson agent;
        public KitTier[] tiers;
        public KitCaravan defaultCaravan;
        public KitUnits units;
        public KitAutoPour autoPour;
        public KitWarning warning;
        public KitDeparture departureCheck;
    }

    [Serializable]
    public class KitPerson
    {
        public float waterPerDay;
        public float rationsPerDay;
        public float kitWater;
        public float kitRations;
        public int mountTier;
    }

    [Serializable]
    public class KitTier
    {
        public int tier;
        public string id;
        public float waterPerDay;
        public float rationsPerDay;
        public float kitWater;
        public float kitRations;
        public int cargoSpace;
        public int cargoWeight;
        public int price;
    }

    [Serializable]
    public class KitCaravan
    {
        public int merchantMountTier;
        public int[] packBeasts;
        public int agents;
    }

    [Serializable]
    public class KitUnits
    {
        public float waterLitresPerUnit;
        public float rationsKgPerUnit;
    }

    [Serializable]
    public class KitAutoPour
    {
        public bool enabled;
    }

    [Serializable]
    public class KitWarning
    {
        public float lowHours;
        public float criticalHours;
    }

    [Serializable]
    public class KitDeparture
    {
        public float amber;
    }

    [Serializable]
    public class SoftResetFile
    {
        public int schemaVersion;
        public float carriedCoinFraction;
        public bool paupersPurse;
        public int pauperFloorCoin;
        public float kitOnWakingDays;
        public string returnTo;
    }

    [Serializable]
    public class RescueFile
    {
        public int schemaVersion;
        public string title;
        public string lead;
        public RescueRegion[] regions;
    }

    [Serializable]
    public class RescueRegion
    {
        public string id;
        public int rescueDays;
        public string[] roads;
        public string[] texts;
    }
}
