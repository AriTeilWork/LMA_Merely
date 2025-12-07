using MerelyApp.Models;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace MerelyApp.Services;

public class WeatherService
{
    private readonly HttpClient _http = new HttpClient();
    private const string ApiKey = "YOUR_API_KEY_HERE"; // Replace with your OpenWeatherMap API key

    // Gets weather. If days <= 7 uses One Call API daily; if days > 7 uses forecast/daily (16 days) endpoint
    public async Task<OpenWeatherResponse?> GetWeatherAsync(double latitude, double longitude, int days = 7)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if (days <= 7)
            {
                string url = $"https://api.openweathermap.org/data/2.5/onecall?lat={latitude}&lon={longitude}&units=metric&exclude=minutely,hourly,alerts&appid={ApiKey}&lang=ru";
                var resp = await _http.GetAsync(url);
                if (!resp.IsSuccessStatusCode) return null;
                var json = await resp.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<OpenWeatherResponse>(json, options);
                return data;
            }
            else
            {
                // Forecast 16-day (daily) API
                // Note: endpoint and parameters may require subscription/key features
                string url = $"https://api.openweathermap.org/data/2.5/forecast/daily?lat={latitude}&lon={longitude}&cnt={Math.Min(days,16)}&units=metric&appid={ApiKey}&lang=ru";
                var resp = await _http.GetAsync(url);
                if (!resp.IsSuccessStatusCode) return null;
                var json = await resp.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<OpenWeatherResponse>(json, options);
                return data;
            }
        }
        catch
        {
            return null;
        }
    }
}
