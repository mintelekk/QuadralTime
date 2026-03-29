using System;

namespace QuadralTimeApp
{
    public static class TimeExtensions
    {
        public static int Hours(this TimeSpan t) => t.Hours;
        public static int Minutes(this TimeSpan t) => t.Minutes;
    }
}
