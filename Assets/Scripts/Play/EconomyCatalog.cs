using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Caravans.Play
{
    public static class EconomyCatalog
    {
        static readonly string[] PlaceLabels =
        {
            "Khar\u00fbn", "Draven", "Zamath", "Thalor", "Veythar", "Ghorath",
            "Ashar", "Moraq", "Torvern", "Sorel", "Kethra", "Neth", "Rukh", "Gh\u00fbl",
            "Westmark", "Kaleth", "Southmark", "Highmark", "Brineford", "Ridgewatch"
        };

        static readonly string[] GoodLabels =
        {
            "Shimmersteel Jars", "Finger Fungus", "Water", "Rations",
            "Shimmersteel", "Shimmersteel Cookware", "Shimmersteel Fineware", "Frost Iron",
            "Titan's Heart-rock", "Palestone Bricks", "Living Lumber", "Hulv Bone",
            "Water Witching Rods", "Khor Crafted Tools", "Vek-kilned Pottery", "Zar'Kun Sinew",
            "Scrubcast Resin", "Brine Scrub", "Mor Root", "Giant Duzhu Hide",
            "Pincer Beast Fur", "Clack Fiend Molt-skin", "Tiger Cactus Flesh", "Kel Eel Flesh",
            "Clack Fiend Larvae", "Moon Soaked Zethi-Bulb", "Fire Salt", "Sweetrock",
            "Sweet Bliss Pomegranates", "Sunburnt Brew", "Crimson Eyed Melon Juice", "Dream Lotus Nectar",
            "Sarn's Bane Liver", "Numbspindle", "Dune Mollusk Pearls", "Eyes of Arkhul",
            "Terrorshriek Talons", "Kelkris Feathers", "Reth's Blood", "Ishka Crafted Music Makers"
        };

        static readonly Dictionary<string, PlaceId> PlaceByName = BuildPlaces();
        static readonly Dictionary<string, GoodId> GoodByName = BuildGoods();

        static bool loaded;
        static int[] basePrice = new int[WorldIds.GoodCount];
        static int[] weight = new int[WorldIds.GoodCount];
        static int[] space = new int[WorldIds.GoodCount];
        static float[,] eat = new float[WorldIds.PlaceCount, WorldIds.GoodCount];
        static bool[,] producer = new bool[WorldIds.PlaceCount, WorldIds.GoodCount];
        static int[,] hops = new int[WorldIds.PlaceCount, WorldIds.GoodCount];
        static int[,] cap = new int[WorldIds.PlaceCount, WorldIds.GoodCount];
        static float[] worldEat = new float[WorldIds.GoodCount];
        static float[] desired = new float[WorldIds.GoodCount];

        public static void EnsureLoaded()
        {
            if (loaded)
                return;
            Load();
            loaded = true;
        }

        public static string PlaceName(PlaceId place)
        {
            return PlaceLabels[(int)place];
        }

        public static string GoodName(GoodId good)
        {
            return GoodLabels[(int)good];
        }

        public static string HouseName(HouseId house)
        {
            switch (house)
            {
                case HouseId.Kharun:
                    return PlaceLabels[(int)PlaceId.Kharun];
                case HouseId.Zamath:
                    return "Zamath";
                case HouseId.Thalor:
                    return "Thalor";
                case HouseId.Veythar:
                    return "Veythar";
                default:
                    return "Ghorath";
            }
        }

        public static PlaceKind KindOf(PlaceId place)
        {
            switch (place)
            {
                case PlaceId.Kharun:
                case PlaceId.Zamath:
                case PlaceId.Thalor:
                case PlaceId.Veythar:
                case PlaceId.Ghorath:
                    return PlaceKind.City;
                case PlaceId.Westmark:
                case PlaceId.Kaleth:
                case PlaceId.Southmark:
                case PlaceId.Highmark:
                case PlaceId.Brineford:
                case PlaceId.Ridgewatch:
                    return PlaceKind.Post;
                default:
                    return PlaceKind.Village;
            }
        }

        public static bool IsCity(PlaceId place)
        {
            return KindOf(place) == PlaceKind.City;
        }

        public static HouseId CityHouse(PlaceId place)
        {
            switch (place)
            {
                case PlaceId.Kharun:
                    return HouseId.Kharun;
                case PlaceId.Zamath:
                    return HouseId.Zamath;
                case PlaceId.Thalor:
                    return HouseId.Thalor;
                case PlaceId.Veythar:
                    return HouseId.Veythar;
                default:
                    return HouseId.Ghorath;
            }
        }

        public static int BasePrice(GoodId good)
        {
            EnsureLoaded();
            return basePrice[(int)good];
        }

        public static int Weight(GoodId good)
        {
            EnsureLoaded();
            return weight[(int)good];
        }

        public static int Space(GoodId good)
        {
            EnsureLoaded();
            return space[(int)good];
        }

        public static float Eat(PlaceId place, GoodId good)
        {
            EnsureLoaded();
            return eat[(int)place, (int)good];
        }

        public static float WorldEat(GoodId good)
        {
            EnsureLoaded();
            return worldEat[(int)good];
        }

        public static float DayUnit(PlaceId place, GoodId good)
        {
            float local = Eat(place, good);
            return local > 0f ? local : 1f;
        }

        public static float Desired(GoodId good)
        {
            EnsureLoaded();
            return desired[(int)good];
        }

        public static bool IsProducer(PlaceId place, GoodId good)
        {
            EnsureLoaded();
            return producer[(int)place, (int)good];
        }

        public static int HopsFromProducer(PlaceId place, GoodId good)
        {
            EnsureLoaded();
            return hops[(int)place, (int)good];
        }

        public static int StartingStock(PlaceId place, GoodId good)
        {
            int distance = HopsFromProducer(place, good);
            int span = EconomyRates.FullCoverDays - distance;
            if (span <= 0)
                return 0;
            return Mathf.FloorToInt(DayUnit(place, good) * span);
        }

        public static int Cap(PlaceId place, GoodId good)
        {
            EnsureLoaded();
            return cap[(int)place, (int)good];
        }

        public static float DecayOf(GoodId good)
        {
            if (good == GoodId.SweetBliss)
                return EconomyRates.SweetBlissDecay;
            if (IsOrdinaryFood(good))
                return EconomyRates.OrdinaryDecay;
            return 0f;
        }

        public static bool IsContraband(PlaceId place, GoodId good)
        {
            if (good == GoodId.DreamLotusNectar)
                return true;
            if (good == GoodId.CrimsonEyedMelonJuice && (place == PlaceId.Veythar || place == PlaceId.Thalor))
                return true;
            if (good == GoodId.SweetBliss && IsCity(place))
                return true;
            if (good == GoodId.EyesOfArkhul && place == PlaceId.Kharun)
                return true;
            return false;
        }

        public static float CoverMultiplier(float cover)
        {
            if (cover <= 0f)
                return EconomyRates.EmptyMultiplier;
            if (cover >= EconomyRates.FullCoverDays)
                return EconomyRates.FullMultiplier;
            if (cover <= EconomyRates.OrdinaryCoverDays)
            {
                float t = cover / EconomyRates.OrdinaryCoverDays;
                return Mathf.Lerp(EconomyRates.EmptyMultiplier, 1f, t);
            }

            float span = (cover - EconomyRates.OrdinaryCoverDays) / (EconomyRates.FullCoverDays - EconomyRates.OrdinaryCoverDays);
            return Mathf.Lerp(1f, EconomyRates.FullMultiplier, span);
        }

        public static int ShelfPrice(int price, int stock, float dayUnit)
        {
            float unit = dayUnit > 0f ? dayUnit : 1f;
            float cover = stock <= 0 ? 0f : stock / unit;
            return Mathf.Max(1, Mathf.RoundToInt(price * CoverMultiplier(cover)));
        }

        public static bool IsRecipeOutput(GoodId good)
        {
            for (int i = 0; i < EconomyRates.Recipes.Length; i++)
            {
                if (EconomyRates.Recipes[i].Output == good)
                    return true;
            }

            return false;
        }

        public static int Affordable(Recipe recipe, Func<GoodId, int> stockOf)
        {
            if (recipe.CountA <= 0)
                return 0;
            int makes = stockOf(recipe.InputA) / recipe.CountA;
            if (recipe.HasSecond)
                makes = Mathf.Min(makes, stockOf(recipe.InputB) / recipe.CountB);
            return Mathf.Max(0, makes);
        }

        static bool IsOrdinaryFood(GoodId good)
        {
            switch (good)
            {
                case GoodId.TigerCactusFlesh:
                case GoodId.MoonSoakedZethiBulb:
                case GoodId.FingerFungus:
                case GoodId.FireSalt:
                case GoodId.Sweetrock:
                case GoodId.ClackFiendLarvae:
                case GoodId.KelEelFlesh:
                case GoodId.SunburntBrew:
                case GoodId.CrimsonEyedMelonJuice:
                case GoodId.Rations:
                    return true;
                default:
                    return false;
            }
        }

        static void Load()
        {
            if (PlaceLabels.Length != WorldIds.PlaceCount || GoodLabels.Length != WorldIds.GoodCount)
                throw new InvalidOperationException("Place or good labels do not match the id lists.");
            basePrice = new int[WorldIds.GoodCount];
            weight = new int[WorldIds.GoodCount];
            space = new int[WorldIds.GoodCount];
            eat = new float[WorldIds.PlaceCount, WorldIds.GoodCount];
            producer = new bool[WorldIds.PlaceCount, WorldIds.GoodCount];
            ReadWeight(DocsFile("Economy_Weight_Price.csv"));
            ReadEat(DocsFile("Economy_Eat_Per_Day.csv"));
            basePrice[(int)GoodId.Water] = EconomyRates.WaterBasePrice;
            weight[(int)GoodId.Water] = EconomyRates.WaterWeight;
            space[(int)GoodId.Water] = EconomyRates.WaterSpace;
            basePrice[(int)GoodId.Rations] = EconomyRates.RationBasePrice;
            weight[(int)GoodId.Rations] = EconomyRates.RationWeight;
            space[(int)GoodId.Rations] = EconomyRates.RationSpace;
            for (int place = 0; place < WorldIds.PlaceCount; place++)
                eat[place, (int)GoodId.Rations] = EconomyRates.RationEat;

            for (int good = 0; good < WorldIds.GoodCount; good++)
            {
                if (basePrice[good] <= 0 || weight[good] <= 0 || space[good] <= 0)
                    throw new InvalidOperationException("Missing pack data for " + GoodLabels[good]);
            }

            MarkProducers();
            var makersOf = new int[WorldIds.GoodCount];
            for (int good = 0; good < WorldIds.GoodCount; good++)
            {
                float sum = 0f;
                int makers = 0;
                for (int place = 0; place < WorldIds.PlaceCount; place++)
                {
                    sum += eat[place, good];
                    if (producer[place, good])
                        makers += 1;
                }

                if (sum <= 0f)
                    throw new InvalidOperationException("Nothing eats " + GoodLabels[good]);
                if (makers <= 0)
                    throw new InvalidOperationException("Nothing produces " + GoodLabels[good]);
                worldEat[good] = sum;
                makersOf[good] = makers;
            }

            for (int good = 0; good < WorldIds.GoodCount; good++)
            {
                float total = (worldEat[good] + RecipeDraw((GoodId)good)) * EconomyRates.ProductionScale;
                desired[good] = makersOf[good] <= 1 ? total : total / makersOf[good];
            }

            hops = new int[WorldIds.PlaceCount, WorldIds.GoodCount];
            cap = new int[WorldIds.PlaceCount, WorldIds.GoodCount];
            for (int place = 0; place < WorldIds.PlaceCount; place++)
            {
                for (int good = 0; good < WorldIds.GoodCount; good++)
                {
                    hops[place, good] = NearestProducer((PlaceId)place, (GoodId)good);
                    float day = eat[place, good] > 0f ? eat[place, good] : 1f;
                    int coverCap = Mathf.Max(1, Mathf.CeilToInt(EconomyRates.FullCoverDays * day));
                    int makeCap = producer[place, good] ? Mathf.CeilToInt(desired[good]) : 0;
                    cap[place, good] = Mathf.Max(coverCap, makeCap);
                }
            }
        }

        static float RecipeDraw(GoodId good)
        {
            if (good == GoodId.Shimmersteel)
            {
                return 2f * worldEat[(int)GoodId.ShimmersteelJars]
                    + worldEat[(int)GoodId.ShimmersteelCookware]
                    + worldEat[(int)GoodId.ShimmersteelFineware];
            }

            if (good == GoodId.TitansHeartRock)
                return worldEat[(int)GoodId.ShimmersteelCookware];
            if (good == GoodId.LivingLumber)
                return worldEat[(int)GoodId.WaterWitchingRods];
            if (good == GoodId.Numbspindle)
                return worldEat[(int)GoodId.HulvBone];
            return 0f;
        }

        static int NearestProducer(PlaceId place, GoodId good)
        {
            int best = 99;
            int goodIndex = (int)good;
            for (int i = 0; i < WorldIds.PlaceCount; i++)
            {
                if (!producer[i, goodIndex])
                    continue;
                int distance = RoadGraph.Hops(place, (PlaceId)i);
                if (distance < best)
                    best = distance;
            }

            return best;
        }

        static void MarkProducers()
        {
            Produces(GoodId.ShimmersteelJars, PlaceId.Kharun);
            Produces(GoodId.TigerCactusFlesh, PlaceId.Kharun);
            Produces(GoodId.KhorCraftedTools, PlaceId.Kharun);
            Produces(GoodId.ScrubcastResin, PlaceId.Kharun);
            Produces(GoodId.DreamLotusNectar, PlaceId.Ghorath);
            Produces(GoodId.CrimsonEyedMelonJuice, PlaceId.Ghorath);
            Produces(GoodId.Numbspindle, PlaceId.Ghorath);
            Produces(GoodId.MorRoot, PlaceId.Ghorath);
            Produces(GoodId.MoonSoakedZethiBulb, PlaceId.Veythar);
            Produces(GoodId.TitansHeartRock, PlaceId.Veythar);
            Produces(GoodId.ShimmersteelFineware, PlaceId.Veythar);
            Produces(GoodId.VekKilnedPottery, PlaceId.Veythar);
            Produces(GoodId.Shimmersteel, PlaceId.Thalor);
            Produces(GoodId.ShimmersteelCookware, PlaceId.Thalor);
            Produces(GoodId.GiantDuzhuHide, PlaceId.Thalor);
            Produces(GoodId.IshkaMusicMakers, PlaceId.Thalor);
            Produces(GoodId.LivingLumber, PlaceId.Zamath);
            Produces(GoodId.WaterWitchingRods, PlaceId.Zamath);
            Produces(GoodId.SweetBliss, PlaceId.Zamath);
            Produces(GoodId.BrineScrub, PlaceId.Zamath);
            Produces(GoodId.PalestoneBricks, PlaceId.Ashar);
            Produces(GoodId.TerrorshriekTalons, PlaceId.Ashar);
            Produces(GoodId.DuneMolluskPearls, PlaceId.Moraq);
            Produces(GoodId.SunburntBrew, PlaceId.Moraq);
            Produces(GoodId.KelkrisFeathers, PlaceId.Torvern);
            Produces(GoodId.PincerBeastFur, PlaceId.Torvern);
            Produces(GoodId.FrostIron, PlaceId.Draven);
            Produces(GoodId.FingerFungus, PlaceId.Draven);
            Produces(GoodId.FireSalt, PlaceId.Sorel);
            Produces(GoodId.Sweetrock, PlaceId.Sorel);
            Produces(GoodId.ClackFiendLarvae, PlaceId.Kethra);
            Produces(GoodId.ClackFiendMoltSkin, PlaceId.Kethra);
            Produces(GoodId.EyesOfArkhul, PlaceId.Neth);
            Produces(GoodId.RethsBlood, PlaceId.Neth);
            Produces(GoodId.SarnsBaneLiver, PlaceId.Rukh);
            Produces(GoodId.KelEelFlesh, PlaceId.Rukh);
            Produces(GoodId.ZarKunSinew, PlaceId.Ghul);
            Produces(GoodId.HulvBone, PlaceId.Ghul);
            for (int i = 0; i < WorldIds.PlaceCount; i++)
            {
                if (KindOf((PlaceId)i) == PlaceKind.Post)
                    continue;
                producer[i, (int)GoodId.Water] = true;
                producer[i, (int)GoodId.Rations] = true;
            }
        }

        static void Produces(GoodId good, PlaceId place)
        {
            producer[(int)place, (int)good] = true;
        }

        static void ReadWeight(string path)
        {
            string[] lines = File.ReadAllLines(path);
            bool header = true;
            foreach (string raw in lines)
            {
                string line = Strip(raw);
                if (line.Length == 0)
                    continue;
                if (header)
                {
                    header = false;
                    continue;
                }

                string[] fields = line.Split(',');
                if (fields.Length < 4)
                    throw new InvalidOperationException("Short weight row: " + line);
                GoodId good = RequireGood(fields[0]);
                weight[(int)good] = int.Parse(fields[1].Trim(), CultureInfo.InvariantCulture);
                space[(int)good] = int.Parse(fields[2].Trim(), CultureInfo.InvariantCulture);
                basePrice[(int)good] = int.Parse(fields[3].Trim(), CultureInfo.InvariantCulture);
            }
        }

        static void ReadEat(string path)
        {
            string[] lines = File.ReadAllLines(path);
            string[] header = null;
            foreach (string raw in lines)
            {
                string line = Strip(raw);
                if (line.Length == 0)
                    continue;
                string[] fields = line.Split(',');
                if (header == null)
                {
                    header = fields;
                    continue;
                }

                PlaceId place = RequirePlace(fields[0]);
                for (int i = 1; i < header.Length; i++)
                {
                    GoodId good = RequireGood(header[i]);
                    string cell = i < fields.Length ? fields[i] : "0";
                    eat[(int)place, (int)good] = ParseEat(cell);
                }
            }

            if (header == null)
                throw new InvalidOperationException("Eat sheet is empty: " + path);
        }

        static float ParseEat(string text)
        {
            text = text.Trim();
            if (text.StartsWith("=", StringComparison.Ordinal))
                text = text.Substring(1);
            int slash = text.IndexOf('/');
            if (slash >= 0)
            {
                float num = float.Parse(text.Substring(0, slash), CultureInfo.InvariantCulture);
                float den = float.Parse(text.Substring(slash + 1), CultureInfo.InvariantCulture);
                return den == 0f ? 0f : num / den;
            }

            if (text.Length == 0)
                return 0f;
            return float.Parse(text, CultureInfo.InvariantCulture);
        }

        static GoodId RequireGood(string name)
        {
            string key = name.Trim();
            if (GoodByName.TryGetValue(key, out GoodId good))
                return good;
            throw new InvalidOperationException("Unknown good: " + key);
        }

        static PlaceId RequirePlace(string name)
        {
            string key = name.Trim();
            if (PlaceByName.TryGetValue(key, out PlaceId place))
                return place;
            throw new InvalidOperationException("Unknown place: " + key);
        }

        static string DocsFile(string file)
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Docs", file));
            if (!File.Exists(path))
                throw new FileNotFoundException("Economy sheet was not found.", path);
            return path;
        }

        static string Strip(string line)
        {
            if (line == null)
                return string.Empty;
            return line.Trim().TrimStart('\uFEFF');
        }

        static Dictionary<string, PlaceId> BuildPlaces()
        {
            var map = new Dictionary<string, PlaceId>(StringComparer.Ordinal);
            map["Khar\u00fbn"] = PlaceId.Kharun;
            map["Zamath"] = PlaceId.Zamath;
            map["Thalor"] = PlaceId.Thalor;
            map["Veythar"] = PlaceId.Veythar;
            map["Ghorath"] = PlaceId.Ghorath;
            map["Ashar"] = PlaceId.Ashar;
            map["Moraq"] = PlaceId.Moraq;
            map["Torvern"] = PlaceId.Torvern;
            map["Draven"] = PlaceId.Draven;
            map["Sorel"] = PlaceId.Sorel;
            map["Kethra"] = PlaceId.Kethra;
            map["Neth"] = PlaceId.Neth;
            map["Rukh"] = PlaceId.Rukh;
            map["Gh\u00fbl"] = PlaceId.Ghul;
            map["Westmark"] = PlaceId.Westmark;
            map["Kaleth"] = PlaceId.Kaleth;
            map["Southmark"] = PlaceId.Southmark;
            map["Highmark"] = PlaceId.Highmark;
            map["Brineford"] = PlaceId.Brineford;
            map["Ridgewatch"] = PlaceId.Ridgewatch;
            return map;
        }

        static Dictionary<string, GoodId> BuildGoods()
        {
            var map = new Dictionary<string, GoodId>(StringComparer.Ordinal);
            map["Shimmersteel"] = GoodId.Shimmersteel;
            map["Shimmersteel Jars"] = GoodId.ShimmersteelJars;
            map["Shimmersteel Cookware"] = GoodId.ShimmersteelCookware;
            map["Shimmersteel Fineware"] = GoodId.ShimmersteelFineware;
            map["Frost Iron"] = GoodId.FrostIron;
            map["Titan's Heart-rock"] = GoodId.TitansHeartRock;
            map["Palestone Bricks"] = GoodId.PalestoneBricks;
            map["Living Lumber"] = GoodId.LivingLumber;
            map["Hulv Bone"] = GoodId.HulvBone;
            map["Water Witching Rods"] = GoodId.WaterWitchingRods;
            map["Khor Crafted Tools"] = GoodId.KhorCraftedTools;
            map["Vek-kilned Pottery"] = GoodId.VekKilnedPottery;
            map["Zar'Kun Sinew"] = GoodId.ZarKunSinew;
            map["Scrubcast Resin"] = GoodId.ScrubcastResin;
            map["Brine Scrub"] = GoodId.BrineScrub;
            map["Mor Root"] = GoodId.MorRoot;
            map["Giant Duzhu Hide"] = GoodId.GiantDuzhuHide;
            map["Pincer Beast Fur"] = GoodId.PincerBeastFur;
            map["Clack Fiend Molt-skin"] = GoodId.ClackFiendMoltSkin;
            map["Tiger Cactus Flesh"] = GoodId.TigerCactusFlesh;
            map["Kel Eel Flesh"] = GoodId.KelEelFlesh;
            map["Clack Fiend Larvae"] = GoodId.ClackFiendLarvae;
            map["Moon Soaked Zethi-Bulb"] = GoodId.MoonSoakedZethiBulb;
            map["Finger Fungus"] = GoodId.FingerFungus;
            map["Fire Salt"] = GoodId.FireSalt;
            map["Sweetrock"] = GoodId.Sweetrock;
            map["Sweet Bliss Pomegranates"] = GoodId.SweetBliss;
            map["Sunburnt Brew"] = GoodId.SunburntBrew;
            map["Crimson Eyed Melon Juice"] = GoodId.CrimsonEyedMelonJuice;
            map["Dream Lotus Nectar"] = GoodId.DreamLotusNectar;
            map["Sarn's Bane Liver"] = GoodId.SarnsBaneLiver;
            map["Numbspindle"] = GoodId.Numbspindle;
            map["Dune Mollusk Pearls"] = GoodId.DuneMolluskPearls;
            map["Eyes of Arkhul"] = GoodId.EyesOfArkhul;
            map["Terrorshriek Talons"] = GoodId.TerrorshriekTalons;
            map["Kelkris Feathers"] = GoodId.KelkrisFeathers;
            map["Reth's Blood"] = GoodId.RethsBlood;
            map["Ishka Crafted Music Makers"] = GoodId.IshkaMusicMakers;
            map["Music Makers"] = GoodId.IshkaMusicMakers;
            map["Water"] = GoodId.Water;
            map["Rations"] = GoodId.Rations;
            return map;
        }
    }
}
