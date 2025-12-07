using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MerelyApp.Models;

public class OpenWeatherResponse
{
    // One Call API
    [JsonPropertyName("current")]
    public CurrentWeather? Current { get; set; }

    [JsonPropertyName("daily")]
    public List<DailyWeather>? Daily { get; set; }

    // Forecast 16-day API returns 'list' property
    [JsonPropertyName("list")]
    public List<DailyWeather>? List { get; set; }
}

public class CurrentWeather
{
    [JsonPropertyName("dt")] public long Dt { get; set; }
    [JsonPropertyName("temp")] public double? Temp { get; set; }
    [JsonPropertyName("humidity")] public int? Humidity { get; set; }
    [JsonPropertyName("wind_speed")] public double? WindSpeed { get; set; }
    [JsonPropertyName("weather")] public List<WeatherDescription>? Weather { get; set; }
}

public class DailyWeather
{
    [JsonPropertyName("dt")] public long Dt { get; set; }
    [JsonPropertyName("temp")] public Temperature? Temp { get; set; }
    [JsonPropertyName("weather")] public List<WeatherDescription>? Weather { get; set; }
    [JsonPropertyName("humidity")] public int? Humidity { get; set; }
    // Some APIs use 'speed' for wind speed
    [JsonPropertyName("speed")] public double? WindSpeed { get; set; }
}

public class Temperature
{
    // forecast/daily provides multiple temp fields; we only need min/max
    [JsonPropertyName("min")] public double? Min { get; set; }
    [JsonPropertyName("max")] public double? Max { get; set; }
}

public class WeatherDescription
{
    [JsonPropertyName("main")] public string? Main { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("icon")] public string? Icon { get; set; }
}
