using System;

namespace EL2MapGenMod.Tuning
{
    internal static class ClampUtil
    {
        public static byte ClampByte(int value)
        {
            if (value < byte.MinValue)
                return byte.MinValue;

            if (value > byte.MaxValue)
                return byte.MaxValue;

            return (byte)value;
        }

        public static sbyte ClampSByte(int value)
        {
            if (value < sbyte.MinValue)
                return sbyte.MinValue;

            if (value > sbyte.MaxValue)
                return sbyte.MaxValue;

            return (sbyte)value;
        }

        public static int ClampInt(int value, int min, int max)
        {
            if (value < min)
                return min;

            if (value > max)
                return max;

            return value;
        }
    }
}