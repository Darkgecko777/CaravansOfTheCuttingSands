using System;

namespace Caravans.Play
{
    [Serializable]
    public class EndScreenFile
    {
        public int schemaVersion;
        public string title;
        public string[] lines;
        public string footer;
        public string statsHeading;
        public Stat[] stats;
        public Button[] buttons;

        [Serializable]
        public class Stat
        {
            public string key;
            public string label;
        }

        [Serializable]
        public class Button
        {
            public string label;
            public string action;
        }
    }
}
