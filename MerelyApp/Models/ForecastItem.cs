using System;

namespace MerelyApp.Models;

public class ForecastItem
{
    public string? DayOfWeek { get; set; }
    public string? Summary { get; set; }
    public string? TempRange { get; set; }
    public string? IconUrl { get; set; }
    public string? IconEmoji { get; set; }
}
