/*
  ESP8266 Departure Assistant - knob-route selection version
  - NodeMCU v3 (ESP8266)
  - MG90S servo on D5
  - Rotary Angle Sensor v1.2 SIG -> A0 (used to select route by position)
  - WS2812-like LED strip on D2 (KQB-4 treated as 5V addressable)
  - No physical display: display code commented out, Serial used as placeholder
  - Weather / Timetable / Bus API functions are placeholders (return mock data)
  - All comments and identifiers are English and detailed for collaborators

  How this works:
  - Turn the knob to select route: the analog range 0..1023 is split into ROUTE_COUNT bands.
    When the band (route index) changes the device will recompute recommended bus & UI.
  - Periodic full recompute also runs every MAIN_LOOP_INTERVAL_MS.
  - Replace the fetch*Placeholder functions with real HTTP+JSON code later.
*/

#include <Arduino.h>
#include <Servo.h>
#include <FastLED.h>
#include <time.h>
#include <vector>

// ----------------------------- Configurable constants -----------------------------
const uint8_t SERVO_PIN    = D5;   // MG90S signal pin
const uint8_t LED_PIN      = D2;   // data pin for WS2812-style strip
const uint16_t NUM_LEDS    = 24;   // set to number of LEDs you have
const uint8_t KNOB_PIN     = A0;   // rotary angle sensor SIG
const unsigned long MAIN_LOOP_INTERVAL_MS = 30UL * 1000UL; // periodic refresh interval
const int ROUTE_COUNT = 3; // number of routes selectable by knob

// Route names (for display / serial); change as you like
const char* ROUTES[ROUTE_COUNT] = {
  "Route A - 42",
  "Route B - 7",
  "Route C - Express"
};

// Servo params (MG90S typical)
Servo myServo;
const int SERVO_MIN_US = 500;
const int SERVO_MAX_US = 2500;

// LED
CRGB leds[NUM_LEDS];

// State / timing
int currentRouteIndex = 0;
int lastRouteIndex = -1; // initial force-update
unsigned long lastMainMs = 0;

// ----------------------------- Data structures -----------------------------
struct WeatherInfo {
  String condition; // e.g. "Clear", "Clouds", "Rain", "Thunderstorm"
  float tempC;
  int severity;     // 0..3
};

struct TimetableEntry {
  String title;
  time_t startEpoch; // epoch seconds
  int durationMin;
  String location;
};

struct BusOption {
  String routeName;
  time_t departureEpoch;
  int travelMin;
  String departureTimeStr; // e.g. "08:43"
  String remarks;
};

// runtime variables
WeatherInfo currentWeather;
TimetableEntry nextClass;
bool hasClass = false;
BusOption recommendedBus;
bool hasRecommendedBus = false;

// ----------------------------- Placeholder API functions (replace later) -----------------------------
// Keep these signatures; API team can replace internals with actual HTTP + JSON parsing.

WeatherInfo fetchWeatherPlaceholder() {
  // MOCK: return different weather depending on selected route for easy testing.
  WeatherInfo w;
  if (currentRouteIndex == 0) {
    w.condition = "Clear";
    w.tempC = 22.0;
    w.severity = 0;
  } else if (currentRouteIndex == 1) {
    w.condition = "Clouds";
    w.tempC = 16.0;
    w.severity = 1;
  } else {
    w.condition = "Rain";
    w.tempC = 8.5;
    w.severity = 2;
  }
  return w;
}

TimetableEntry fetchTimetablePlaceholder() {
  TimetableEntry t;
  time_t now = time(nullptr);
  // MOCK: next class 40 minutes from now
  t.title = "Math 101";
  t.startEpoch = now + 40 * 60;
  t.durationMin = 60;
  t.location = "Room A";
  return t;
}

std::vector<BusOption> fetchBusOptionsPlaceholder(const char* routeName, const TimetableEntry &target) {
  std::vector<BusOption> list;
  time_t now = time(nullptr);
  // MOCK: 3 upcoming departures spaced 7 minutes apart
  for (int i = 1; i <= 3; ++i) {
    BusOption b;
    b.routeName = String(routeName);
    b.departureEpoch = now + i * 7 * 60;
    b.travelMin = 20 + i*3; // arbitrary travel time
    struct tm tmStruct;
    gmtime_r(&b.departureEpoch, &tmStruct);
    char buf[8];
    strftime(buf, sizeof(buf), "%H:%M", &tmStruct);
    b.departureTimeStr = String(buf);
    b.remarks = "";
    list.push_back(b);
  }
  return list;
}

