using UnityEngine;

namespace Caravans.Play
{
    public static class MarketSounds
    {
        public const string SaleResource = "Audio/sale-scrubstone";

        static AudioSource source;

        public static string BuyResource(GoodId good)
        {
            if (good == GoodId.Rations)
                return null;
            if (good == GoodId.Shimmersteel)
                return "Audio/ore-market-to-caravan-ui";
            return "Audio/buy-" + good;
        }

        public static void Bind(GameObject host)
        {
            source = host.GetComponent<AudioSource>();
            if (source == null)
                source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }

        public static void PlayBuy(GoodId good)
        {
            Play(BuyResource(good));
        }

        public static void PlaySale()
        {
            Play(SaleResource);
        }

        static void Play(string resource)
        {
            if (source == null || string.IsNullOrEmpty(resource))
                return;
            AudioClip clip = Resources.Load<AudioClip>(resource);
            if (clip == null)
                return;
            source.PlayOneShot(clip);
        }
    }
}
