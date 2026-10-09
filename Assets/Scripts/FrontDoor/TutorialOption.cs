using UnityEngine;

namespace Caravans.FrontDoor
{
    public static class TutorialOption
    {
        public const string Key = "caravans.tutorial";

        // Parked. A new game does not read this. A later pass can offer the lesson from it.
        public const bool DefaultEnabled = false;

        public static bool Enabled
        {
            get { return PlayerPrefs.GetInt(Key, DefaultEnabled ? 1 : 0) == 1; }
        }

        public static void Set(bool on)
        {
            PlayerPrefs.SetInt(Key, on ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