// ----------------------------- Mapping & helpers -----------------------------

// Map a weather condition to a base servo angle (degrees)
int weatherToBaseAngle(const WeatherInfo &w) {
  String s = w.condition;
  s.toLowerCase();
  if (s.indexOf("clear") >= 0 || s.indexOf("sun") >= 0) return 20;
  if (s.indexOf("cloud") >= 0) return 60;
  if (s.indexOf("rain") >= 0 || s.indexOf("drizzle") >= 0) return 110;
  if (s.indexOf("snow") >= 0) return 120;
  if (s.indexOf("thunder") >= 0 || w.severity >= 3) return 150;
  return 80;
}

// Convert minutes until departure into an angle offset (0..40)
int minutesToOffsetAngle(int minutesUntil) {
  if (minutesUntil < 0) minutesUntil = 0;
  if (minutesUntil > 120) minutesUntil = 120;
  return map(minutesUntil, 0, 120, 40, 0);
}

// Update servo: base (weather) + offset (minutes until)
void updateServoForSituation(const WeatherInfo &w, int minutesUntil) {
  int base = weatherToBaseAngle(w);
  int offset = minutesToOffsetAngle(minutesUntil);
  int finalAngle = base + offset;
  finalAngle = constrain(finalAngle, 0, 180);
  Serial.printf("[Servo] weather=%s severity=%d base=%d offset=%d -> angle=%d\n",
                w.condition.c_str(), w.severity, base, offset, finalAngle);
  myServo.write(finalAngle);
}

// LED urgency visualization: green/yellow/red+blink
void updateLEDsForUrgency(int minutesUntil) {
  if (minutesUntil < 0) minutesUntil = 0;
  if (minutesUntil > 120) minutesUntil = 120;

  CRGB color = CRGB::Green;
  bool blink = false;
  if (minutesUntil > 30) { color = CRGB::Green; blink = false; }
  else if (minutesUntil > 15) { color = CRGB::Yellow; blink = false; }
  else { color = CRGB::Red; blink = true; }

  static unsigned long lastToggle = 0;
  static bool onState = true;
  unsigned long now = millis();
  if (blink) {
    if (now - lastToggle > 500) {
      onState = !onState;
      lastToggle = now;
    }
  } else {
    onState = true;
  }

  for (uint16_t i = 0; i < NUM_LEDS; ++i) leds[i] = onState ? color : CRGB::Black;
  FastLED.show();
}

// Choose best bus: prefer earliest bus that arrives before class start minus buffer
bool chooseBestBus(const TimetableEntry &target, const std::vector<BusOption> &options, BusOption &out) {
  if (target.title.length() == 0 || options.size() == 0) return false;
  time_t classStart = target.startEpoch;
  const int arriveBufferMin = 5;
  bool found = false;
  time_t bestDeparture = 0;
  for (size_t i = 0; i < options.size(); ++i) {
    time_t arrival = options[i].departureEpoch + options[i].travelMin*60;
    if (arrival <= (classStart - arriveBufferMin*60)) {
      if (!found || options[i].departureEpoch < bestDeparture) {
        found = true;
        bestDeparture = options[i].departureEpoch;
        out = options[i];
      }
    }
  }
  if (!found) { out = options[0]; found = true; } // fallback earliest
  return found;
}

// ----------------------------- Display placeholder (Serial) -----------------------------
// Replace this with actual display drawing code. For now we print to Serial.
void updateDisplaySerial(const TimetableEntry &t, const BusOption &bus, const WeatherInfo &w, int minutesUntil) {
  Serial.println("===== DISPLAY (serial placeholder) =====");
  if (t.title.length() > 0) {
    struct tm ts;
    gmtime_r(&t.startEpoch, &ts);
    char timestr[32]; strftime(timestr, sizeof(timestr), "%Y-%m-%d %H:%M", &ts);
    Serial.printf("Next class: %s @ %s\n", t.title.c_str(), timestr);
    Serial.printf("Location: %s Duration: %d min\n", t.location.c_str(), t.durationMin);
  } else {
    Serial.println("No upcoming class.");
  }
  if (bus.routeName.length() > 0) {
    Serial.printf("Recommended bus: %s  Departs: %s  Travel: %d min\n",
                  bus.routeName.c_str(), bus.departureTimeStr.c_str(), bus.travelMin);
    if (bus.remarks.length() > 0) Serial.printf("Remarks: %s\n", bus.remarks.c_str());
  } else {
    Serial.println("No recommended bus.");
  }
  Serial.printf("Weather: %s  %.1fC  severity=%d\n", w.condition.c_str(), w.tempC, w.severity);
  Serial.printf("Minutes until departure: %d\n", minutesUntil);
  Serial.println("========================================");
}

