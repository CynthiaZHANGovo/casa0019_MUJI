// server.js
// Main backend for bus timetables + current walk info

require("dotenv").config();

const express = require("express");
const mqtt = require("mqtt");

const {
  buildTimetable108,
  buildTimetable339
} = require("./timetable");

const {
  fetchCurrentWeather,
  fetchDailyWalkHistory
} = require("./currentWalkService");

// -------------------- Express app --------------------

const app = express();
const PORT = process.env.PORT || 3000;

// -------------------- MQTT setup --------------------

const MQTT_BROKER = process.env.MQTT_BROKER || "mqtt.cetools.org";
const MQTT_PORT = process.env.MQTT_PORT || 1884;
const MQTT_USERNAME = process.env.MQTT_USERNAME || "student";
const MQTT_PASSWORD = process.env.MQTT_PASSWORD || "ce2021-mqtt-forget-whale";

const MQTT_TOPIC_108 =
  process.env.MQTT_TOPIC_108 || "student/MUJI/qingshan/bus-108";
const MQTT_TOPIC_339 =
  process.env.MQTT_TOPIC_339 || "student/MUJI/qingshan/bus-339";
const MQTT_TOPIC_WALK =
  process.env.MQTT_TOPIC_WALK || "student/MUJI/qingshan/walk";

// best-only topics (single recommended trip for each route)
const MQTT_TOPIC_108_BEST =
  process.env.MQTT_TOPIC_108_BEST || "student/MUJI/qingshan/hzh-108";
const MQTT_TOPIC_339_BEST =
  process.env.MQTT_TOPIC_339_BEST || "student/MUJI/qingshan/hzh-339";

// 30-day walking history topic
const MQTT_TOPIC_WALK_HISTORY =
  process.env.MQTT_TOPIC_WALK_HISTORY ||
  "student/MUJI/qingshan/walk-history";

const mqttUrl = `mqtt://${MQTT_BROKER}:${MQTT_PORT}`;

const mqttOptions = {
  username: MQTT_USERNAME,
  password: MQTT_PASSWORD
};

const mqttClient = mqtt.connect(mqttUrl, mqttOptions);

// -------------------- MQTT publish helpers --------------------

// Publish timetable for route 108 (full day + recommended flag)
async function publishTimetable108() {
  try {
    const walk = await fetchCurrentWeather();
    const walkMinutes = walk.walkingTimeMinutes;

    const timetable108 = buildTimetable108(walkMinutes);
    const payload = JSON.stringify(timetable108);

    mqttClient.publish(
      MQTT_TOPIC_108,
      payload,
      { qos: 1, retain: true },
      err => {
        if (err) {
          console.error("Failed to publish timetable 108:", err.message);
        } else {
          console.log(
            `Published timetable 108 to ${MQTT_TOPIC_108} with walkMinutes = ${walkMinutes}`
          );
        }
      }
    );

    // publish best-only trip
    const bestTrip = timetable108.find(t => t.recommended);
    if (bestTrip) {
      mqttClient.publish(
        MQTT_TOPIC_108_BEST,
        JSON.stringify(bestTrip),
        { qos: 1, retain: true },
        err => {
          if (err) {
            console.error("Failed to publish best trip 108:", err.message);
          } else {
            console.log(
              `Published best trip 108 to ${MQTT_TOPIC_108_BEST}: ${bestTrip.arrival_london_aquatics}`
            );
          }
        }
      );
    }
  } catch (err) {
    console.error("Error building timetable 108:", err.message);
  }
}

// Publish timetable for route 339 (full day + recommended flag)
async function publishTimetable339() {
  try {
    const walk = await fetchCurrentWeather();
    const walkMinutes = walk.walkingTimeMinutes;

    const timetable339 = buildTimetable339(walkMinutes);
    const payload = JSON.stringify(timetable339);

    mqttClient.publish(
      MQTT_TOPIC_339,
      payload,
      { qos: 1, retain: true },
      err => {
        if (err) {
          console.error("Failed to publish timetable 339:", err.message);
        } else {
          console.log(
            `Published timetable 339 to ${MQTT_TOPIC_339} with walkMinutes = ${walkMinutes}`
          );
        }
      }
    );

    // publish best-only trip
    const bestTrip = timetable339.find(t => t.recommended);
    if (bestTrip) {
      mqttClient.publish(
        MQTT_TOPIC_339_BEST,
        JSON.stringify(bestTrip),
        { qos: 1, retain: true },
        err => {
          if (err) {
            console.error("Failed to publish best trip 339:", err.message);
          } else {
            console.log(
              `Published best trip 339 to ${MQTT_TOPIC_339_BEST}: ${bestTrip.arrival_london_aquatics}`
            );
          }
        }
      );
    }
  } catch (err) {
    console.error("Error building timetable 339:", err.message);
  }
}

