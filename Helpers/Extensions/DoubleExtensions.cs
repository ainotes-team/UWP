using System;

namespace Helpers.Extensions {
    public static class DoubleExtensions {
        public static double Clamp(this double x, double min, double max) { 
            return Math.Min(Math.Max(x, min), max);
        }
    }
}