// ----------------------------- Workflow: recompute & update -----------------------------
void recomputeAndUpdateAll() {
  Serial.println("[Workflow] recomputeAndUpdateAll start");

  // 1) fetch placeholders (replace these calls later)
  currentWeather = fetchWeatherPlaceholder();
  nextClass = fetchTimetablePlaceholder();
  hasClass = true;

  // 2) bus options for current route
  std::vector<BusOption> options = fetchBusOptionsPlaceholder(ROUTES[currentRouteIndex], nextClass);

  // 3) choose best
  hasRecommendedBus = chooseBestBus(nextClass, options, recommendedBus);

  // 4) compute minutes until departure (prefer recommended bus)
  time_t now = time(nullptr);
  int minutesUntil = 0;
  if (hasRecommendedBus) minutesUntil = max(0, (int)difftime(recommendedBus.departureEpoch, now)/60);
  else minutesUntil = max(0, (int)difftime(nextClass.startEpoch, now)/60);

  // 5) update servo and LEDs
  updateServoForSituation(currentWeather, minutesUntil);
  updateLEDsForUrgency(minutesUntil);

  // 6) show info on serial (placeholder for physical display)
  updateDisplaySerial(nextClass, recommendedBus, currentWeather, minutesUntil);

  Serial.println("[Workflow] recomputeAndUpdateAll done");
}

// ----------------------------- Knob -> route band logic -----------------------------
int knobToRouteIndex(int knobRaw) {
  // knobRaw: 0..1023. Split into ROUTE_COUNT equal bands.
  int bandSize = 1024 / ROUTE_COUNT; // integer division
  int idx = knobRaw / bandSize;
  if (idx >= ROUTE_COUNT) idx = ROUTE_COUNT - 1;
  return idx;
}

// ----------------------------- Setup & Loop -----------------------------
void setup() {
  Serial.begin(115200);
  delay(50);
  Serial.println("Departure Assistant (knob-route selection) starting...");

  // Initialize LED strip
  FastLED.addLeds<WS2812, LED_PIN, GRB>(leds, NUM_LEDS);
  FastLED.clear();
  FastLED.show();

  // Initialize servo
  myServo.attach(SERVO_PIN, SERVO_MIN_US, SERVO_MAX_US);
  myServo.write(90);

  // seed time (optional, NTP would be better if you care about timezone)
  configTime(0, 0, "pool.ntp.org");

  // initial route read to set currentRouteIndex
  int raw = analogRead(KNOB_PIN);
  currentRouteIndex = knobToRouteIndex(raw);
  lastRouteIndex = -1; // ensure initial recompute

  // initial compute/update
  recomputeAndUpdateAll();
  lastMainMs = millis();
}

void loop() {
  // 1) check knob for route changes
  int knobRaw = analogRead(KNOB_PIN);
  int routeIdx = knobToRouteIndex(knobRaw);
  if (routeIdx != currentRouteIndex) {
    // simple hysteresis: require stable new position for a few loops could be added,
    // but for now we update immediate when band changes
    currentRouteIndex = routeIdx;
    Serial.printf("[Knob] route changed to index=%d name=%s (raw=%d)\n",
                  currentRouteIndex, ROUTES[currentRouteIndex], knobRaw);
    recomputeAndUpdateAll();
  }

  // 2) periodic refresh (in case APIs change)
  unsigned long nowMs = millis();
  if (nowMs - lastMainMs >= MAIN_LOOP_INTERVAL_MS) {
    lastMainMs = nowMs;
    recomputeAndUpdateAll();
  }

  // small delay (keeps CPU available and stabilizes analog readings)
  delay(120);
}
