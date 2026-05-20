namespace AchWorker.Services;

public static class BankingCalendar
{
    public static TimeSpan GetReturnWindow(DateTime from, bool isSameDayAch)
    {
        var bankingDays = isSameDayAch ? 1 : 2;
        var current = from.Date;
        var added = 0;
        while (added < bankingDays)
        {
            current = current.AddDays(1);
            if (!IsWeekend(current) && !IsFedHoliday(current))
                added++;
        }
        var windowEnd = current.AddDays(1).AddSeconds(-1);
        return windowEnd - from;
    }

    private static bool IsWeekend(DateTime d) =>
        d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    private static bool IsFedHoliday(DateTime d)
    {
        var year = d.Year;
        var holidays = new[]
        {
            ObservedDate(new DateTime(year, 1, 1)),
            NthWeekday(year, 1, DayOfWeek.Monday, 3),
            NthWeekday(year, 2, DayOfWeek.Monday, 3),
            LastWeekday(year, 5, DayOfWeek.Monday),
            ObservedDate(new DateTime(year, 6, 19)),
            ObservedDate(new DateTime(year, 7, 4)),
            NthWeekday(year, 9, DayOfWeek.Monday, 1),
            NthWeekday(year, 10, DayOfWeek.Monday, 2),
            ObservedDate(new DateTime(year, 11, 11)),
            NthWeekday(year, 11, DayOfWeek.Thursday, 4),
            ObservedDate(new DateTime(year, 12, 25)),
        };
        return holidays.Any(h => h.Date == d.Date);
    }

    private static DateTime ObservedDate(DateTime holiday)
    {
        if (holiday.DayOfWeek == DayOfWeek.Saturday) return holiday.AddDays(-1);
        if (holiday.DayOfWeek == DayOfWeek.Sunday) return holiday.AddDays(1);
        return holiday;
    }

    private static DateTime NthWeekday(int year, int month, DayOfWeek dow, int n)
    {
        var d = new DateTime(year, month, 1);
        while (d.DayOfWeek != dow) d = d.AddDays(1);
        return d.AddDays(7 * (n - 1));
    }

    private static DateTime LastWeekday(int year, int month, DayOfWeek dow)
    {
        var d = new DateTime(year, month + 1, 1).AddDays(-1);
        while (d.DayOfWeek != dow) d = d.AddDays(-1);
        return d;
    }
}
