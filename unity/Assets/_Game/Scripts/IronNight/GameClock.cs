namespace IronNight
{
    /// <summary>
    /// The game's one clock, for everything that waits on time: the days, the quartermaster and the crates, the
    /// counterattacks on the war map, the war bonds season, premium service. The test switch --clock=+36 moves it
    /// by that many hours (fractions and negatives too), so all of it can be tried without waiting.
    /// </summary>
    public static class GameClock
    {
        static double? shift;
        static double Shift
        {
            get
            {
                if (shift == null)
                {
                    shift = 0.0;
                    foreach (var a in System.Environment.GetCommandLineArgs())
                        if (a.StartsWith("--clock=") && double.TryParse(a.Substring(8), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var h)) shift = h;
                }
                return shift.Value;
            }
        }
        public static System.DateTime Now => System.DateTime.Now.AddHours(Shift);
        public static System.DateTime UtcNow => System.DateTime.UtcNow.AddHours(Shift);
    }
}
