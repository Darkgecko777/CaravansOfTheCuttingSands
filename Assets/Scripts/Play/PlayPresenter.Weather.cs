using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Caravans.Play
{
    public sealed partial class PlayPresenter
    {
        RawImage weatherHaze;
        RectTransform weatherRoot;
        readonly List<RawImage> massPatches = new List<RawImage>();
        Texture2D hazeTexture;
        Texture2D massTexture;
        WeatherLook.Weights lookWeights;

        internal float SandstormLookWeight
        {
            get { return lookWeights.Sandstorm; }
        }

        void BuildWeatherLook()
        {
            WeatherLookFile look = GameConfig.WeatherLook;
            hazeTexture = WeatherLook.BuildHazeTexture(look.noiseSeed);
            massTexture = WeatherLook.BuildMassTexture(look.noiseSeed);

            weatherRoot = Child(mapSheet, "Weather");
            Stretch(weatherRoot);
            weatherRoot.SetSiblingIndex(0);

            IReadOnlyList<WeatherMass> masses = session.Masses;
            var order = new int[masses.Count];
            for (int i = 0; i < masses.Count; i++)
                order[i] = i;
            System.Array.Sort(order, delegate (int a, int b)
            {
                return KindOrder(masses[a].Kind).CompareTo(KindOrder(masses[b].Kind));
            });

            for (int i = 0; i < masses.Count; i++)
                massPatches.Add(null);
            for (int slot = 0; slot < order.Length; slot++)
            {
                int index = order[slot];
                var patch = Child(weatherRoot, "Mass").gameObject.AddComponent<RawImage>();
                patch.texture = massTexture;
                patch.raycastTarget = false;
                patch.color = WeatherLook.MassColour(masses[index].Kind);
                massPatches[index] = patch;
            }

            RectTransform haze = Child(mapRoot, "WeatherHaze");
            haze.SetSiblingIndex(mapSheet.GetSiblingIndex() + 1);
            Stretch(haze);
            weatherHaze = haze.gameObject.AddComponent<RawImage>();
            weatherHaze.texture = hazeTexture;
            weatherHaze.raycastTarget = false;
            weatherHaze.color = new Color(1f, 1f, 1f, 0f);
            weatherHaze.enabled = false;
        }

        void StepWeatherLook(float dt)
        {
            if (session == null)
                return;
            WeatherKind target = WeatherLook.Target(session.Traveling, session.WeatherAt(session.CaravanPoint));
            lookWeights = WeatherLook.Step(lookWeights, target, dt, GameConfig.WeatherLook.fadeSeconds);
        }

        void PaintWeatherLook()
        {
            if (mapImage != null && mapImage.texture != null)
            {
                float sun = TravelScale.Sunlight(session.Hour);
                Color sunColour = Color.Lerp(new Color(0.55f, 0.62f, 0.8f), Color.white, sun);
                mapImage.color = WeatherLook.MapGrade(sunColour, lookWeights);
            }

            if (weatherHaze != null)
            {
                Color haze = WeatherLook.HazeColour(lookWeights);
                weatherHaze.color = haze;
                weatherHaze.enabled = haze.a >= 0.002f;
            }

            if (weatherRoot != null)
                weatherRoot.gameObject.SetActive(GameConfig.WeatherLook.showMasses);
            if (weatherRoot == null || mapSheet == null || session == null)
                return;

            float height = mapSheet.rect.height;
            IReadOnlyList<WeatherMass> masses = session.Masses;
            int count = masses.Count < massPatches.Count ? masses.Count : massPatches.Count;
            for (int i = 0; i < count; i++)
            {
                RawImage patch = massPatches[i];
                if (patch == null)
                    continue;
                WeatherMass mass = masses[i];
                PlaceOnMap(patch.rectTransform, mass.Center, WeatherLook.MassDiameter(mass.Radius, height));
                patch.color = WeatherLook.MassColour(mass.Kind);
            }
        }

        void OnDestroy()
        {
            ReleaseSteamSync();
            if (hazeTexture != null)
                Destroy(hazeTexture);
            if (massTexture != null)
                Destroy(massTexture);
        }

        static int KindOrder(WeatherKind kind)
        {
            switch (kind)
            {
                case WeatherKind.Heat:
                    return 0;
                case WeatherKind.Wind:
                    return 1;
                case WeatherKind.Sandstorm:
                    return 2;
                default:
                    return 3;
            }
        }

#if UNITY_EDITOR
        void SnapWeatherLook()
        {
            if (session == null)
                return;
            WeatherKind target = WeatherLook.Target(session.Traveling, session.WeatherAt(session.CaravanPoint));
            lookWeights = WeatherLook.Snap(target);
        }

        bool ShotMapWeather(WeatherKind kind)
        {
            ShotClearOverlays();
            session.ShotDockAt(PlaceId.Kharun);
            if (!session.ShotPlaceWeather(kind, RoadGraph.DockPoint(PlaceId.Kharun), 0.10f))
                return false;
            if (!session.ShotDepartPaused(PlaceId.Draven))
                return false;
            page = Page.Plaza;
            plazaSpot = PlazaSpot.Gates;
            SnapWeatherLook();
            FinishShot();
            if (!session.Traveling)
                return false;
            if (session.WeatherAt(session.CaravanPoint) != kind || session.LegWeather != kind)
                return false;
            if (kind == WeatherKind.Sandstorm && Mathf.Abs(session.VisionRadius() - 0.07f) > 0.0001f)
                return false;
            return true;
        }

        bool ShotGatesStorm()
        {
            ShotClearOverlays();
            if (session.Traveling || session.Dock != PlaceId.Kharun)
                session.ShotDockAt(PlaceId.Kharun);
            page = Page.Plaza;
            plazaSpot = PlazaSpot.Gates;
            if (!session.CanTakeRoad())
                return false;
            pendingDest = PlaceId.Draven;
            roadLit = true;
            draftPace = RoadPace.Steady;
            choosingRoad = false;
            if (!session.ShotPlaceWeather(WeatherKind.Sandstorm, RoadPath.PointAtDistance(RoadPath.Length * 0.5f), 0.10f))
                return false;
            SnapWeatherLook();
            FinishShot();
            return roadLit && pendingDest == PlaceId.Draven && session.WeatherAlongRoad() == WeatherKind.Sandstorm;
        }
#endif
    }
}
