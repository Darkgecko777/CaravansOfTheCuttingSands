using NUnit.Framework;

namespace Caravans.Play.Tests
{
    public class TravelKitTests
    {
        [Test]
        public void KitsNameTheSixTerrainsAndTheirPieces()
        {
            TravelKitsFile file = GameConfig.ReadFile<TravelKitsFile>("travel_kits.json", "terrains");
            Assert.AreEqual(6, file.terrains.Length);

            TravelKitsFile.Terrain dune = file.Find("Dune Sea");
            Assert.IsTrue(dune.nearTiles);
            Assert.AreEqual(4, dune.near.Length);
            Assert.AreEqual(4, dune.far.Length);
            Assert.AreEqual(0.78f, dune.midScale, 0.001f);
            Assert.AreEqual(1f, dune.midScaleMax, 0.001f);
            Assert.AreEqual(0.48f, dune.farScale, 0.001f);
            Assert.AreEqual(647f, dune.midBottom, 0.01f);
            Assert.AreEqual(582f, dune.farBottom, 0.01f);
            Assert.IsFalse(dune.pieceSpacing);
            Assert.AreEqual(212f, dune.nearFill.r, 0.01f);
            AssertPpu(dune, "prop_dry_scrub_01", 280f, false);
            AssertPpu(dune, "prop_sandstone_outcrop_01", 210f, false);

            TravelKitsFile.Terrain badlands = file.Find("Badlands");
            Assert.AreEqual(4, badlands.near.Length);
            Assert.AreEqual(4, badlands.mid.Length);
            Assert.AreEqual(4, badlands.far.Length);
            Assert.AreEqual(0.93f, badlands.midScale, 0.001f);
            Assert.AreEqual(680f, badlands.midBottom, 0.01f);
            Assert.AreEqual(0.59f, badlands.farScale, 0.001f);
            Assert.AreEqual(603f, badlands.farBottom, 0.01f);
            Assert.AreEqual(36f, badlands.farFade, 0.01f);
            Assert.AreEqual(160f, badlands.farOverlapPx, 0.01f);
            Assert.IsTrue(badlands.pieceSpacing);
            Assert.AreEqual(155f, badlands.nearFill.r, 0.01f);
            Assert.AreEqual(201f, badlands.backFill.r, 0.01f);
            AssertSharedSky(badlands);
            AssertPpu(badlands, "prop_hoodoo_01", 450f, true);
            AssertLaneCounts(badlands, 3, 1);

            TravelKitsFile.Terrain scrub = file.Find("Dry Scrub");
            Assert.AreEqual(3, scrub.far.Length);
            Assert.IsFalse(Lists(scrub.far, "far_hills_02"));
            Assert.AreEqual(240f, scrub.farOverlapPx, 0.01f);
            Assert.AreEqual(1150f, scrub.farOverlapBelowPx, 0.01f);
            AssertSharedSky(scrub);
            AssertPpu(scrub, "prop_scrubstone_spires_01", 450f, true);
            AssertLaneCounts(scrub, 3, 1);

            TravelKitsFile.Terrain highlands = file.Find("Highlands");
            Assert.AreEqual(3, highlands.mid.Length);
            Assert.AreEqual(4, highlands.far.Length);
            Assert.IsFalse(Lists(highlands.mid, "mid_foothill_03"));
            Assert.AreEqual(0.62f, highlands.midScale, 0.001f);
            Assert.AreEqual("mid_foothill_04", highlands.oncePerScreen[0]);
            AssertPpu(highlands, "prop_rock_spire_01", 630f, true);
            AssertPpu(highlands, "prop_scree_pile_01", 415f, false);

            TravelKitsFile.Terrain coast = file.Find("Coast");
            Assert.AreEqual(0.62f, coast.midScale, 0.001f);
            Assert.AreEqual(458f, coast.farBottom, 0.01f);
            Assert.AreEqual(6f, coast.farFade, 0.01f);
            Assert.AreEqual(260f, coast.farGapPx, 0.01f);
            Assert.AreEqual(0f, coast.farOverlapPx, 0.01f);
            Assert.AreEqual("mid_shore_04", coast.oncePerScreen[0]);
            Assert.AreEqual(96f, coast.backFill.r, 0.01f);
            AssertPpu(coast, "prop_sea_stack_01", 630f, true);
            AssertPpu(coast, "prop_beach_grass_01", 315f, false);

            TravelKitsFile.Terrain salt = file.Find("Salt Flats");
            Assert.AreEqual(3, salt.near.Length);
            Assert.AreEqual(3, salt.far.Length);
            Assert.AreEqual(4, salt.mid.Length);
            Assert.IsFalse(Lists(salt.near, "near_salt_03"));
            Assert.IsFalse(Lists(salt.far, "far_salt_02"));
            Assert.AreEqual(0.62f, salt.midScale, 0.001f);
            Assert.AreEqual(625f, salt.midBottom, 0.01f);
            Assert.AreEqual(458f, salt.farBottom, 0.01f);
            Assert.GreaterOrEqual(salt.farFade, 6f);
            Assert.LessOrEqual(salt.farFade, 14f);
            Assert.AreEqual(260f, salt.farGapPx, 0.01f);
            AssertPpu(salt, "prop_salt_spire_01", 632f, true);
            AssertPpu(salt, "prop_saltbush_01", 358f, false);
            AssertLaneCounts(salt, 3, 1);
        }

        static void AssertSharedSky(TravelKitsFile.Terrain terrain)
        {
            StringAssert.Contains("Art/Travel/DuneSea/sky/sky_gradient_01", terrain.sky);
        }

        static void AssertLaneCounts(TravelKitsFile.Terrain terrain, int nearCount, int midCount)
        {
            terrain.SplitProps(out string[] near, out string[] mid);
            Assert.AreEqual(nearCount, near.Length);
            Assert.AreEqual(midCount, mid.Length);
        }

        static void AssertPpu(TravelKitsFile.Terrain terrain, string file, float ppu, bool mid)
        {
            TravelKitsFile.PropSpec spec = null;
            for (int i = 0; i < terrain.propSpecs.Length; i++)
            {
                if (terrain.propSpecs[i].path.Contains(file))
                    spec = terrain.propSpecs[i];
            }

            Assert.IsNotNull(spec, file);
            Assert.AreEqual(ppu, spec.ppu, 0.01f);
            Assert.AreEqual(mid, spec.mid);
        }

        static bool Lists(string[] paths, string fragment)
        {
            if (paths == null)
                return false;
            for (int i = 0; i < paths.Length; i++)
            {
                if (paths[i] != null && paths[i].Contains(fragment))
                    return true;
            }

            return false;
        }
    }
}
