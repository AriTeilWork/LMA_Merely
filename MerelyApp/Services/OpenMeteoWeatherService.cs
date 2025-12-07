using OpenMeteo;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MerelyApp.Models;

namespace MerelyApp.Services;

public class OpenMeteoWeatherService
{
    private static readonly HttpClient _http = new HttpClient()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public async Task<OpenMeteoForecast?> GetForecastAsync(double latitude, double longitude, int days = 7)
    {
        try
        {
            int cnt = Math.Min(days, 16);
            string fields = "temperature_2m_max,temperature_2m_min,weather_code,wind_speed_10m_max,relative_humidity_2m_max,relative_humidity_2m_min,sunrise,sunset";
            string url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&daily={fields}&forecast_days={cnt}&timezone=auto&current_weather=true&hourly=is_day";
            
            var resp = await _http.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return null;
            var json = await resp.Content.ReadAsStringAsync();
            // save JSON for debugging
            try { System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "openmeteo_latest.json"), json); } catch {}
            JsonDocument? doc = null;
            try
            {
                doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("daily", out var daily)) return null;

                var model = new OpenMeteoForecast();

                if (daily.TryGetProperty("time", out var time) && time.ValueKind == JsonValueKind.Array)
                {
                    model.Time = time.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.String)
                        .Select(e => e.GetString() ?? string.Empty)
                        .ToArray();
                }
                if (daily.TryGetProperty("temperature_2m_max", out var tmax) && tmax.ValueKind == JsonValueKind.Array)
                {
                    model.Temperature2MMax = tmax.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.Number)
                        .Select(e => e.TryGetDouble(out var val) ? val : double.NaN)
                        .Where(v => !double.IsNaN(v))
                        .ToArray();
                }
                if (daily.TryGetProperty("temperature_2m_min", out var tmin) && tmin.ValueKind == JsonValueKind.Array)
                {
                    model.Temperature2MMin = tmin.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.Number)
                        .Select(e => e.TryGetDouble(out var val) ? val : double.NaN)
                        .Where(v => !double.IsNaN(v))
                        .ToArray();
                }
                // weather code - API returns as 'weather_code'
                if (daily.TryGetProperty("weather_code", out var wcode) && wcode.ValueKind == JsonValueKind.Array)
                {
                    model.Weathercode = wcode.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.Number)
                        .Select(e => e.TryGetInt32(out var val) ? val : 0)
                        .ToArray();
                }
                if (daily.TryGetProperty("wind_speed_10m_max", out var wwind) && wwind.ValueKind == JsonValueKind.Array)
                {
                    model.Windspeed10MMax = wwind.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.Number)
                        .Select(e => e.TryGetDouble(out var val) ? val : double.NaN)
                        .Where(v => !double.IsNaN(v))
                        .ToArray();
                }
                if (daily.TryGetProperty("relative_humidity_2m_max", out var rhmax) && rhmax.ValueKind == JsonValueKind.Array)
                {
                    model.RelativeHumidity2MMax = rhmax.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.Number)
                        .Select(e => e.TryGetDouble(out var val) ? val : double.NaN)
                        .Where(v => !double.IsNaN(v))
                        .ToArray();
                }
                if (daily.TryGetProperty("relative_humidity_2m_min", out var rhmin) && rhmin.ValueKind == JsonValueKind.Array)
                {
                    model.RelativeHumidity2MMin = rhmin.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.Number)
                        .Select(e => e.TryGetDouble(out var val) ? val : double.NaN)
                        .Where(v => !double.IsNaN(v))
                        .ToArray();
                }
                if (daily.TryGetProperty("sunrise", out var sunrise) && sunrise.ValueKind == JsonValueKind.Array)
                {
                    model.Sunrise = sunrise.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.String)
                        .Select(e => e.GetString() ?? string.Empty)
                        .ToArray();
                }
                if (daily.TryGetProperty("sunset", out var sunset) && sunset.ValueKind == JsonValueKind.Array)
                {
                    model.Sunset = sunset.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.String)
                        .Select(e => e.GetString() ?? string.Empty)
                        .ToArray();
                }

                // current_weather block
                if (doc.RootElement.TryGetProperty("current_weather", out var current) && current.ValueKind == JsonValueKind.Object)
                {
                    if (current.TryGetProperty("temperature", out var ct) && ct.ValueKind == JsonValueKind.Number && ct.TryGetDouble(out var temp))
                        model.CurrentTemperature = temp;
                    if (current.TryGetProperty("wind_speed", out var cw) && cw.ValueKind == JsonValueKind.Number && cw.TryGetDouble(out var wind))
                        model.CurrentWindspeed = wind;
                    else if (current.TryGetProperty("windspeed", out cw) && cw.ValueKind == JsonValueKind.Number && cw.TryGetDouble(out wind))
                        model.CurrentWindspeed = wind;
                    if (current.TryGetProperty("weather_code", out var cc) && cc.ValueKind == JsonValueKind.Number && cc.TryGetInt32(out var code))
                        model.CurrentWeathercode = code;
                    else if (current.TryGetProperty("weathercode", out cc) && cc.ValueKind == JsonValueKind.Number && cc.TryGetInt32(out code))
                        model.CurrentWeathercode = code;
                    if (current.TryGetProperty("time", out var ctime) && ctime.ValueKind == JsonValueKind.String)
                        model.CurrentTime = ctime.GetString();
                    if (current.TryGetProperty("is_day", out var isDay) && isDay.ValueKind == JsonValueKind.Number && isDay.TryGetInt32(out var dayValue))
                        model.CurrentIsDay = dayValue == 1;
                }
                
                // Try to get is_day from hourly data if not in current_weather
                if (!model.CurrentIsDay.HasValue && doc.RootElement.TryGetProperty("hourly", out var hourly) && hourly.ValueKind == JsonValueKind.Object)
                {
                    if (hourly.TryGetProperty("is_day", out var hourlyIsDay) && hourlyIsDay.ValueKind == JsonValueKind.Array)
                    {
                        var isDayArray = hourlyIsDay.EnumerateArray()
                            .Where(e => e.ValueKind == JsonValueKind.Number)
                            .Select(e => e.TryGetInt32(out var val) ? val : 0)
                            .ToArray();
                        if (isDayArray.Length > 0)
                        {
                            // Use the most recent value (last in array)
                            model.CurrentIsDay = isDayArray[isDayArray.Length - 1] == 1;
                        }
                    }
                }

                model.HasCurrent = model.CurrentTemperature.HasValue || model.CurrentWindspeed.HasValue || model.CurrentWeathercode.HasValue;
                model.HasHumidityDaily = model.RelativeHumidity2MMax != null && model.RelativeHumidity2MMin != null && 
                                         model.RelativeHumidity2MMax.Length > 0 && model.RelativeHumidity2MMin.Length > 0;
                model.HasWindspeedDaily = model.Windspeed10MMax != null && model.Windspeed10MMax.Length > 0;
                model.DaysCount = model.Time?.Length ?? 0;

                return model;
            }
            finally
            {
                doc?.Dispose();
            }
        }
        catch (System.Net.Http.HttpRequestException)
        {
            // Network error - return null to indicate failure
            return null;
        }
        catch (System.Threading.Tasks.TaskCanceledException)
        {
            // Timeout - return null to indicate failure
            return null;
        }
        catch (Exception)
        {
            // Other errors - return null to indicate failure
            return null;
        }
    }
}
