using System;

namespace Caravans.Play
{
    [Serializable]
    public class AgentFile
    {
        public int scoutHire;
        public int scoutWage;
        public int hagglerHire;
        public int hagglerWage;
        public int guardHire;
        public int guardWage;
        public float hagglerSpread;
        public float rushCargoLossChance;
    }
}
