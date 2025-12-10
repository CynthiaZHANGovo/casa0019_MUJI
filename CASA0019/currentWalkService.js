// currentWalkService.js
// Fetch real weather (current + last N days) and map it into a 24-segment walking-time model.

"use strict";

const axios = require("axios");

// Stratford / London Aquatics Centre
const LATITUDE = 51.538;
const LONGITUDE = -0.011;

// ---------- Weather helpers ----------

// Map Open-Meteo weathercode to simple conditions
// Output: "clear" | "cloudy" | "rain" | "snow" | "other"
function mapWeatherCodeToCondition(code) {
  // Clear sky
  if (code === 0) return "clear";

  // Mainly clear / partly cloudy / overcast
  if (code === 1 || code === 2 || code === 3) return "cloudy";

  // Rain / drizzle
  if (
    (code >= 51 && code <= 57) || // drizzle
    (code >= 61 && code <= 67) || // rain
    code === 80 ||
    code === 81 ||
    code === 82 // rain showers
  ) {
    return "rain";
  }

  // Snow
  if (
    (code >= 71 && code <= 77) || // snow / grains
    code === 85 ||
    code === 86 // snow showers
  ) {
    return "snow";
  }

  return "other";
}

// Get weather index: clear=0, cloudy=1, rain=2, snow=3
function getWeatherIndex(condition) {
  if (condition === "clear") return 0;
  if (condition === "cloudy") return 1;
  if (condition === "rain") return 2;
  if (condition === "snow") return 3;
  // Treat "other" as cloudy
  return 1;
}

// Get temperature bucket index 0..5
// 0: temp <= 0
// 1: 0 < temp <= 5
// 2: 5 < temp <= 10
// 3: 10 < temp <= 20
// 4: 20 < temp <= 25
// 5: temp > 25
function getTempBucketIndex(tempC) {
  if (tempC <= 0) return 0;
  if (tempC <= 5) return 1;
  if (tempC <= 10) return 2;
  if (tempC <= 20) return 3;
  if (tempC <= 25) return 4;
  return 5;
}

// ---------- Walking-time model ----------
// 4–8 minutes (240–480s), 24 segments, each 10 seconds.
// segmentIndex = weatherIndex * 6 + tempIndex (0..23)
function estimateWalkingTime(tempC, condition) {
  const weatherIndex = getWeatherIndex(condition); // 0..3
  const tempIndex = getTempBucketIndex(tempC);     // 0..5
  const segmentIndex = weatherIndex * 6 + tempIndex; // 0..23

  const baseSeconds = 4 * 60; // 4 minutes = 240s
  const seconds = baseSeconds + segmentIndex * 10; // 240..480
  const minutes = seconds / 60;                    // 4.0..8.0

  return {
    weatherIndex,
    tempIndex,
    segmentIndex,
    walkingTimeSeconds: seconds,
    walkingTimeMinutes: Number(minutes.toFixed(2))
  };
}

// ---------- Current weather ----------

async function fetchCurrentWeather() {
  const url = "https://api.open-meteo.com/v1/forecast";

  const params = {
    latitude: LATITUDE,
    longitude: LONGITUDE,
    current_weather: true,
    timezone: "Europe/London"
  };

  const res = await axios.get(url, { params });
  const cw = res.data.current_weather;

  const temperatureC = cw.temperature;
  const weathercode = cw.weathercode;
  const condition = mapWeatherCodeToCondition(weathercode);

  const timeInfo = estimateWalkingTime(temperatureC, condition);

  return {
    temperatureC,
    condition,
    walkingTimeMinutes: timeInfo.walkingTimeMinutes,
    walkingTimeSeconds: timeInfo.walkingTimeSeconds,
    weatherIndex: timeInfo.weatherIndex,
    tempBucketIndex: timeInfo.tempIndex,
    segmentIndex: timeInfo.segmentIndex
  };
}

// ---------- 30-day daily history ----------
// For each day: average temp, condition, and walking time from the same model.
async function fetchDailyWalkHistory(days = 30) {
  const url = "https://api.open-meteo.com/v1/forecast";

  const params = {
    latitude: LATITUDE,
    longitude: LONGITUDE,
    daily: "temperature_2m_mean,weathercode",
    timezone: "Europe/London",
    past_days: days,
    forecast_days: 0
  };

  const res = await axios.get(url, { params });
  const daily = res.data.daily;

  const times = daily.time;
  const temps = daily.temperature_2m_mean;
  const codes = daily.weathercode;

  const items = [];

  for (let i = 0; i < times.length; i++) {
    const date = times[i];
    const avgTemp = temps[i];
    const code = codes[i];

    const condition = mapWeatherCodeToCondition(code);
    const walkInfo = estimateWalkingTime(avgTemp, condition);

    items.push({
      date,
      avgTemperatureC: avgTemp,
      condition,
      walkingTimeMinutes: walkInfo.walkingTimeMinutes,
      walkingTimeSeconds: walkInfo.walkingTimeSeconds,
      weatherIndex: walkInfo.weatherIndex,
      tempBucketIndex: walkInfo.tempIndex,
      segmentIndex: walkInfo.segmentIndex
    });
  }

  return {
    days: items.length,
    items
  };
}

module.exports = {
  fetchCurrentWeather,
  fetchDailyWalkHistory
};

