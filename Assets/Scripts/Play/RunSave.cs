using System;

namespace Caravans.Play
{
    // One autosave. JsonUtility reads public fields only, so every row is a flat array.
    [Serializable]
    public class RunSave
    {
        public const int CurrentVersion = 1;

        public int saveVersion;
        public string savedAtUtc;
        public string buildVersion;

        public int coin;
        public float waterLitres;
        public float rationKg;
        public int day;
        public float hour;
        public float travelHours;

        public int dock;
        public int travelFrom;
        public int travelTo;
        public int chosenPace;
        public int stretchChecks;

        public PackRow[] packs;

        public int merchantMountTier;
        public int[] packBeasts;

        public bool scoutHired;
        public bool hagglerHired;
        public bool guardHired;
        public string[] roadNotes;

        public int[] shelfStock;
        public float[] eatAcc;
        public float[] makeAcc;
        public int[] boughtHere;

        public PriceRow[] loggedBuy;
        public PriceRow[] loggedSale;

        public float[] influence;
        public int[] patron;

        public NpcRow[] npcs;
        public WeatherRow[] weather;

        public float kharunPoints;
        public bool[] kharunFlipSeen;

        public string[] runFlags;
        public bool friendOfBandits;

        public int rescueCount;
        public string lastRescueText;

        public HeldRow[] held;
        public int mapPieceLeads;
        public int mapCoin;
        public int leadsUsed;
        public int legsTravelled;
        public int encountersResolved;
        public int buyCoin;
        public int saleCoin;
        public int unitsSold;
        public bool bestSaleSeen;
        public int bestSaleCoin;
        public int bestSaleGood;
        public int bestSalePlace;
        public int banditUnits;
        public int banditCoin;
        public int embarkedDay;

        public string[] achievementIds;
        public int[] achievementFires;

        public int runSeed;
        public long encounterRngState;
        public long rescueRngState;
        public long npcRngState;

        [Serializable]
        public class PackRow
        {
            public int good;
            public float integrity;
            public int decayedOnDay;
            public int basis;
        }

        [Serializable]
        public class PriceRow
        {
            public bool seen;
            public int coin;
            public int place;
        }

        [Serializable]
        public class NpcRow
        {
            public int house;
            public int dock;
            public bool traveling;
            public int from;
            public int to;
            public float distance;
            public float edgeLength;
            public int[] path;
            public int[] cargo;
        }

        [Serializable]
        public class WeatherRow
        {
            public int kind;
            public float centerX;
            public float centerY;
            public float radius;
            public float velocityX;
            public float velocityY;
        }

        [Serializable]
        public class HeldRow
        {
            public bool owned;
            public int day;
            public int listPrice;
            public int paid;
        }
    }
}
