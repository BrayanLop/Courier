using CourierMax.Application.Services;
using FluentAssertions;
using Xunit;

namespace CourierMax.Tests;

public class BusinessDayCalculatorTests
{
    private readonly BusinessDayCalculator _calculator = new();

    [Fact]
    public void AddBusinessDays_FridayPlusOne_LandsOnMonday()
    {
        // RN-02: viernes + 1 dia habil = lunes (no cuenta sabado ni domingo).
        var friday = new DateTime(2026, 6, 19); // viernes

        var result = _calculator.AddBusinessDays(friday, 1);

        result.Should().Be(new DateTime(2026, 6, 22)); // lunes
    }

    [Fact]
    public void AddBusinessDays_SkipsColombianHoliday()
    {
        // 29 de junio de 2026 (lunes) es festivo; partiendo del viernes 26,
        // un dia habil debe caer el martes 30.
        var friday = new DateTime(2026, 6, 26);

        var result = _calculator.AddBusinessDays(friday, 1);

        result.Should().Be(new DateTime(2026, 6, 30));
    }

    [Theory]
    [InlineData(2026, 6, 20, false)] // sabado
    [InlineData(2026, 6, 21, false)] // domingo
    [InlineData(2026, 7, 20, false)] // festivo
    [InlineData(2026, 6, 22, true)]  // lunes habil
    public void IsBusinessDay_EvaluatesWeekendsAndHolidays(int y, int m, int d, bool expected)
    {
        _calculator.IsBusinessDay(new DateTime(y, m, d)).Should().Be(expected);
    }

    [Fact]
    public void BusinessDaysBetween_ExcludesWeekends()
    {
        // De viernes 19 a lunes 22: solo el lunes cuenta como dia habil transcurrido.
        var count = _calculator.BusinessDaysBetween(new DateTime(2026, 6, 19), new DateTime(2026, 6, 22));

        count.Should().Be(1);
    }
}
