namespace Caravans.Play
{
    public static class WorldIds
    {
        public const int PlaceCount = 20;
        public const int GoodCount = 41;
        public const int HouseCount = 5;
    }

    public enum PlaceKind
    {
        City,
        Village,
        Post
    }

    // Kharun then Draven stay first. The tests use those names.
    public enum PlaceId
    {
        Kharun,
        Draven,
        Zamath,
        Thalor,
        Veythar,
        Ghorath,
        Ashar,
        Moraq,
        Torvern,
        Sorel,
        Kethra,
        Neth,
        Rukh,
        Ghul,
        Westmark,
        Kaleth,
        Southmark,
        Highmark,
        Brineford,
        Ridgewatch
    }

    // Jars, fungus, water, and rations stay first. The tests buy them by these names.
    public enum GoodId
    {
        ShimmersteelJars,
        FingerFungus,
        Water,
        Rations,
        Shimmersteel,
        ShimmersteelCookware,
        ShimmersteelFineware,
        FrostIron,
        TitansHeartRock,
        PalestoneBricks,
        LivingLumber,
        HulvBone,
        WaterWitchingRods,
        KhorCraftedTools,
        VekKilnedPottery,
        ZarKunSinew,
        ScrubcastResin,
        BrineScrub,
        MorRoot,
        GiantDuzhuHide,
        PincerBeastFur,
        ClackFiendMoltSkin,
        TigerCactusFlesh,
        KelEelFlesh,
        ClackFiendLarvae,
        MoonSoakedZethiBulb,
        FireSalt,
        Sweetrock,
        SweetBliss,
        SunburntBrew,
        CrimsonEyedMelonJuice,
        DreamLotusNectar,
        SarnsBaneLiver,
        Numbspindle,
        DuneMolluskPearls,
        EyesOfArkhul,
        TerrorshriekTalons,
        KelkrisFeathers,
        RethsBlood,
        IshkaMusicMakers,
        SeaWater
    }

    public enum HouseId
    {
        Kharun,
        Zamath,
        Thalor,
        Veythar,
        Ghorath
    }
}
