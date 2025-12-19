using MerelyApp.Models;
using MerelyApp.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MerelyApp;

public partial class WeatherPage : ContentPage
{
    private readonly OpenMeteoWeatherService _openMeteo;
    private bool _isLoading = false;
    private System.Threading.CancellationTokenSource? _cts;

    public WeatherPage()
    {
        InitializeComponent();
        _openMeteo = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services?.GetService(typeof(OpenMeteoWeatherService)) as OpenMeteoWeatherService ?? new OpenMeteoWeatherService();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (!_isLoading)
        {
            _cts = new System.Threading.CancellationTokenSource();
            _ = LoadWeatherAsync(_cts.Token);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        try { _cts?.Cancel(); } catch { }
        _cts?.Dispose();
        _cts = null;
    }

    private async Task LoadWeatherAsync(System.Threading.CancellationToken cancellationToken = default)
    {
        if (_isLoading) return;
        
        LocationLabel.Text = "📍 Determining location...";
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        // show loading
        _isLoading = true;
        double lat = 0, lon = 0;
        try
        {
            // Ensure permission is granted (Android/iOS)
            try
            {
                var status = await Microsoft.Maui.ApplicationModel.Permissions.CheckStatusAsync<Microsoft.Maui.ApplicationModel.Permissions.LocationWhenInUse>();
                if (status != Microsoft.Maui.ApplicationModel.PermissionStatus.Granted)
                {
                    status = await Microsoft.Maui.ApplicationModel.Permissions.RequestAsync<Microsoft.Maui.ApplicationModel.Permissions.LocationWhenInUse>();
                }

                if (status != Microsoft.Maui.ApplicationModel.PermissionStatus.Granted)
                {
                    // permission denied -> fallback
                    LocationLabel.Text = "📍 Location permission denied. Using default location (Moscow)";
                    lat = 55.7558; lon = 37.6173;
                }
            }
            catch
            {
                // permission API may fail on some platforms; continue to attempt geolocation
            }

            var loc = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10)));
            if (loc != null)
            {
                lat = loc.Latitude;
                lon = loc.Longitude;
                LocationLabel.Text = $"📍 Location: {lat:F2}°N, {lon:F2}°E";
            }
            else
            {
                LocationLabel.Text = "📍 Using default location (Moscow)";
                lat = 55.7558; lon = 37.6173;
            }
        }
        catch (Exception ex)
        {
            LocationLabel.Text = "📍 Using default location (Moscow)";
            lat = 55.7558; lon = 37.6173;
            System.Diagnostics.Debug.WriteLine($"Location error: {ex.Message}");
        }

        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        
        OpenMeteoForecast? data = null;
        try
        {
            data = await _openMeteo.GetForecastAsync(lat, lon, days: 16, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Weather service error: {ex.Message}");
        }
        
        if (data == null)
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            _isLoading = false;
            await DisplayAlert("Error", "Failed to get weather data. Please check your internet connection and try again.", "OK");
            return;
        }

        // current weather (if available)
        if (data.CurrentTemperature.HasValue)
        {
            TempLabel.Text = $"{Math.Round(data.CurrentTemperature.Value)}°C";

            // current weathercode -> icon and summary
            if (data.CurrentWeathercode.HasValue)
            {
                bool isDay = data.CurrentIsDay ?? IsDayTime(DateTime.Now);
                CurrentIconLabel.Text = WeathercodeToEmoji(data.CurrentWeathercode.Value, isDay);
                SummaryLabel.Text = HumanReadableWeathercode(data.CurrentWeathercode.Value);
            }

            // wind: prefer current_windspeed, fallback to first day's max
            if (data.CurrentWindspeed.HasValue)
            {
                WindLabel.Text = $"Wind: {Math.Round(data.CurrentWindspeed.Value)} m/s";
            }
            else if (data.Windspeed10MMax != null && data.Windspeed10MMax.Length > 0)
            {
                WindLabel.Text = $"Wind: {Math.Round(data.Windspeed10MMax[0])} m/s";
            }
            else
            {
                WindLabel.Text = "Wind: --";
            }

            // humidity: prefer daily arrays first element
            if (data.RelativeHumidity2MMax != null && data.RelativeHumidity2MMin != null && data.RelativeHumidity2MMax.Length > 0 && data.RelativeHumidity2MMin.Length > 0)
            {
                var hum = (data.RelativeHumidity2MMax[0] + data.RelativeHumidity2MMin[0]) / 2.0;
                HumidityLabel.Text = $"Humidity: {Math.Round(hum)}%";
            }
            else
            {
                HumidityLabel.Text = "Humidity: --";
            }
        }
        else
        {
            TempLabel.Text = "--°C";
            WindLabel.Text = "Wind: --";
            CurrentIconLabel.Text = "❓";
            SummaryLabel.Text = "--";
            HumidityLabel.Text = "Humidity: --";
        }

        var daysCount = data.Time?.Length ?? 0;
        var items = Enumerable.Range(0, daysCount).Select(i => new Models.ForecastItem
        {
            DayOfWeek = (data.Time != null && i < data.Time.Length && DateTime.TryParse(data.Time[i], out var date)) 
                ? date.ToString("ddd", CultureInfo.CurrentCulture) 
                : "--",
            Summary = (data.Weathercode != null && i < data.Weathercode.Length) ? HumanReadableWeathercode(data.Weathercode[i]) : "--",
            TempRange = (data.Temperature2MMin != null && data.Temperature2MMax != null && 
                        i < data.Temperature2MMin.Length && i < data.Temperature2MMax.Length &&
                        !double.IsNaN(data.Temperature2MMin[i]) && !double.IsNaN(data.Temperature2MMax[i])) 
                ? $"{Math.Round(data.Temperature2MMin[i])}° / {Math.Round(data.Temperature2MMax[i])}°" 
                : "--",
            IconEmoji = (data.Weathercode != null && i < data.Weathercode.Length) 
                ? WeathercodeToEmoji(data.Weathercode[i], IsDayForForecast(data, i)) 
                : "❓"
        }).ToList();

        ForecastList.ItemsSource = items;

        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
        _isLoading = false;

        // Debug info
        var jsonPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "openmeteo_latest.json");
        DebugInfoLabel.Text = $"hasCurrent:{data.HasCurrent} humidityDaily:{data.HasHumidityDaily} windDaily:{data.HasWindspeedDaily} days:{data.DaysCount} json:{jsonPath}";
    }

    private string GetIconUrl(string? icon)
    {
        return string.Empty; // icons not provided by OpenMeteo; could map weathercode to local icons
    }

    private bool IsDayTime(DateTime time)
    {
        // Simple heuristic: 6 AM to 8 PM is day
        int hour = time.Hour;
        return hour >= 6 && hour < 20;
    }

    private bool IsDayForForecast(OpenMeteoForecast data, int index)
    {
        // Try to determine if it's day based on sunrise/sunset
        if (data.Sunrise != null && data.Sunset != null && 
            index < data.Sunrise.Length && index < data.Sunset.Length &&
            DateTime.TryParse(data.Sunrise[index], out var sunrise) &&
            DateTime.TryParse(data.Sunset[index], out var sunset))
        {
            var now = DateTime.Now;
            var today = now.Date;
            var sunriseToday = today.Add(sunrise.TimeOfDay);
            var sunsetToday = today.Add(sunset.TimeOfDay);
            
            return now >= sunriseToday && now < sunsetToday;
        }
        
        // Fallback: use current time heuristic
        return IsDayTime(DateTime.Now);
    }

    private string WeathercodeToEmoji(int code, bool isDay = true)
    {
        // WMO weather code to emoji mapping with day/night variants
        if (isDay)
        {
            return code switch
            {
                0 => "☀️", // Clear sky
                1 => "🌤️", // Mainly clear
                2 => "⛅", // Partly cloudy
                3 => "☁️", // Overcast
                45 => "🌫️", // Fog
                48 => "🌫️", // Depositing rime fog
                51 => "🌦️", // Light drizzle
                53 => "🌦️", // Moderate drizzle
                55 => "🌦️", // Dense drizzle
                56 => "🌨️", // Light freezing drizzle
                57 => "🌨️", // Dense freezing drizzle
                61 => "🌧️", // Slight rain
                63 => "🌧️", // Moderate rain
                65 => "🌧️", // Heavy rain
                66 => "🌨️", // Light freezing rain
                67 => "🌨️", // Heavy freezing rain
                71 => "❄️", // Slight snow fall
                73 => "❄️", // Moderate snow fall
                75 => "❄️", // Heavy snow fall
                77 => "🌨️", // Snow grains
                80 => "🌦️", // Slight rain showers
                81 => "🌦️", // Moderate rain showers
                82 => "🌧️", // Violent rain showers
                85 => "🌨️", // Slight snow showers
                86 => "🌨️", // Heavy snow showers
                95 => "⛈️", // Thunderstorm
                96 => "⛈️", // Thunderstorm with slight hail
                99 => "⛈️", // Thunderstorm with heavy hail
                _ => "❓" // Unknown
            };
        }
        else
        {
            // Night variants - using night-appropriate emojis
            return code switch
            {
                0 => "🌙", // Clear sky - moon
                1 => "🌙", // Mainly clear - moon (slightly cloudy)
                2 => "☁️🌙", // Partly cloudy - cloud with moon
                3 => "☁️", // Overcast - same for day/night
                45 => "🌫️", // Fog - same
                48 => "🌫️", // Depositing rime fog - same
                51 => "🌧️", // Light drizzle - rain cloud
                53 => "🌧️", // Moderate drizzle
                55 => "🌧️", // Dense drizzle
                56 => "🌨️", // Light freezing drizzle
                57 => "🌨️", // Dense freezing drizzle
                61 => "🌧️", // Slight rain
                63 => "🌧️", // Moderate rain
                65 => "🌧️", // Heavy rain
                66 => "🌨️", // Light freezing rain
                67 => "🌨️", // Heavy freezing rain
                71 => "❄️", // Slight snow fall
                73 => "❄️", // Moderate snow fall
                75 => "❄️", // Heavy snow fall
                77 => "🌨️", // Snow grains
                80 => "🌧️", // Slight rain showers
                81 => "🌧️", // Moderate rain showers
                82 => "🌧️", // Violent rain showers
                85 => "🌨️", // Slight snow showers
                86 => "🌨️", // Heavy snow showers
                95 => "⛈️", // Thunderstorm
                96 => "⛈️", // Thunderstorm with slight hail
                99 => "⛈️", // Thunderstorm with heavy hail
                _ => "🌙" // Unknown - default to moon at night
            };
        }
    }

    private string HumanReadableWeathercode(int code)
    {
        return code switch
        {
            0 => "Clear sky",
            1 => "Mainly clear",
            2 => "Partly cloudy",
            3 => "Overcast",
            45 => "Fog",
            48 => "Depositing rime fog",
            51 => "Drizzle: Light",
            53 => "Drizzle: Moderate",
            55 => "Drizzle: Dense",
            61 => "Rain: Slight",
            63 => "Rain: Moderate",
            65 => "Rain: Heavy",
            71 => "Snow: Slight",
            73 => "Snow: Moderate",
            75 => "Snow: Heavy",
            80 => "Rain showers: Slight",
            81 => "Rain showers: Moderate",
            82 => "Rain showers: Violent",
            >= 95 => "Thunderstorm",
            _ => "Unknown"
        };
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await LoadWeatherAsync();
    }
}
