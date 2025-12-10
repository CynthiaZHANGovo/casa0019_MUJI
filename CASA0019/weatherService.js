// weatherService.js
// Fetch hourly temperature for Stratford area and attach to trips

const axios = require("axios");
const { toMinutes } = require("./timetable");

// Rough coordinates for Stratford / London Aquatics Centre
const LATITUDE = 51.538;
const LONGITUDE = -0.011; // west is negative

// dateStr format: "2025-12-03"
async function fetchHourlyWeather(dateStr) {
  const url = "https://api.open-meteo.com/v1/forecast";
  const params = {
    latitude: LATITUDE,
    longitude: LONGITUDE,
    hourly: "temperature_2m",
    timezone: "Europe/London",
    start_date: dateStr,
    end_date: dateStr
  };

  const res = await axios.get(url, { params });
  const { time, temperature_2m } = res.data.hourly;

  const result = [];

  for (let i = 0; i < time.length; i++) {
    const iso = time[i]; // e.g. "2025-12-03T13:00"
    const hhmm = iso.split("T")[1].slice(0, 5); // "13:00"
    result.push({
      iso,
      hhmm,
      minutes: toMinutes(hhmm),
      temperatureC: temperature_2m[i]
    });
  }

  return result;
}

// For a bus arriving at a given HH:MM, find closest hourly temperature
function matchTemperatureForTime(arrivalHHMM, hourlyWeather) {
  const m = toMinutes(arrivalHHMM);
  let best = hourlyWeather[0];
  let bestDiff = Math.abs(m - best.minutes);

  for (const item of hourlyWeather) {
    const diff = Math.abs(m - item.minutes);
    if (diff < bestDiff) {
      best = item;
      bestDiff = diff;
    }
  }

  return best.temperatureC;
}

// Attach temperatureC to each trip object
async function attachWeatherToTrips(trips, dateStr) {
  const hourly = await fetchHourlyWeather(dateStr);

  return trips.map(trip => {
    const temp = matchTemperatureForTime(
      trip.arrival_london_aquatics,
      hourly
    );
    return {
      ...trip,
      temperatureC: temp
    };
  });
}

module.exports = { attachWeatherToTrips };

