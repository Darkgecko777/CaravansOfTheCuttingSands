using NUnit.Framework;

namespace Caravans.Play.Tests
{
    public class MapTokenTests
    {
        [Test]
        public void PlayerTokenFollowsTheMount()
        {
            Assert.AreEqual("player_thrask_256", MapTokens.PlayerSpriteName(1));
            Assert.AreEqual("player_thrask_256", MapTokens.PlayerSpriteName(0));
            Assert.AreEqual("player_thrask_256", MapTokens.PlayerSpriteName(-1));
            Assert.AreEqual("player_thrask_256", MapTokens.PlayerSpriteName(4));
            Assert.AreEqual("player_duzhu_256", MapTokens.PlayerSpriteName(2));
            Assert.AreEqual("player_pincer_beast_256", MapTokens.PlayerSpriteName(3));
        }

        [Test]
        public void NpcTokenCyclesPackColour()
        {
            Assert.AreEqual("npc_duzhu_darkhide_256", MapTokens.NpcSpriteName(0));
            Assert.AreEqual("npc_duzhu_rust_256", MapTokens.NpcSpriteName(1));
            Assert.AreEqual("npc_duzhu_ivory_256", MapTokens.NpcSpriteName(2));
            Assert.AreEqual("npc_duzhu_darkhide_256", MapTokens.NpcSpriteName(3));
            Assert.AreEqual("npc_duzhu_rust_256", MapTokens.NpcSpriteName(4));
            Assert.AreEqual("npc_duzhu_ivory_256", MapTokens.NpcSpriteName(8));
        }
    }
}
