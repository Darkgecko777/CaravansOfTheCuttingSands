using UnityEngine;

namespace Caravans.Play
{
    public static class MapTokens
    {
        public const float PixelSize = 48f;
        public static readonly Vector2 Pivot = new Vector2(0.5f, 0.0625f);

        public static string PlayerSpriteName(int tier)
        {
            if (tier == 2)
                return "player_duzhu_256";
            if (tier == 3)
                return "player_pincer_beast_256";
            return "player_thrask_256";
        }

        public static string NpcSpriteName(int index)
        {
            int slot = index % 3;
            if (slot < 0)
                slot += 3;
            if (slot == 1)
                return "npc_duzhu_rust_256";
            if (slot == 2)
                return "npc_duzhu_ivory_256";
            return "npc_duzhu_darkhide_256";
        }

        public static string ResourcePath(string spriteName)
        {
            return "MapMarkers/" + spriteName;
        }

        public static float FacingSign(float headingX, float previous)
        {
            if (headingX > 0.0008f)
                return -1f;
            if (headingX < -0.0008f)
                return 1f;
            return previous < 0f ? -1f : 1f;
        }
    }
}
