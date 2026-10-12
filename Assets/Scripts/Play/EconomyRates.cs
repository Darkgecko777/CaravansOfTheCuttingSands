namespace Caravans.Play
{
    // Balancing levers live in data/config. This type is the read path.
    // FullCoverDays is the shelf that sells at FullMultiplier. OrdinaryCoverDays sells at the base.
    // EmptyMultiplier is the ceiling. ProductionScale is nightly make against world eat.
    // A sea opening adds no clock time. NPC profit still divides by SeaHourFloor.
    public static class EconomyRates
    {
        public static int FullCoverDays
        {
            get { return GameConfig.Prices.fullCoverDays; }
        }

        public static int OrdinaryCoverDays
        {
            get { return GameConfig.Prices.ordinaryCoverDays; }
        }

        public static float EmptyMultiplier
        {
            get { return GameConfig.Prices.emptyMultiplier; }
        }

        public static float FullMultiplier
        {
            get { return GameConfig.Prices.fullMultiplier; }
        }

        public static float ProductionScale
        {
            get { return GameConfig.Prices.productionScale; }
        }

        public static int MorningHour
        {
            get { return GameConfig.Travel.morningHour; }
        }

        public static int NightHour
        {
            get { return GameConfig.Travel.nightHour; }
        }

        public static float DayHours
        {
            get { return GameConfig.Travel.dayHours; }
        }

        public static int WagonSpace
        {
            get { return GameConfig.Travel.wagonSpace; }
        }

        public static int WagonWeight
        {
            get { return GameConfig.Travel.wagonWeight; }
        }

        public static int WaterBasePrice
        {
            get { return GameConfig.Prices.waterBasePrice; }
        }

        public static int WaterWeight
        {
            get { return GameConfig.Prices.waterWeight; }
        }

        public static int WaterSpace
        {
            get { return GameConfig.Prices.waterSpace; }
        }

        public static int RationBasePrice
        {
            get { return GameConfig.Prices.rationBasePrice; }
        }

        public static int RationWeight
        {
            get { return GameConfig.Prices.rationWeight; }
        }

        public static int RationSpace
        {
            get { return GameConfig.Prices.rationSpace; }
        }

        public static float RationEat
        {
            get { return GameConfig.Consumption.rationEat; }
        }

        public static float WaterLitresPerDay
        {
            get { return GameConfig.Consumption.waterLitresPerDay; }
        }

        public static float RationsKgPerDay
        {
            get { return GameConfig.Consumption.rationsKgPerDay; }
        }

        public static float CanteenLitres
        {
            get { return GameConfig.Consumption.canteenLitres; }
        }

        public static float RationBagKg
        {
            get { return GameConfig.Consumption.rationBagKg; }
        }

        public static float WaterLitresPerUnit
        {
            get { return GameConfig.Consumption.waterLitresPerUnit; }
        }

        public static float RationsKgPerUnit
        {
            get { return GameConfig.Consumption.rationsKgPerUnit; }
        }

        public static int TariffBase
        {
            get { return GameConfig.Prices.tariffBase; }
        }

        public static int TariffFloor
        {
            get { return GameConfig.Prices.tariffFloor; }
        }

        public static int StandingMax
        {
            get { return GameConfig.Prices.standingMax; }
        }

        public static float InfluenceKeep
        {
            get { return GameConfig.Prices.influenceKeep; }
        }

        public static int NpcHopLimit
        {
            get { return GameConfig.Rivals.hopLimit; }
        }

        public static int NpcSeed
        {
            get { return GameConfig.Rivals.seed; }
        }

        public static float SeaHourFloor
        {
            get { return GameConfig.Travel.seaHourFloor; }
        }

        public static float OrdinaryDecay
        {
            get { return GameConfig.Consumption.ordinaryDecay; }
        }

        public static float SweetBlissDecay
        {
            get { return GameConfig.Consumption.sweetBlissDecay; }
        }

        public static Recipe[] Recipes
        {
            get { return GameConfig.Workshops; }
        }
    }

    public struct Recipe
    {
        public PlaceId Place;
        public GoodId Output;
        public GoodId InputA;
        public int CountA;
        public GoodId InputB;
        public int CountB;

        public Recipe(PlaceId place, GoodId output, GoodId inputA, int countA, GoodId inputB, int countB)
        {
            Place = place;
            Output = output;
            InputA = inputA;
            CountA = countA;
            InputB = inputB;
            CountB = countB;
        }

        public bool HasSecond
        {
            get { return CountB > 0; }
        }
    }
}
