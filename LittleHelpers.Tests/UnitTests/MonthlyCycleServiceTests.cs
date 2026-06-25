using LittleHelpers.ApiService.Application.Cqrs;
using LittleHelpers.ApiService.Services;
using Microsoft.Extensions.Options;

namespace LittleHelpers.Tests.UnitTests;

public class MonthlyCycleServiceTests
{
    private static MonthlyCycleService CreateService(int breakpointDay) =>
        new(Options.Create(new MonthlyCycleOptions { BreakpointDay = breakpointDay }));

    [Fact]
    public void GetCurrentPeriod_BeforeBreakpoint_UsesPreviousMonthStart()
    {
        var service = CreateService(27);

        var period = service.GetCurrentPeriod(new DateTimeOffset(2026, 2, 10, 12, 0, 0, TimeSpan.Zero));

        Assert.Equal(new DateTimeOffset(2026, 1, 27, 0, 0, 0, TimeSpan.Zero), period.StartInclusive);
        Assert.Equal(new DateTimeOffset(2026, 2, 27, 0, 0, 0, TimeSpan.Zero), period.EndExclusive);
        Assert.Equal(2026, period.Year);
        Assert.Equal(1, period.Month);
    }

    [Fact]
    public void GetCurrentPeriod_OnBreakpoint_UsesCurrentMonthStart()
    {
        var service = CreateService(27);

        var period = service.GetCurrentPeriod(new DateTimeOffset(2026, 2, 27, 12, 0, 0, TimeSpan.Zero));

        Assert.Equal(new DateTimeOffset(2026, 2, 27, 0, 0, 0, TimeSpan.Zero), period.StartInclusive);
        Assert.Equal(new DateTimeOffset(2026, 3, 27, 0, 0, 0, TimeSpan.Zero), period.EndExclusive);
        Assert.Equal(2026, period.Year);
        Assert.Equal(2, period.Month);
    }

    [Fact]
    public void GetPeriodForMonth_UsesDisplayMonthWithConfiguredBreakpoint()
    {
        var service = CreateService(27);

        var period = service.GetPeriodForMonth(2026, 6);

        Assert.Equal(new DateTimeOffset(2026, 5, 27, 0, 0, 0, TimeSpan.Zero), period.StartInclusive);
        Assert.Equal(new DateTimeOffset(2026, 6, 27, 0, 0, 0, TimeSpan.Zero), period.EndExclusive);
        Assert.Equal(2026, period.Year);
        Assert.Equal(6, period.Month);
    }

    [Fact]
    public void GetPeriodForMonth_InvalidMonth_ThrowsValidationException()
    {
        var service = CreateService(27);

        var exception = Assert.Throws<RequestValidationException>(() => service.GetPeriodForMonth(2026, 13));
        Assert.Equal("Month must be between 1 and 12.", exception.Message);
    }

    [Fact]
    public void GetPeriodForMonth_Breakpoint31_FallsBackToLastDayInFebruary()
    {
        var service = CreateService(31);

        var period = service.GetPeriodForMonth(2026, 3);

        Assert.Equal(new DateTimeOffset(2026, 2, 28, 0, 0, 0, TimeSpan.Zero), period.StartInclusive);
        Assert.Equal(new DateTimeOffset(2026, 3, 31, 0, 0, 0, TimeSpan.Zero), period.EndExclusive);
    }

    [Fact]
    public void GetPeriodForMonth_Breakpoint31_FallsBackToLeapDayInLeapYear()
    {
        var service = CreateService(31);

        var period = service.GetPeriodForMonth(2028, 3);

        Assert.Equal(new DateTimeOffset(2028, 2, 29, 0, 0, 0, TimeSpan.Zero), period.StartInclusive);
        Assert.Equal(new DateTimeOffset(2028, 3, 31, 0, 0, 0, TimeSpan.Zero), period.EndExclusive);
    }

    [Fact]
    public void GetPeriodForMonth_Breakpoint1_UsesCalendarMonth()
    {
        var service = CreateService(1);

        var period = service.GetPeriodForMonth(2026, 6);

        Assert.Equal(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero), period.StartInclusive);
        Assert.Equal(new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero), period.EndExclusive);
        Assert.Equal(2026, period.Year);
        Assert.Equal(6, period.Month);
    }
}
