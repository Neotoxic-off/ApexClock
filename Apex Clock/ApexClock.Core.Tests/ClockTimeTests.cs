using ApexClock.Core;

namespace ApexClock.Core.Tests;

public sealed class ClockTimeTests
{
    [Theory]
    [InlineData("19", "19:00")]
    [InlineData("19h", "19:00")]
    [InlineData("19h30", "19:30")]
    [InlineData("19H30", "19:30")]
    [InlineData("19:05", "19:05")]
    [InlineData("19.05", "19:05")]
    [InlineData("1930", "19:30")]
    [InlineData("930", "09:30")]
    [InlineData(" 0:00 ", "00:00")]
    [InlineData("23:59", "23:59")]
    public void TryParse_accepts_common_notations(string text, string expected)
    {
        bool parsed = ClockTime.TryParse(text, out ClockTime time);

        Assert.True(parsed);
        Assert.Equal(expected, time.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("24:00")]
    [InlineData("19:60")]
    [InlineData("-1")]
    [InlineData("abc")]
    [InlineData("1:2:3")]
    public void TryParse_rejects_invalid_times(string? text)
    {
        Assert.False(ClockTime.TryParse(text, out ClockTime _));
    }

    [Theory]
    [InlineData(600, 870, 270)]
    [InlineData(1410, 240, 270)]
    [InlineData(600, 600, ClockTime.MinutesPerDay)]
    public void MinutesUntil_wraps_past_midnight(int from, int to, int expected)
    {
        Assert.Equal(expected, new ClockTime(from).MinutesUntil(new ClockTime(to)));
    }

    [Theory]
    [InlineData(1410, 270, 240)]
    [InlineData(30, -60, 1410)]
    public void AddMinutes_wraps_around_the_day(int from, int minutes, int expected)
    {
        Assert.Equal(new ClockTime(expected), new ClockTime(from).AddMinutes(minutes));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(ClockTime.MinutesPerDay)]
    public void Constructor_rejects_out_of_range_minutes(int minuteOfDay)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ClockTime(minuteOfDay));
    }
}
