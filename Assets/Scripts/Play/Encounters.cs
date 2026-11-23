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
        static readonly Dictionary<string, HashSet<string>> routeTags = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

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
            routeTags.Clear();
            EncounterCards.ForgetLoaded();
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

        public static bool RouteHasTags(PlaceId a, PlaceId b, string[] tags)
        {
            if (tags == null || tags.Length == 0)
                return true;
            EnsureLoaded();
            HashSet<string> found;
            if (!routeTags.TryGetValue(PairKey(a, b), out found))
                return false;
            for (int i = 0; i < tags.Length; i++)
            {
                if (string.IsNullOrEmpty(tags[i]))
                    continue;
                if (!found.Contains(tags[i]))
                    return false;
            }

            return true;
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

            if (rows.Count == 0)
                throw new InvalidOperationException("Encounter_IDs.md has no rows.");
        }

        static void LoadRoutes(string text)
        {
            routes.Clear();
            seaRoads.Clear();
            mountainRoads.Clear();
            routeKeys.Clear();
            routeTags.Clear();
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
                var tagSet = new HashSet<string>(StringComparer.Ordinal);
                string[] parts = tags.Split(',');
                for (int p = 0; p < parts.Length; p++)
                {
                    string tag = parts[p].Trim();
                    if (tag.Length == 0)
                        continue;
                    tagSet.Add(tag);
                    if (tag == "wildlife-sea")
                        seaRoads.Add(key);
                    else if (tag == "wilderness-mountain")
                        mountainRoads.Add(key);
                }

                routeTags[key] = tagSet;
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

    public static class EncounterCards
    {
        static bool loaded;
        static EncounterCardFile file;
        static readonly List<EncounterCard> cards = new List<EncounterCard>();

        public static IReadOnlyList<EncounterCard> All
        {
            get { EnsureLoaded(); return cards; }
        }

        public static void ForgetLoaded()
        {
            loaded = false;
            file = null;
            cards.Clear();
        }

        public static void EnsureLoaded()
        {
            if (loaded)
                return;
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "data", "config", "encounter_cards.json"));
            if (!File.Exists(path))
                throw new FileNotFoundException("Encounter cards were not found.", path);
            LoadText(File.ReadAllText(path));
        }

        public static void LoadText(string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new InvalidOperationException("encounter_cards.json is empty.");
            if (text[0] == '\uFEFF')
                text = text.Substring(1);
            if (text.IndexOf("\"schemaVersion\"", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("encounter_cards.json is missing schemaVersion.");
            if (text.IndexOf("\"cards\"", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("encounter_cards.json is missing cards.");
            if (text.IndexOf("\"agents\"", StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException("encounter_cards.json must not carry an agents effect.");

            EncounterCardFile parsed = JsonUtility.FromJson<EncounterCardFile>(text);
            if (parsed == null)
                throw new InvalidOperationException("encounter_cards.json did not parse.");
            Validate(parsed);
            file = parsed;
            cards.Clear();
            for (int i = 0; i < parsed.cards.Length; i++)
                cards.Add(parsed.cards[i]);
            loaded = true;
        }

        public static EncounterCard Find(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            EnsureLoaded();
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].id == id)
                    return cards[i];
            }

            return null;
        }

        public static bool IsRoadPool(string id)
        {
            EncounterCard card = Find(id);
            return card != null && card.pool == "road";
        }

        public static bool Fits(EncounterCard card, PlaceId a, PlaceId b, float hour, Func<string, bool> hasFlag)
        {
            if (card == null || card.pool != "road")
                return false;
            return Filters(card, a, b, hour, hasFlag, true);
        }

        public static string Pick(IReadOnlyList<EncounterCategory> eligible, PlaceId a, PlaceId b, float hour, Func<string, bool> hasFlag, System.Random random)
        {
            EnsureLoaded();
            if (random == null)
                random = new System.Random();
            var ready = new List<EncounterCategory>();
            if (eligible != null)
            {
                for (int i = 0; i < eligible.Count; i++)
                {
                    if (Matching(eligible[i], a, b, hour, hasFlag).Count > 0)
                        ready.Add(eligible[i]);
                }
            }

            if (ready.Count == 0)
                return null;
            EncounterCategory category = ready[random.Next(ready.Count)];
            List<EncounterCard> found = Matching(category, a, b, hour, hasFlag);
            if (found.Count == 0)
            {
                found = MatchingAny(a, b, hour, hasFlag, category);
                if (found.Count == 0)
                    return null;
            }

            return Weighted(found, random).id;
        }

        public static bool ScriptedReplacement(PlaceId a, PlaceId b, Func<string, bool> hasFlag, out string id)
        {
            EnsureLoaded();
            id = null;
            for (int i = 0; i < cards.Count; i++)
            {
                EncounterCard card = cards[i];
                if (card.pool != "scripted")
                    continue;
                if (string.IsNullOrEmpty(card.requireFlag))
                    continue;
                if (hasFlag == null || !hasFlag(card.requireFlag))
                    continue;
                if (!RoadListed(card.requireRoads, a, b, false))
                    continue;
                id = card.id;
                return true;
            }

            return false;
        }

        public static EncounterOutcome[] Adjust(EncounterCard card, EncounterChoice choice, bool trait, bool skill)
        {
            var outcomes = new EncounterOutcome[choice.outcomes.Length];
            for (int i = 0; i < choice.outcomes.Length; i++)
                outcomes[i] = Clone(choice.outcomes[i]);
            if (trait)
                ApplyMode(outcomes, choice.traitMode, choice.skillShift, choice.traitReplaces, choice.traitOutcome);
            if (skill)
                ApplyMode(outcomes, choice.skillMode, choice.skillShift, null, null);
            return outcomes;
        }

        public static EncounterOutcome Roll(EncounterOutcome[] outcomes, System.Random random)
        {
            if (outcomes == null || outcomes.Length == 0)
                return null;
            if (random == null)
                random = new System.Random();
            int sum = 0;
            for (int i = 0; i < outcomes.Length; i++)
            {
                if (outcomes[i].weight > 0)
                    sum += outcomes[i].weight;
            }

            if (sum <= 0)
                return outcomes[0];
            int roll = random.Next(sum);
            int acc = 0;
            for (int i = 0; i < outcomes.Length; i++)
            {
                if (outcomes[i].weight <= 0)
                    continue;
                acc += outcomes[i].weight;
                if (roll < acc)
                    return outcomes[i];
            }

            return outcomes[outcomes.Length - 1];
        }

        static void ApplyMode(EncounterOutcome[] outcomes, string mode, int shift, string replaces, EncounterOutcome replacement)
        {
            if (string.IsNullOrEmpty(mode) || mode == "none")
                return;
            if (mode == "replace")
            {
                if (replacement == null || string.IsNullOrEmpty(replaces))
                    return;
                for (int i = 0; i < outcomes.Length; i++)
                {
                    if (outcomes[i].key != replaces)
                        continue;
                    EncounterOutcome swapped = Clone(replacement);
                    swapped.weight = outcomes[i].weight;
                    swapped.milder = true;
                    outcomes[i] = swapped;
                }

                return;
            }

            int milder = Milder(outcomes);
            if (milder < 0)
                return;
            if (mode == "guarantee")
            {
                for (int i = 0; i < outcomes.Length; i++)
                    outcomes[i].weight = i == milder ? 100 : 0;
                return;
            }

            if (mode != "shift")
                return;
            int other = milder == 0 ? 1 : 0;
            if (other < 0 || other >= outcomes.Length)
                return;
            int move = shift;
            if (move > outcomes[other].weight)
                move = outcomes[other].weight;
            if (outcomes[milder].weight + move > 100)
                move = 100 - outcomes[milder].weight;
            if (move < 0)
                move = 0;
            outcomes[milder].weight += move;
            outcomes[other].weight -= move;
        }

        static int Milder(EncounterOutcome[] outcomes)
        {
            for (int i = 0; i < outcomes.Length; i++)
            {
                if (outcomes[i].milder)
                    return i;
            }

            return -1;
        }

        static EncounterOutcome Clone(EncounterOutcome source)
        {
            return new EncounterOutcome
            {
                key = source.key,
                weight = source.weight,
                tone = source.tone,
                milder = source.milder,
                result = source.result,
                effects = source.effects
            };
        }

        static List<EncounterCard> Matching(EncounterCategory category, PlaceId a, PlaceId b, float hour, Func<string, bool> hasFlag)
        {
            var found = new List<EncounterCard>();
            for (int i = 0; i < cards.Count; i++)
            {
                EncounterCard card = cards[i];
                if (card.pool != "road")
                    continue;
                if (CategoryOf(card) != category)
                    continue;
                if (!Filters(card, a, b, hour, hasFlag, true))
                    continue;
                found.Add(card);
            }

            return found;
        }

        static List<EncounterCard> MatchingAny(PlaceId a, PlaceId b, float hour, Func<string, bool> hasFlag, EncounterCategory skip)
        {
            var found = new List<EncounterCard>();
            for (int i = 0; i < cards.Count; i++)
            {
                EncounterCard card = cards[i];
                if (card.pool != "road" || CategoryOf(card) == skip)
                    continue;
                if (Filters(card, a, b, hour, hasFlag, true))
                    found.Add(card);
            }

            return found;
        }

        static bool Filters(EncounterCard card, PlaceId a, PlaceId b, float hour, Func<string, bool> hasFlag, bool emptyRoadMeansAll)
        {
            if (!WhenMatches(card.when, hour))
                return false;
            if (!RoadListed(card.requireRoads, a, b, emptyRoadMeansAll))
                return false;
            if (!CityFits(card, a, b))
                return false;
            if (!PlacesListed(card.requirePlaces, a, b))
                return false;
            if (!EncounterTable.RouteHasTags(a, b, card.requireRoadTags))
                return false;
            if (!string.IsNullOrEmpty(card.requireFlag) && (hasFlag == null || !hasFlag(card.requireFlag)))
                return false;
            if (!string.IsNullOrEmpty(card.blockFlag) && hasFlag != null && hasFlag(card.blockFlag))
                return false;
            return true;
        }

        static bool WhenMatches(string when, float hour)
        {
            if (string.IsNullOrEmpty(when) || when == "either")
                return true;
            bool day = TravelScale.IsDayHour(hour);
            if (when == "day")
                return day;
            if (when == "night")
                return !day;
            return false;
        }

        static bool RoadListed(string[] roads, PlaceId a, PlaceId b, bool emptyMeansAll)
        {
            if (roads == null || roads.Length == 0)
                return emptyMeansAll;
            for (int i = 0; i < roads.Length; i++)
            {
                PlaceId left;
                PlaceId right;
                if (!TryRoad(roads[i], out left, out right))
                    continue;
                if ((left == a && right == b) || (left == b && right == a))
                    return true;
            }

            return false;
        }

        static bool CityFits(EncounterCard card, PlaceId a, PlaceId b)
        {
            PlaceId city;
            switch (card.category)
            {
                case "CityKharun":
                    city = PlaceId.Kharun;
                    break;
                case "CityGhorath":
                    city = PlaceId.Ghorath;
                    break;
                case "CityVeythar":
                    city = PlaceId.Veythar;
                    break;
                case "CityZamath":
                    city = PlaceId.Zamath;
                    break;
                case "CityThalor":
                    city = PlaceId.Thalor;
                    break;
                default:
                    return true;
            }

            return a == city || b == city;
        }

        static bool PlacesListed(string[] places, PlaceId a, PlaceId b)
        {
            if (places == null || places.Length == 0)
                return true;
            for (int i = 0; i < places.Length; i++)
            {
                PlaceId place;
                if (!Enum.TryParse(places[i], false, out place))
                    return false;
                if (place != a && place != b)
                    return false;
            }

            return true;
        }

        static EncounterCard Weighted(List<EncounterCard> found, System.Random random)
        {
            float sum = 0f;
            for (int i = 0; i < found.Count; i++)
                sum += WeightOf(found[i]);
            double roll = random.NextDouble() * sum;
            float acc = 0f;
            for (int i = 0; i < found.Count; i++)
            {
                acc += WeightOf(found[i]);
                if (roll < acc)
                    return found[i];
            }

            return found[found.Count - 1];
        }

        static float WeightOf(EncounterCard card)
        {
            if (card.drawWeight <= 0f)
                return 1f;
            return card.drawWeight;
        }

        public static EncounterCategory CategoryOf(EncounterCard card)
        {
            EncounterCategory category;
            if (card != null && Enum.TryParse(card.category, false, out category))
                return category;
            throw new InvalidOperationException("Encounter card has no category.");
        }

        static bool TryRoad(string road, out PlaceId a, out PlaceId b)
        {
            a = PlaceId.Kharun;
            b = PlaceId.Kharun;
            if (string.IsNullOrEmpty(road))
                return false;
            int dash = road.IndexOf('-');
            if (dash <= 0 || dash >= road.Length - 1)
                return false;
            return Enum.TryParse(road.Substring(0, dash), false, out a)
                && Enum.TryParse(road.Substring(dash + 1), false, out b);
        }

        static void Validate(EncounterCardFile parsed)
        {
            if (parsed.schemaVersion != 3)
                throw new InvalidOperationException("encounter_cards.json schemaVersion must be 3.");
            if (parsed.cards == null || parsed.cards.Length != 16)
                throw new InvalidOperationException("encounter_cards.json needs 16 cards.");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < parsed.cards.Length; i++)
            {
                EncounterCard card = parsed.cards[i];
                if (card == null || string.IsNullOrEmpty(card.id))
                    throw new InvalidOperationException("encounter_cards.json has an empty card.");
                if (!seen.Add(card.id))
                    throw new InvalidOperationException("encounter_cards.json repeats " + card.id);
                CategoryOf(card);
                if (card.pool != "road" && card.pool != "scripted" && card.pool != "expedition" && card.pool != "deferred" && card.pool != "retired")
                    throw new InvalidOperationException(card.id + " has an unknown pool.");
                if (card.when != "day" && card.when != "night" && card.when != "either")
                    throw new InvalidOperationException(card.id + " has no day, night, or either.");
                CheckSkill(card.id, card.skillTag);
                CheckTrait(card.id, card.traitTag);
                CheckRoads(card.id, card.requireRoads);
                CheckPlaces(card.id, card.requirePlaces);
                if (card.choices == null || card.choices.Length != 2)
                    throw new InvalidOperationException(card.id + " needs two choices.");
                for (int c = 0; c < card.choices.Length; c++)
                {
                    EncounterChoice choice = card.choices[c];
                    if (choice == null || choice.outcomes == null || choice.outcomes.Length != 2)
                        throw new InvalidOperationException(card.id + " needs two outcomes on each choice.");
                    int sum = choice.outcomes[0].weight + choice.outcomes[1].weight;
                    if (sum != 100)
                        throw new InvalidOperationException(card.id + " choice weights sum to " + sum + ".");
                    CheckMode(card.id, choice.skillMode);
                    CheckMode(card.id, choice.traitMode);
                }
            }

            if (parsed.zones != null)
            {
                for (int i = 0; i < parsed.zones.Length; i++)
                {
                    if (parsed.zones[i] != null)
                        CheckRoads(parsed.zones[i].id, parsed.zones[i].roads);
                }
            }
        }

        static void CheckSkill(string id, string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return;
            if (tag == "Courier")
                throw new InvalidOperationException(id + " rejects the Courier skill.");
            if (tag != "Guard" && tag != "Scout" && tag != "Handler" && tag != "Haggler" && tag != "Gossip")
                throw new InvalidOperationException(id + " has an unknown skill " + tag + ".");
        }

        static void CheckTrait(string id, string tag)
        {
            if (string.IsNullOrEmpty(tag) || tag == "FriendOfBandits")
                return;
            throw new InvalidOperationException(id + " has an unknown trait " + tag + ".");
        }

        static void CheckMode(string id, string mode)
        {
            if (string.IsNullOrEmpty(mode) || mode == "none" || mode == "shift" || mode == "guarantee" || mode == "replace")
                return;
            throw new InvalidOperationException(id + " has an unknown adjust mode " + mode + ".");
        }

        static void CheckRoads(string id, string[] roads)
        {
            if (roads == null)
                return;
            for (int i = 0; i < roads.Length; i++)
            {
                PlaceId a;
                PlaceId b;
                if (!TryRoad(roads[i], out a, out b))
                    throw new InvalidOperationException(id + " has a road that is not two places: " + roads[i]);
            }
        }

        static void CheckPlaces(string id, string[] places)
        {
            if (places == null)
                return;
            for (int i = 0; i < places.Length; i++)
            {
                PlaceId place;
                if (!Enum.TryParse(places[i], false, out place))
                    throw new InvalidOperationException(id + " has an unknown place " + places[i] + ".");
            }
        }
    }

    [Serializable]
    public class EncounterCardFile
    {
        public int schemaVersion;
        public EncounterFlagRow[] flags;
        public EncounterZoneRow[] zones;
        public EncounterCard[] cards;
    }

    [Serializable]
    public class EncounterFlagRow
    {
        public string id;
        public string scope;
        public string note;
    }

    [Serializable]
    public class EncounterZoneRow
    {
        public string id;
        public string[] roads;
        public string note;
    }

    [Serializable]
    public class EncounterCard
    {
        public string id;
        public string category;
        public string pool;
        public float drawWeight;
        public string when;
        public string skillTag;
        public string traitTag;
        public string[] requireRoads;
        public string[] requirePlaces;
        public string[] requireRoadTags;
        public string requireFlag;
        public string blockFlag;
        public string trigger;
        public string title;
        public string scene;
        public EncounterChoice[] choices;
        public string notes;
    }

    [Serializable]
    public class EncounterChoice
    {
        public string label;
        public string skillMode;
        public int skillShift;
        public string traitMode;
        public string traitReplaces;
        public EncounterOutcome traitOutcome;
        public EncounterOutcome[] outcomes;
    }

    [Serializable]
    public class EncounterOutcome
    {
        public string key;
        public int weight;
        public string tone;
        public bool milder;
        public string result;
        public EncounterEffects effects;
    }

    [Serializable]
    public class EncounterEffects
    {
        public int coin;
        public float waterLitres;
        public float rationKg;
        public float delayHours;
        public float shortcutHours;
        public bool camp;
        public bool fillKit;
        public EncounterCargoOp[] cargo;
        public int rumourStars;
        public int kharunPoints;
        public string[] setFlags;
        public string[] clearFlags;
    }

    [Serializable]
    public class EncounterCargoOp
    {
        public string select;
        public string good;
        public int units;
        public float integrityHit;
        public int fallbackCoin;
    }
}
