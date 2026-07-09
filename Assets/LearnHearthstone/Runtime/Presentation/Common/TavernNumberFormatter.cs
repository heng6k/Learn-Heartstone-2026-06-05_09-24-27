using System.Globalization;

namespace LearnHearthstone.Presentation.Common
{
    public static class TavernNumberFormatter
    {
        private const long Wan = 10000L;
        private const long Yi = 100000000L;

        public static string CompactStat(int value)
        {
            var negative = value < 0;
            var absolute = negative ? -(long)value : (long)value;
            var prefix = negative ? "-" : string.Empty;

            if (absolute >= Yi)
            {
                return prefix + CompactUnit(absolute, Yi, "亿");
            }

            if (absolute >= Wan)
            {
                return prefix + CompactUnit(absolute, Wan, "万");
            }

            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string CompactStats(int attack, int health)
        {
            return CompactStat(attack) + "/" + CompactStat(health);
        }

        public static string CompactStatsWithMax(int attack, int health, int maxHealth)
        {
            return CompactStat(attack) + "/" + CompactStat(health) + "/" + CompactStat(maxHealth);
        }

        public static string FullNumber(int value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }

        public static string FullStats(int attack, int health)
        {
            return FullNumber(attack) + " / " + FullNumber(health);
        }

        private static string CompactUnit(long absolute, long unit, string suffix)
        {
            var scaledTenths = absolute * 10L / unit;
            var whole = scaledTenths / 10L;
            var tenth = scaledTenths % 10L;
            return tenth == 0L
                ? whole.ToString(CultureInfo.InvariantCulture) + suffix
                : whole.ToString(CultureInfo.InvariantCulture) + "." + tenth.ToString(CultureInfo.InvariantCulture) + suffix;
        }
    }
}
