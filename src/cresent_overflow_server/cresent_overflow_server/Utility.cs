
using System;
using System.Globalization;

namespace cresent_overflow_server {
    public static class Utility
    {
        public static DateTime Today()
        {
            return DateTime.UtcNow.AddHours(9);
        }

    }
}