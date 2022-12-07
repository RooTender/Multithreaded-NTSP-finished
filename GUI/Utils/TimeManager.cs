using System;

namespace GUI.Utils;

public static class TimeManager
{
    internal static int GetDurationInMs(int phaseDuration, int units) => units switch
    {
        Units.Seconds => (int)TimeSpan.FromSeconds(phaseDuration).TotalMilliseconds,
        Units.Minutes => (int)TimeSpan.FromMinutes(phaseDuration).TotalMilliseconds,
        _ => throw new NotImplementedException()
    };

    public static class Units
    {
        public const char Seconds = 's';
        public const char Minutes = 'm';
    }
}
