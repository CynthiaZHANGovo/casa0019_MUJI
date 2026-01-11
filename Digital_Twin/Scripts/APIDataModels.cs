using System;
using System.Collections.Generic;

/// <summary>
/// Data models matching actual API structures
/// </summary>

// ═══════════════════════════════════════════════════════════
//                  BUS API DATA MODELS (RAW FORMAT)
// ═══════════════════════════════════════════════════════════

/// <summary>
/// Raw bus arrival item returned from API
/// Example:
/// {"route":"108","arrival_london_aquatics":"00:00","arrival_stratford_city":"00:02","recommended":false}
/// </summary>
[System.Serializable]
public class BusArrivalAPIItem
{
    public string route;                        // Route code: "108" or "339"
    public string arrival_london_aquatics;      // Arrival time at London Aquatics
    public string arrival_stratford_city;       // Arrival time at Stratford City
    public bool recommended;                    // Whether recommended
}

/// <summary>
/// Converted bus arrival info for UI display
/// </summary>
[System.Serializable]
public class BusArrivalItem
{
    public string departureTime;                // Departure time "06:39"
    public string stopName;                     // Stop name
    public int minutesUntilArrival;             // Minutes until arrival
    public string status;                       // Status "ON TIME" / "DELAYED"
    public string destination;                  // Destination
}

// ═══════════════════════════════════════════════════════════
//                  WEATHER API DATA MODELS (RAW FORMAT)
// ═══════════════════════════════════════════════════════════

/// <summary>
/// Raw weather data returned from API
/// Example:
/// {"temperatureC":7.1,"condition":"cloudy","walkingTimeMinutes":5.33,"walkingTimeSeconds":320,"weatherIndex":1,"tempBucketIndex":2,"segmentIndex":8}
/// </summary>
[System.Serializable]
public class WeatherAPIData
{
    public float temperatureC;                  // Temperature (Celsius)
    public string condition;                    // Weather condition string
    public float walkingTimeMinutes;            // Walking duration in minutes
    public int walkingTimeSeconds;              // Walking duration in seconds
    public int weatherIndex;                    // Weather category index
    public int tempBucketIndex;                 // Temperature bucket index
    public int segmentIndex;                    // Route segment index
}

/// <summary>
/// Converted weather data for UI display
/// </summary>
[System.Serializable]
public class WeatherCurrent
{
    public float temperature;                   // Temperature
    public float feelsLike;                     // Approximate feels-like temperature
    public float windSpeed;                     // Wind speed
    public int humidity;                        // Humidity
    public float visibility;                    // Visibility in km
    public string condition;                    // Weather condition text
    public string weatherType;                  // Type used to switch FX/UI theme
    public float walkingTimeMinutes;            // Walking duration

    /// <summary>
    /// Convert raw API data into UI-friendly format
    /// </summary>
    public static WeatherCurrent FromAPIData(WeatherAPIData apiData)
    {
        return new WeatherCurrent
        {
            temperature = apiData.temperatureC,
            feelsLike = apiData.temperatureC - 2f, // Simple estimation for feels-like temperature
            windSpeed = 0f,                        // Placeholder (API does not provide)
            humidity = 0,                          // Placeholder (API does not provide)
            visibility = 10f,                      // Default assumed visibility in km
            condition = FormatCondition(apiData.condition),
            weatherType = MapWeatherType(apiData.condition),
            walkingTimeMinutes = apiData.walkingTimeMinutes
        };
    }

    /// <summary>
    /// Format condition text with capitalized first letter
    /// </summary>
    static string FormatCondition(string condition)
    {
        if (string.IsNullOrEmpty(condition)) return "Clear";

        return char.ToUpper(condition[0]) + condition.Substring(1).ToLower();
    }

    /// <summary>
    /// Map raw condition text to weather type used for UI/FX
    /// </summary>
    static string MapWeatherType(string condition)
    {
        if (string.IsNullOrEmpty(condition)) return "Clear";

        string lower = condition.ToLower();

        if (lower.Contains("sun") || lower.Contains("clear"))
            return "Sunny";
        else if (lower.Contains("rain") || lower.Contains("shower") || lower.Contains("drizzle"))
            return "Rainy";
        else if (lower.Contains("cloud") || lower.Contains("overcast"))
            return "Cloudy";
        else if (lower.Contains("snow"))
            return "Snowy";
        else
            return "Clear";
    }
}

// ═══════════════════════════════════════════════════════════
//                      COMBINED DATA MODELS
// ═══════════════════════════════════════════════════════════

/// <summary>
/// Full bus service summary for UI
/// </summary>
[System.Serializable]
public class CompleteBusData
{
    public List<BusArrivalItem> route339;       // Arrival list for Route 339
    public List<BusArrivalItem> route108;       // Arrival list for Route 108
    public DateTime nextBusTime;                // Next departure time
    public DateTime latestDepartureTime;        // Cutoff time to leave
    public string nextBusRoute;                 // Which route is next
}

// ═══════════════════════════════════════════════════════════
//                      UTILITY CLASSES
// ═══════════════════════════════════════════════════════════

/// <summary>
/// Utility methods for time parsing and conversion
/// </summary>
public static class TimeConverter
{
    /// <summary>
    /// Parse HH:mm time string as DateTime for today
    /// e.g. "06:39" -> Today 06:39; if passed already, assume tomorrow
    /// </summary>
    public static DateTime ParseTimeToday(string timeString)
    {
        try
        {
            string[] parts = timeString.Split(':');
            if (parts.Length == 2)
            {
                int hour = int.Parse(parts[0]);
                int minute = int.Parse(parts[1]);

                DateTime now = DateTime.Now;
                DateTime result = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);

                // If time has passed (e.g. it's 14:00 but bus is 06:39), assume next day
                if (result < now)
                {
                    result = result.AddDays(1);
                }

                return result;
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Time parsing failed: " + timeString + " - " + e.Message);
        }

        return DateTime.Now;
    }

    /// <summary>
    /// Calculate minutes remaining until target time
    /// </summary>
    public static int GetMinutesUntil(DateTime targetTime)
    {
        TimeSpan diff = targetTime - DateTime.Now;
        return Math.Max(0, (int)diff.TotalMinutes);
    }
}

/// <summary>
/// Helper to deserialize JSON arrays using Unity JsonUtility
/// </summary>
public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        string newJson = "{\"array\":" + json + "}";
        Wrapper<T> wrapper = UnityEngine.JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.array;
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }
}
