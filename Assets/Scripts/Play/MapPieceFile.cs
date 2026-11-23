using System;

namespace Caravans.Play
{
    [Serializable]
    public class MapPieceFile
    {
        public int schemaVersion;
        public int piecesRequired;
        public string sellerPlace;
        public int leadPercent;
        public int leadBankMax;
        public string pieceText;
        public string firstAchievement;
        public string embarkAchievement;
        public Piece[] pieces;
        public string gatePlace;
        public string destinationLandmark;
        public float gateSteadyHours;
        public float finalSteadyHours;
        public string arrivalId;
        public string arrivalTitle;
        public string arrivalScene;
        public string embarkLabel;
        public string embarkResult;
        public string returnLabel;
        public string returnResult;
        public Card[] finalCards;

        [Serializable]
        public class Piece
        {
            public string id;
            public int unlockRank;
            public int price;
            public string name;
        }

        [Serializable]
        public class Card
        {
            public string id;
            public string title;
            public string line;
            public float fraction;
        }
    }
}
