using Octaryn.Shared.Time;

namespace Octaryn.Server.World.Time;

internal static class WorldTimeCalendar
{
    public static bool IsLeapYear(int year)
    {
        if (year % 4 != 0)
        {
            return false;
        }

        if (year % 100 != 0)
        {
            return true;
        }

        return year % 400 == 0;
    }

    public static int DaysInMonth(int year, int month)
    {
        ReadOnlySpan<int> daysPerMonth = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
        if (month is < 1 or > 12)
        {
            return 31;
        }

        if (month == 2 && IsLeapYear(year))
        {
            return 29;
        }

        return daysPerMonth[month - 1];
    }

    public static long DaysFromCivil(int year, uint month, uint day)
    {
        year -= month <= 2 ? 1 : 0;
        var era = (year >= 0 ? year : year - 399) / 400;
        var yearOfEra = (uint)(year - era * 400);
        var monthOffset = month > 2 ? (int)month - 3 : (int)month + 9;
        var dayOfYear = (uint)((153 * monthOffset + 2) / 5) + day - 1;
        var dayOfEra = yearOfEra * 365 + yearOfEra / 4 - yearOfEra / 100 + dayOfYear;
        return (long)era * 146097L + dayOfEra - 719468L;
    }

    public static WorldDate CivilFromDays(long day)
    {
        day += 719468L;
        var era = (day >= 0 ? day : day - 146096L) / 146097L;
        var dayOfEra = (uint)(day - era * 146097L);
        var yearOfEra = (dayOfEra - dayOfEra / 1460 + dayOfEra / 36524 - dayOfEra / 146096) / 365;
        var year = (int)yearOfEra + (int)era * 400;
        var dayOfYear = dayOfEra - (365 * yearOfEra + yearOfEra / 4 - yearOfEra / 100);
        var monthPrime = (5 * dayOfYear + 2) / 153;
        var civilDay = dayOfYear - (153 * monthPrime + 2) / 5 + 1;
        var civilMonth = monthPrime < 10 ? monthPrime + 3 : monthPrime - 9;
        return new WorldDate(
            year + (civilMonth <= 2 ? 1 : 0),
            (int)civilMonth,
            (int)civilDay);
    }
}
