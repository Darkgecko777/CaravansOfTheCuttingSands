using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Caravans.Play
{
    public enum EncounterCategory
    {
        WildlifeDesert,
        WildlifeSea,
        WildlifeMountain,
        Bandits,
        CityKharun,
        CityGhorath,
        CityVeythar,
        CityZamath,
        CityThalor,
        RivalHouses,
        WeatherClear,
        WeatherHeat,
        WeatherWind,
        WeatherSandstorm,
        AncientMysteries,
        Catchall
    }

    public enum DayNight
    {
        Day,
        Night,
        Either
    }

    public sealed class EncounterRow
    {
        public string Id;
        public EncounterCategory Category;
        public DayNight When;
    }

    public struct TaggedRoute
    {
        public PlaceId A;
        public PlaceId B;
    }

    // Ids are read from Encounter_IDs.md. Sea and mountain roads are read from Route_Tags.md.
    // Desert, bandits, catchall, and a city at either end are rules, not a column the draw trusts.
    public static class EncounterTable
    {
        const string Dash = " \u2014 ";

        static bool loaded;
        static readonly List<EncounterRow> rows = new List<EncounterRow>();
        static readonly List<EncounterRow>[] byCategory = new List<EncounterRow>[16];
        static readonly List<TaggedRoute> routes = new List<TaggedRoute>();
        static readonly HashSet<string> seaRoads = new HashSet<string>(StringComparer.Ordinal);
        static readonly HashSet<string> mountainRoads = new HashSet<string>(StringComparer.Ordinal);
        static readonly HashSet<string> routeKeys = new HashSet<string>(StringComparer.Ordinal);

        public static IReadOnlyList<EncounterRow> All
        {
            get { EnsureLoaded(); return rows; }
        }

        public static IReadOnlyList<TaggedRoute> Routes
        {
            get { EnsureLoaded(); return routes; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ForgetLoaded()
        {
            loaded = false;
            rows.Clear();
            routes.Clear();
            seaRoads.Clear();
            mountainRoads.Clear();
            routeKeys.Clear();
            for (int i = 0; i < byCategory.Length; i++)
            {
                if (byCategory[i] != null)
                    byCategory[i].Clear();
            }
        }

        public static void EnsureLoaded()
        {
            if (loaded)
                return;
            LoadIds(ReadProjectFile("Encounter_IDs.md"));
            LoadRoutes(ReadProjectFile("Route_Tags.md"));
            loaded = true;
        }

        public static IReadOnlyList<EncounterRow> Rows(EncounterCategory category)
        {
            EnsureLoaded();
            return byCategory[(int)category];
        }

        public static float Chance(RoadPace pace)
        {
            float chance = GameConfig.Encounters.baseChance;
            if (pace == RoadPace.Careful)
                chance += GameConfig.Encounters.carefulOffset;
            else if (pace == RoadPace.Rush)
                chance += GameConfig.Encounters.rushOffset;
            float floor = GameConfig.Encounters.floorChance;
            if (chance < floor)
                chance = floor;
            if (chance > 1f)
                chance = 1f;
            return chance;
        }

        // Rival houses stay out until a later pass can tell that this leg crossed a caravan of a house.
        // Ancient mysteries are expedition-only.
        public static List<EncounterCategory> Eligible(PlaceId a, PlaceId b, WeatherKind weather)
        {
            EnsureLoaded();
            var pool = new List<EncounterCategory>();
            pool.Add(EncounterCategory.WildlifeDesert);
            pool.Add(EncounterCategory.Bandits);
            pool.Add(EncounterCategory.Catchall);
            pool.Add(WeatherCategory(weather));
            PlaceId first = a;
            PlaceId second = b;
            if ((int)first > (int)second)
            {
                PlaceId swap = first;
                first = second;
                second = swap;
            }

            AddCity(pool, first);
            AddCity(pool, second);
            string key = PairKey(a, b);
            if (seaRoads.Contains(key))
                pool.Add(EncounterCategory.WildlifeSea);
            if (mountainRoads.Contains(key))
                pool.Add(EncounterCategory.WildlifeMountain);
            return pool;
        }

        public static bool HasRoute(PlaceId a, PlaceId b)
        {
            EnsureLoaded();
            return routeKeys.Contains(PairKey(a, b));
        }

        public static EncounterRow Pick(IReadOnlyList<EncounterCategory> categories, int categoryIndex, int rowIndex)
        {
            if (categories == null || categories.Count == 0)
                throw new InvalidOperationException("Encounter pool is empty.");
            EncounterCategory category = categories[Positive(categoryIndex, categories.Count)];
            IReadOnlyList<EncounterRow> found = Rows(category);
            if (found == null || found.Count == 0)
                throw new InvalidOperationException("Encounter category has no rows: " + category);
            return found[Positive(rowIndex, found.Count)];
        }

        static int Positive(int index, int count)
        {
            int mod = index % count;
            return mod < 0 ? mod + count : mod;
        }

        static void AddCity(List<EncounterCategory> pool, PlaceId place)
        {
            if (!EconomyCatalog.IsCity(place))
                return;
            switch (EconomyCatalog.CityHouse(place))
            {
                case HouseId.Kharun:
                    pool.Add(EncounterCategory.CityKharun);
                    break;
                case HouseId.Zamath:
                    pool.Add(EncounterCategory.CityZamath);
                    break;
                case HouseId.Thalor:
                    pool.Add(EncounterCategory.CityThalor);
                    break;
                case HouseId.Veythar:
                    pool.Add(EncounterCategory.CityVeythar);
                    break;
                default:
                    pool.Add(EncounterCategory.CityGhorath);
                    break;
            }
        }

        static EncounterCategory WeatherCategory(WeatherKind weather)
        {
            switch (weather)
            {
                case WeatherKind.Heat:
                    return EncounterCategory.WeatherHeat;
                case WeatherKind.Wind:
                    return EncounterCategory.WeatherWind;
                case WeatherKind.Sandstorm:
                    return EncounterCategory.WeatherSandstorm;
                default:
                    return EncounterCategory.WeatherClear;
            }
        }

        static string PairKey(PlaceId a, PlaceId b)
        {
            int left = (int)a;
            int right = (int)b;
            if (left > right)
            {
                int swap = left;
                left = right;
                right = swap;
            }

            return left + ":" + right;
        }

        static void LoadIds(string text)
        {
            rows.Clear();
            for (int i = 0; i < byCategory.Length; i++)
                byCategory[i] = new List<EncounterRow>();
            EncounterCategory category = (EncounterCategory)(-1);
            bool inCategory = false;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            string[] lines = text.Split(new[] { '\n' }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim().TrimStart('\uFEFF');
                if (line.StartsWith("## ", StringComparison.Ordinal))
                {
                    category = ParseCategory(line.Substring(3).Trim());
                    inCategory = true;
                    continue;
                }

                if (!inCategory || !line.StartsWith("|", StringComparison.Ordinal))
                    continue;
                if (line.IndexOf("---", StringComparison.Ordinal) >= 0)
                    continue;
                if (line.IndexOf("ID", StringComparison.Ordinal) >= 0 && line.IndexOf("Day/Night", StringComparison.Ordinal) >= 0)
                    continue;
                string[] cells = line.Split('|');
                if (cells.Length < 4)
                    throw new InvalidOperationException("Encounter_IDs.md has a short row: " + line);
                string id = cells[1].Trim();
                string when = cells[2].Trim();
                if (id.Length == 0)
                    continue;
                if (!seen.Add(id))
                    throw new InvalidOperationException("Encounter_IDs.md repeats " + id);
                var row = new EncounterRow
                {
                    Id = id,
                    Category = category,
                    When = ParseWhen(when, id)
                };
                rows.Add(row);
                byCategory[(int)category].Add(row);
            }

            if (rows.Count != 80)
                throw new InvalidOperationException("Encounter_IDs.md has " + rows.Count + " rows.");
            for (int i = 0; i < byCategory.Length; i++)
            {
                if (byCategory[i].Count != 5)
                    throw new InvalidOperationException("Encounter category " + (EncounterCategory)i + " has " + byCategory[i].Count + " rows.");
            }
        }

        static void LoadRoutes(string text)
        {
            routes.Clear();
            seaRoads.Clear();
            mountainRoads.Clear();
            routeKeys.Clear();
            string[] lines = text.Split(new[] { '\n' }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim().TrimStart('\uFEFF');
                if (!line.StartsWith("|", StringComparison.Ordinal))
                    continue;
                if (line.IndexOf("---", StringComparison.Ordinal) >= 0)
                    continue;
                string[] cells = line.Split('|');
                if (cells.Length < 3)
                    continue;
                string route = cells[1].Trim();
                string tags = cells[2].Trim();
                if (route.StartsWith("Route", StringComparison.Ordinal))
                    continue;
                int dash = route.IndexOf(Dash, StringComparison.Ordinal);
                if (dash < 0)
                    throw new InvalidOperationException("Route_Tags.md has no road dash: " + route);
                PlaceId a = EconomyCatalog.ParsePlace(route.Substring(0, dash).Trim());
                PlaceId b = EconomyCatalog.ParsePlace(route.Substring(dash + Dash.Length).Trim());
                string key = PairKey(a, b);
                if (!routeKeys.Add(key))
                    throw new InvalidOperationException("Route_Tags.md repeats a road: " + route);
                routes.Add(new TaggedRoute { A = a, B = b });
                string[] parts = tags.Split(',');
                for (int p = 0; p < parts.Length; p++)
                {
                    string tag = parts[p].Trim();
                    if (tag == "wildlife-sea")
                        seaRoads.Add(key);
                    else if (tag == "wilderness-mountain")
                        mountainRoads.Add(key);
                }
            }

            if (routes.Count == 0)
                throw new InvalidOperationException("Route_Tags.md has no roads.");
        }

        static EncounterCategory ParseCategory(string heading)
        {
            switch (heading)
            {
                case "Wildlife-desert":
                    return EncounterCategory.WildlifeDesert;
                case "Wildlife-sea":
                    return EncounterCategory.WildlifeSea;
                case "Wildlife-mountain":
                    return EncounterCategory.WildlifeMountain;
                case "Bandits":
                    return EncounterCategory.Bandits;
                case "City \u2014 Khar\u00fbn":
                    return EncounterCategory.CityKharun;
                case "City \u2014 Ghorath":
                    return EncounterCategory.CityGhorath;
                case "City \u2014 Veythar":
                    return EncounterCategory.CityVeythar;
                case "City \u2014 Zamath":
                    return EncounterCategory.CityZamath;
                case "City \u2014 Thalor":
                    return EncounterCategory.CityThalor;
                case "Rival houses":
                    return EncounterCategory.RivalHouses;
                case "Weather \u2014 Clear":
                    return EncounterCategory.WeatherClear;
                case "Weather \u2014 Heat":
                    return EncounterCategory.WeatherHeat;
                case "Weather \u2014 Wind":
                    return EncounterCategory.WeatherWind;
                case "Weather \u2014 Sandstorm":
                    return EncounterCategory.WeatherSandstorm;
                case "Ancient mysteries (expedition-only)":
                    return EncounterCategory.AncientMysteries;
                case "Catchall":
                    return EncounterCategory.Catchall;
                default:
                    throw new InvalidOperationException("Encounter_IDs.md has an unknown category: " + heading);
            }
        }

        static DayNight ParseWhen(string when, string id)
        {
            switch (when)
            {
                case "day":
                    return DayNight.Day;
                case "night":
                    return DayNight.Night;
                case "either":
                    return DayNight.Either;
                default:
                    throw new InvalidOperationException("Encounter_IDs.md row " + id + " has no day, night, or either.");
            }
        }

        static string ReadProjectFile(string file)
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", file));
            if (!File.Exists(path))
                throw new FileNotFoundException("Encounter chart was not found.", path);
            return File.ReadAllText(path);
        }
    }
}
