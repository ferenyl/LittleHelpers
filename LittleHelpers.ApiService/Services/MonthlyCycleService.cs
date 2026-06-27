using System.ComponentModel.DataAnnotations;
using LittleHelpers.ApiService.Application.Cqrs;
using Microsoft.Extensions.Options;

namespace LittleHelpers.ApiService.Services;

public sealed class MonthlyCycleOptions
{
    public const string SectionName = "MonthlyCycle";

    [Range(1, 31)]
    public int BreakpointDay { get; init; } = 1;
}

public readonly record struct MonthlyCyclePeriod(
    int Year,
    int Month,
    DateTimeOffset StartInclusive,
    DateTimeOffset EndExclusive);

public interface IMonthlyCycleService
{
    MonthlyCyclePeriod GetCurrentPeriod(DateTimeOffset nowUtc);
    MonthlyCyclePeriod GetPeriodForMonth(int year, int month);
}

public sealed class MonthlyCycleService(IOptions<MonthlyCycleOptions> options) : IMonthlyCycleService
{
    private readonly int _breakpointDay = options.Value.BreakpointDay;

    public MonthlyCyclePeriod GetCurrentPeriod(DateTimeOffset nowUtc)
    {
        var normalizedNow = nowUtc.ToUniversalTime();
        var currentMonthStart = CreateCycleStart(normalizedNow.Year, normalizedNow.Month);
        var (previousYear, previousMonth) = GetPreviousMonth(normalizedNow.Year, normalizedNow.Month);
        var periodStart = normalizedNow < currentMonthStart
            ? CreateCycleStart(previousYear, previousMonth)
            : currentMonthStart;

        return CreatePeriod(periodStart.Year, periodStart.Month);
    }

    public MonthlyCyclePeriod GetPeriodForMonth(int year, int month) =>
        CreatePeriod(year, month);

    private DateTimeOffset CreateCycleStart(int year, int month)
    {
        if (year is < 1 or > 9999)
            throw new RequestValidationException("Year must be between 1 and 9999.");

        if (month is < 1 or > 12)
            throw new RequestValidationException("Month must be between 1 and 12.");

        var day = Math.Min(_breakpointDay, DateTime.DaysInMonth(year, month));
        return new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero);
    }

    private MonthlyCyclePeriod CreatePeriod(int year, int month)
    {
        var periodStart = CreateCycleStart(year, month);
        var (nextYear, nextMonth) = GetNextMonth(year, month);
        var periodEnd = CreateCycleStart(nextYear, nextMonth);
        return new MonthlyCyclePeriod(
            periodStart.Year,
            periodStart.Month,
            periodStart,
            periodEnd);
    }

    private static (int Year, int Month) GetPreviousMonth(int year, int month) =>
        month == 1 ? (year - 1, 12) : (year, month - 1);

    private static (int Year, int Month) GetNextMonth(int year, int month) =>
        month == 12 ? (year + 1, 1) : (year, month + 1);
}
