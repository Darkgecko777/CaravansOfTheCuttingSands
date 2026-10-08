namespace Caravans.JarsRun
{
    // Balancing levers for this build. Change the numbers here.
    // FullCoverDays is the shelf that sells at FullMultiplier. OrdinaryCoverDays sells at the base.
    // EmptyMultiplier is the ceiling. ProductionScale is nightly make against world eat.
    // A sea opening adds no clock time. NPC profit still divides by SeaHourFloor.
    public static class EconomyRates
    {
        public const int FullCoverDays = 6;
        public const int OrdinaryCoverDays = 3;
        public const float EmptyMultiplier = 2f;
        public const float FullMultiplier = 0.5f;
        public const float ProductionScale = 1f;
        public const int MorningHour = 6;
        public const int NightHour = 18;
        public const float DayHours = 24f;

        public const int WagonSpace = 16;
        public const int WagonWeight = 256;

        public const int WaterBasePrice = 6;
        public const int WaterWeight = 2;
        public const int WaterSpace = 1;
        public const int RationBasePrice = 5;
        public const int RationWeight = 1;
        public const int RationSpace = 1;
        public const float RationEat = 1f;

        public const int TariffBase = 12;
        public const int TariffFloor = 4;
        public const int StandingMax = 8;
        public const float InfluenceKeep = 0.75f;

        public const int NpcHopLimit = 2;
        public const int NpcSeed = 1701;
        public const float SeaHourFloor = 1f;

        public const float OrdinaryDecay = 0.10f;
        public const float SweetBlissDecay = 0.25f;

        public static readonly Recipe[] Recipes =
        {
            new Recipe(PlaceId.Kharun, GoodId.ShimmersteelJars, GoodId.Shimmersteel, 2, GoodId.Shimmersteel, 0),
            new Recipe(PlaceId.Thalor, GoodId.ShimmersteelCookware, GoodId.Shimmersteel, 1, GoodId.TitansHeartRock, 1),
            new Recipe(PlaceId.Veythar, GoodId.ShimmersteelFineware, GoodId.Shimmersteel, 1, GoodId.Shimmersteel, 0),
            new Recipe(PlaceId.Zamath, GoodId.WaterWitchingRods, GoodId.LivingLumber, 1, GoodId.LivingLumber, 0),
            new Recipe(PlaceId.Ghul, GoodId.HulvBone, GoodId.Numbspindle, 1, GoodId.Numbspindle, 0)
        };
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
