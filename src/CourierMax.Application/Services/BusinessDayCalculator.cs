namespace CourierMax.Application.Services;

/// <summary>
/// Implementa el calculo de dias habiles (RN-02): excluye sabados, domingos y
/// los festivos colombianos. El conjunto de festivos se inyecta para poder
/// extenderlo a otros anios sin modificar la logica.
/// </summary>
public sealed class BusinessDayCalculator : IBusinessDayCalculator
{
    private readonly HashSet<DateTime> _holidays;

    /// <summary>Festivos colombianos 2026 segun el enunciado.</summary>
    public static readonly IReadOnlyList<DateTime> ColombianHolidays2026 = new[]
    {
        new DateTime(2026, 1, 1),
        new DateTime(2026, 1, 26),
        new DateTime(2026, 1, 30),
        new DateTime(2026, 3, 24),
        new DateTime(2026, 5, 1),
        new DateTime(2026, 6, 1),
        new DateTime(2026, 6, 29),
        new DateTime(2026, 7, 20),
        new DateTime(2026, 8, 17),
        new DateTime(2026, 10, 20),
        new DateTime(2026, 11, 9),
        new DateTime(2026, 12, 8)
    };

    public BusinessDayCalculator() : this(ColombianHolidays2026) { }

    public BusinessDayCalculator(IEnumerable<DateTime> holidays)
    {
        _holidays = holidays.Select(d => d.Date).ToHashSet();
    }

    public bool IsBusinessDay(DateTime date)
    {
        var day = date.Date;
        if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return false;
        return !_holidays.Contains(day);
    }

    public DateTime AddBusinessDays(DateTime start, int businessDays)
    {
        if (businessDays < 0)
            throw new ArgumentOutOfRangeException(nameof(businessDays), "No se admiten dias habiles negativos.");

        var date = start.Date;
        var added = 0;
        while (added < businessDays)
        {
            date = date.AddDays(1);
            if (IsBusinessDay(date))
                added++;
        }

        return date;
    }

    public int BusinessDaysBetween(DateTime start, DateTime end)
    {
        if (end.Date <= start.Date)
            return 0;

        var count = 0;
        var cursor = start.Date;
        while (cursor < end.Date)
        {
            cursor = cursor.AddDays(1);
            if (IsBusinessDay(cursor))
                count++;
        }

        return count;
    }
}
