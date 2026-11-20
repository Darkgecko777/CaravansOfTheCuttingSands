using Caravans.FrontDoor;
using NUnit.Framework;
using UnityEngine;

namespace Caravans.Play.Tests
{
    public class FrontDoorTests
    {
        [Test]
        public void IntroShowsOnceUnlessOptionsAsksAgain()
        {
            bool hadSeen = PlayerPrefs.HasKey(SessionSettings.IntroSeenKey);
            bool hadAgain = PlayerPrefs.HasKey(SessionSettings.IntroShowAgainKey);
            int seen = PlayerPrefs.GetInt(SessionSettings.IntroSeenKey, 0);
            int again = PlayerPrefs.GetInt(SessionSettings.IntroShowAgainKey, 0);
            try
            {
                PlayerPrefs.DeleteKey(SessionSettings.IntroSeenKey);
                PlayerPrefs.DeleteKey(SessionSettings.IntroShowAgainKey);
                SessionSettings.LoadIntro();
                Assert.IsTrue(SessionSettings.ShouldShowIntro());

                SessionSettings.MarkIntroSeen();
                Assert.IsFalse(SessionSettings.ShouldShowIntro());
                Assert.AreEqual(1, PlayerPrefs.GetInt(SessionSettings.IntroSeenKey, 0));
                Assert.AreEqual(0, PlayerPrefs.GetInt(SessionSettings.IntroShowAgainKey, 0));

                SessionSettings.SetShowIntroAgain(true);
                Assert.IsTrue(SessionSettings.ShouldShowIntro());
                Assert.AreEqual(1, PlayerPrefs.GetInt(SessionSettings.IntroShowAgainKey, 0));

                SessionSettings.MarkIntroSeen();
                Assert.IsFalse(SessionSettings.ShouldShowIntro());
                Assert.AreEqual(0, PlayerPrefs.GetInt(SessionSettings.IntroShowAgainKey, 0));
            }
            finally
            {
                if (hadSeen)
                    PlayerPrefs.SetInt(SessionSettings.IntroSeenKey, seen);
                else
                    PlayerPrefs.DeleteKey(SessionSettings.IntroSeenKey);
                if (hadAgain)
                    PlayerPrefs.SetInt(SessionSettings.IntroShowAgainKey, again);
                else
                    PlayerPrefs.DeleteKey(SessionSettings.IntroShowAgainKey);
                SessionSettings.LoadIntro();
            }
        }

        [Test]
        public void IntroDismissSwallowsThePauseKey()
        {
            bool readIntro;
            bool readPause;
            PlayPresenter.MenuReads(true, false, out readIntro, out readPause);
            Assert.IsTrue(readIntro);
            Assert.IsFalse(readPause);

            PlayPresenter.MenuReads(false, true, out readIntro, out readPause);
            Assert.IsFalse(readIntro);
            Assert.IsFalse(readPause);

            PlayPresenter.MenuReads(false, false, out readIntro, out readPause);
            Assert.IsFalse(readIntro);
            Assert.IsTrue(readPause);
        }

        [Test]
        public void ARunningFadeIsNotRestarted()
        {
            var fade = new StormFade();
            fade.Begin(0.5f, 2.2f);
            fade.Tick(0.4f);
            float visual = fade.Visual;
            float audio = fade.AudioAmplitude;

            fade.Begin(1f, 9f);
            fade.Tick(0.01f);

            Assert.Less(fade.Visual, visual);
            Assert.Less(fade.AudioAmplitude, audio);
            Assert.Greater(fade.Visual, 0.5f);
            Assert.IsTrue(fade.Active);
        }

        [Test]
        public void FadeReachesSilenceWithoutAnotherBegin()
        {
            var fade = new StormFade();
            fade.Begin(0.8f, 2.2f);
            fade.Tick(0.05f);

            for (int i = 0; i < 30; i++)
                fade.Tick(0.1f);

            Assert.IsTrue(fade.Finished);
            Assert.AreEqual(0f, fade.Visual, 0.0001f);
            Assert.AreEqual(0f, fade.AudioAmplitude, 0.0001f);
            fade.Tick(1f);
            Assert.AreEqual(0f, fade.Visual, 0.0001f);
        }

        [Test]
        public void SmoothstepIsHalfwayAtMidpoint()
        {
            var fade = new StormFade();
            fade.Begin(1f, 2.2f);
            fade.Tick(1.1f);
            Assert.AreEqual(0.5f, fade.Visual, 0.001f);
            Assert.AreEqual(0.5f, fade.AudioAmplitude, 0.001f);
        }

        [Test]
        public void ResumeAllowsANewFade()
        {
            var fade = new StormFade();
            fade.Begin(1f, 2.2f);
            fade.Tick(2.2f);
            Assert.IsTrue(fade.Finished);

            fade.Resume();
            Assert.IsFalse(fade.Finished);
            Assert.AreEqual(1f, fade.Visual, 0.0001f);

            fade.Begin(0.25f, 2.2f);
            Assert.IsTrue(fade.Active);
            Assert.AreEqual(0.25f, fade.AudioAmplitude, 0.0001f);
            Assert.AreEqual(1f, fade.Visual, 0.0001f);
        }

        [Test]
        public void SandStaysFiniteWhileTheGustBreathes()
        {
            var sim = new SandBedSim(1);
            var vertices = new Vector3[sim.GrainCount * 4];
            var colors = new Color32[sim.GrainCount * 4];
            for (int i = 0; i < 180; i++)
            {
                float gust = sim.Tick(1f / 60f);
                Assert.GreaterOrEqual(gust, 0f);
                Assert.LessOrEqual(gust, 1f);
            }

            sim.Write(vertices, colors, new Vector3(50000f, 50000f, 0f), 960f, 540f, 1f);
            for (int i = 0; i < vertices.Length; i++)
            {
                Assert.IsFalse(float.IsNaN(vertices[i].x));
                Assert.IsFalse(float.IsNaN(vertices[i].y));
                Assert.Greater(vertices[i].x, 40000f);
                Assert.Less(vertices[i].x, 60000f);
            }
        }
    }
}