// Publish current weather + walking time
async function publishCurrentWalk() {
  try {
    const data = await fetchCurrentWeather();
    const payload = JSON.stringify(data);

    mqttClient.publish(
      MQTT_TOPIC_WALK,
      payload,
      { qos: 1, retain: true },
      err => {
        if (err) {
          console.error("Failed to publish walk data:", err);
        } else {
          console.log(`Published walk data to ${MQTT_TOPIC_WALK}`);
        }
      }
    );
  } catch (err) {
    console.error("Error fetching walk data:", err);
  }
}

// Publish last 30 days of average temp, condition and walking time
async function publishWalkHistory() {
  try {
    const history = await fetchDailyWalkHistory(30);
    const payload = JSON.stringify(history);

    mqttClient.publish(
      MQTT_TOPIC_WALK_HISTORY,
      payload,
      { qos: 1, retain: true },
      err => {
        if (err) {
          console.error("Failed to publish walk history:", err);
        } else {
          console.log(
            `Published walk history (days=${history.days}) to ${MQTT_TOPIC_WALK_HISTORY}`
          );
        }
      }
    );
  } catch (err) {
    console.error("Error fetching walk history:", err);
  }
}

// -------------------- MQTT events --------------------

mqttClient.on("connect", () => {
  console.log("Connected to MQTT broker");

  // initial publish
  publishTimetable108();
  publishTimetable339();
  publishCurrentWalk();
  publishWalkHistory();

  // timetables: refresh every 1 hour
  setInterval(() => {
    publishTimetable108();
    publishTimetable339();
  }, 60 * 60 * 1000);

  // current walk: refresh every 5 minutes
  setInterval(() => {
    publishCurrentWalk();
  }, 5 * 60 * 1000);

  // history: refresh every 6 hours
  setInterval(() => {
    publishWalkHistory();
  }, 6 * 60 * 60 * 1000);
});

mqttClient.on("error", err => {
  console.error("MQTT connection error:", err.message);
});

// -------------------- HTTP APIs --------------------

// API 1: timetable for 108 (recomputed on each request)
app.get("/api/timetable/108", async (req, res) => {
  try {
    const walk = await fetchCurrentWeather();
    const walkMinutes = walk.walkingTimeMinutes;

    const timetable108 = buildTimetable108(walkMinutes);
    res.json(timetable108);
  } catch (err) {
    console.error("Error in /api/timetable/108:", err);
    res.status(500).json({ error: "Internal server error" });
  }
});

// API 2: timetable for 339 (recomputed on each request)
app.get("/api/timetable/339", async (req, res) => {
  try {
    const walk = await fetchCurrentWeather();
    const walkMinutes = walk.walkingTimeMinutes;

    const timetable339 = buildTimetable339(walkMinutes);
    res.json(timetable339);
  } catch (err) {
    console.error("Error in /api/timetable/339:", err);
    res.status(500).json({ error: "Internal server error" });
  }
});

// API 3: current temperature + condition + walking time
app.get("/api/current-walk", async (req, res) => {
  try {
    const data = await fetchCurrentWeather();
    res.json(data);
  } catch (err) {
    console.error("Error in /api/current-walk:", err);
    res.status(500).json({ error: "Internal server error" });
  }
});

// API 4: last 30 days walking history
app.get("/api/walk-history", async (req, res) => {
  try {
    const history = await fetchDailyWalkHistory(30);
    res.json(history);
  } catch (err) {
    console.error("Error in /api/walk-history:", err);
    res.status(500).json({ error: "Internal server error" });
  }
});

// -------------------- Start server --------------------

app.listen(PORT, () => {
  console.log(`HTTP API listening on http://localhost:${PORT}`);
});