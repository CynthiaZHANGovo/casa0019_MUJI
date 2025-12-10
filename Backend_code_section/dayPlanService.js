// dayPlanService.js
// Combine fake timetable + weather into a final day plan

const { buildFakeDayPlan, toMinutes } = require("./timetable");
const { attachWeatherToTrips } = require("./weatherService");

// dateStr: "2025-12-03"
async function buildDayPlanWithWeather(dateStr) {
  // 1. base timetable (already sorted & recommended)
  const baseTrips = buildFakeDayPlan();

  // 2. attach temperature
  const withWeather = await attachWeatherToTrips(baseTrips, dateStr);

  // 3. ensure sorted by arrival at London Aquatics Centre (safety)
  withWeather.sort(
    (a, b) =>
      toMinutes(a.arrival_london_aquatics) -
      toMinutes(b.arrival_london_aquatics)
  );

  return withWeather;
}

module.exports = { buildDayPlanWithWeather };
