'use strict';

// ------------------------------------------------------------
// Timetable generator for routes 108 & 339
// - includes new 13:06 trip
// - marks 1 recommended trip based on 13:00 + walking time
// ------------------------------------------------------------

// 108 arrival times at Stratford City Bus Station
const times108AtStratford = [
  "00:02","00:17","00:32","00:47",
  "01:02","01:17","01:32","01:47",
  "06:22","06:37","06:52","07:07","07:22","07:37","07:52",
  "08:07","08:22","08:37","08:52","09:07","09:22","09:37","09:52",
  "10:07","10:22","10:37","10:52","11:07","11:22","11:37","11:52",
  "12:07","12:22","12:37","12:52",
  "13:08",
  "13:22","13:37","13:52",
  "14:07","14:22","14:37","14:52",
  "15:07","15:22","15:37","15:52",
  "16:07","16:22","16:37","16:52",
  "17:07","17:22","17:37","17:52",
  "18:07","18:22","18:37","18:52",
  "19:07","19:22","19:37","19:52",
  "20:07","20:22","20:37","20:52",
  "21:07","21:22","21:37","21:52",
  "22:07","22:22","22:37","22:52",
  "23:07","23:22","23:47"
];

// 339 arrival times at London Aquatics Centre
const times339AtAquatics = [
  "07:42","08:01","08:22","09:02","09:23","09:43",
  "10:02","10:21","10:41","11:01","11:21",
  "12:01","12:21","12:41",
  "13:06",
  "13:21","13:41",
  "14:01","14:21","14:41",
  "15:01","15:21","15:41",
  "16:01","16:21","16:41",
  "17:01","17:21","17:41",
  "18:01","18:21","18:41",
  "19:01","19:21","19:41",
  "20:01","20:21","20:41",
  "21:01","21:21","21:41",
  "22:01","22:21","22:39"
];

// ------------------- Helpers -------------------


// ---------------- Helpers ----------------

function toMinutes(hhmm) {
  const [h, m] = hhmm.split(":").map(Number);
  return h * 60 + m;
}

function fromMinutes(total) {
  // wrap around 24h just in case
  const mins = ((total % (24 * 60)) + 24 * 60) % (24 * 60);
  const h = String(Math.floor(mins / 60)).padStart(2, "0");
  const m = String(mins % 60).padStart(2, "0");
  return `${h}:${m}`;
}

/**
 * Choose recommended trip index based on:
 * - target time = 13:00 + walking time (minutes)
 * - boarding time comes from getBoardingMinutes(trip)
 *   (this is the time when the passenger must be at the stop)
 */
function chooseRecommendedIndex(trips, getBoardingMinutes, walkMinutes) {
  const baseMinutes = 13 * 60; // 13:00
  const target = baseMinutes + walkMinutes;

  let bestIdx = -1;
  let bestDiff = Infinity;

  // 1) prefer the earliest trip whose boarding time >= target
  trips.forEach((trip, idx) => {
    const dep = getBoardingMinutes(trip);
    const diff = dep - target;
    if (diff >= 0 && diff < bestDiff) {
      bestDiff = diff;
      bestIdx = idx;
    }
  });

  // 2) if none, fall back to earliest trip after 13:00
  if (bestIdx === -1) {
    const afterBase = trips
      .map((trip, idx) => ({
        idx,
        minutes: getBoardingMinutes(trip),
      }))
      .filter((t) => t.minutes >= baseMinutes)
      .sort((a, b) => a.minutes - b.minutes);

    if (afterBase.length > 0) {
      bestIdx = afterBase[0].idx;
    }
  }

  return bestIdx;
}

// ---------------- 108 ----------------

/**
 * Route 108:
 * - Your array (times108AtStratford) is the time at Stratford City Bus Station.
 * - We assume the bus reaches London Aquatics Centre 2 minutes earlier.
 *
 * For “can I catch this bus after class”, you board at London Aquatics Centre.
 * So we must compare (time at Aquatics) with 13:00 + walkingTime.
 */
function buildTimetable108(walkMinutes = 0) {
  const trips = times108AtStratford.map((t) => {
    const sc = toMinutes(t);      // time at Stratford
    const lac = sc - 2;           // time at London Aquatics (2 min earlier)

    return {
      route: "108",
      arrival_london_aquatics: fromMinutes(lac),
      arrival_stratford_city: t,
      recommended: false,
    };
  });

  // IMPORTANT FIX:
  // use time at Aquatics as the boarding time, not Stratford
  const idx = chooseRecommendedIndex(
    trips,
    (trip) => toMinutes(trip.arrival_london_aquatics),
    walkMinutes
  );

  if (idx !== -1) {
    trips[idx].recommended = true;
  }

  return trips;
}

// ---------------- 339 ----------------

/**
 * Route 339:
 * - Your array (times339AtAquatics) is the time at London Aquatics Centre.
 * - We assume the bus reaches Stratford City 2 minutes later.
 *
 * Here you board at London Aquatics as well, so boarding time = time at Aquatics.
 */
function buildTimetable339(walkMinutes = 0) {
  const trips = times339AtAquatics.map((t) => {
    const lac = toMinutes(t);     // time at London Aquatics
    const sc = lac + 2;           // time at Stratford City (2 min later)

    return {
      route: "339",
      arrival_london_aquatics: t,
      arrival_stratford_city: fromMinutes(sc),
      recommended: false,
    };
  });

  const idx = chooseRecommendedIndex(
    trips,
    (trip) => toMinutes(trip.arrival_london_aquatics),
    walkMinutes
  );

  if (idx !== -1) {
    trips[idx].recommended = true;
  }

  return trips;
}

// ---------------- Exports ----------------

module.exports = {
  buildTimetable108,
  buildTimetable339,
};

