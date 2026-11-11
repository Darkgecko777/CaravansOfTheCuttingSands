using UnityEngine;

namespace Caravans.FrontDoor
{
    // Sprites imported from Assets/Art/Approved. The binder in the Editor fills this asset.
    public sealed class ApprovedArt : ScriptableObject
    {
        public Sprite hidePanel;
        public Sprite hideBar;
        public Sprite goodsCell;
        public Sprite goodsCellSelected;
        public Sprite sealBase;
        public Sprite sealMarket;
        public Sprite sealMarketHover;
        public Sprite sealMarketPressed;
        public Sprite sealInn;
        public Sprite sealInnHover;
        public Sprite sealInnPressed;
        public Sprite sealHouse;
        public Sprite sealHouseHover;
        public Sprite sealHousePressed;
        public Sprite sealGates;
        public Sprite sealGatesHover;
        public Sprite sealGatesPressed;
        public Sprite sealBuy;
        public Sprite sealBuyHover;
        public Sprite sealBuyPressed;
        public Sprite sealSell;
        public Sprite sealSellHover;
        public Sprite sealSellPressed;
        public Sprite iconScrubstone;
        public Sprite iconCapacity;
        public Sprite meterFrame;
        public Sprite meterFill;
        public Texture2D settlementGeneric;

        static ApprovedArt loaded;

        public static ApprovedArt Load()
        {
            if (loaded == null)
                loaded = Resources.Load<ApprovedArt>("Ui/ApprovedArt");
            return loaded;
        }
    }
}
