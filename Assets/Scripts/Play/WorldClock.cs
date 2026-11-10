namespace Caravans.Play
{
    public static class WorldClock
    {
        public const int FirstYear = 112;

        static readonly string[] Months =
        {
            "Vekmar", "Dravun", "Morvek", "Thulor", "Zarneth", "Kalvorn", "Sorvek", "Torvak",
        };

        public static string Format(int day, float hour)
        {
            return Date(day) + " · " + DayPart(hour);
        }

        public static string Date(int day)
        {
            if (day < 1)
                day = 1;
            int dayOfEra = day - 1;
            int year = FirstYear + dayOfEra / 240;
            int dayOfYear = dayOfEra % 240;
            string month = Months[dayOfYear / 30];
            int dayOfMonth = (dayOfYear % 30) + 1;
            return dayOfMonth + " " + month + ", Year " + year;
        }

        public static string DayPart(float hour)
        {
            float h = hour % 24f;
            if (h < 0f)
                h += 24f;
            if (h >= 6f && h < 9f)
                return "Dawn";
            if (h >= 9f && h < 12f)
                return "Morning";
            if (h >= 12f && h < 15f)
                return "Midday";
            if (h >= 15f && h < 18f)
                return "Afternoon";
            if (h >= 18f && h < 21f)
                return "Dusk";
            if (h >= 21f)
                return "Evening";
            if (h < 3f)
                return "Night";
            return "Deep night";
        }
    }
}
