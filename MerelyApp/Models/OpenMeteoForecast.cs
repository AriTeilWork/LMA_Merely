using System;
namespace MerelyApp.Models;

public class OpenMeteoForecast
{
    public string[]? Time { get; set; }
    public double[]? Temperature2MMax { get; set; }
    public double[]? Temperature2MMin { get; set; }
    public int[]? Weathercode { get; set; }
    public double[]? Windspeed10MMax { get; set; }
    public double[]? RelativeHumidity2MMax { get; set; }
    public double[]? RelativeHumidity2MMin { get; set; }
    public string[]? Sunrise { get; set; }
    public string[]? Sunset { get; set; }
    // current weather (optional)
    public double? CurrentTemperature { get; set; }
    public double? CurrentWindspeed { get; set; }
    public int? CurrentWeathercode { get; set; }
    public string? CurrentTime { get; set; }
    public bool? CurrentIsDay { get; set; }
    // presence flags for diagnostics
    public bool HasCurrent { get; set; }
    public bool HasHumidityDaily { get; set; }
    public bool HasWindspeedDaily { get; set; }
    public int DaysCount { get; set; }
}
