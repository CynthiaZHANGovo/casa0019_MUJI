#include <ESP8266WiFi.h>
#include <PubSubClient.h>
#include <Wire.h>
#include <rgb_lcd.h>
#include <ArduinoJson.h>
#include <Servo.h>          // Servo
#include <Adafruit_NeoPixel.h>  // WS2812 / WS2812B

// ================== WiFi config ==================
const char* ssid     = "CE-Hub-Student";
const char* wifiPass = "casa-ce-gagarin-public-service";

// ================== MQTT config ==================
const char* mqttServer  = "mqtt.cetools.org";
const int   mqttPort    = 1884;
const char* mqttUser    = "student";
const char* mqttPass    = "ce2021-mqtt-forget-whale";

const char* topicBus108 = "student/MUJI/qingshan/bus-108-mini";
const char* topicBus339 = "student/MUJI/qingshan/bus-339-mini";
const char* topicWalk   = "student/MUJI/qingshan/walk";

// ================== hardware pins ==================
const int ROTARY_PIN = A0;   // Grove Rotary Angle sensor to A0
const int SERVO_PIN  = D5;   // MG90S to D5
const int LED_PIN    = D4;   // WS2812 strip data pin
const int NUM_LEDS   = 14;   // first 25 LEDs

// ================== Grove LCD ==================
rgb_lcd lcd;

// ================== MQTT objects ==================
WiFiClient espClient;
PubSubClient client(espClient);

// ================== Servo ==================
Servo curtainServo;
int   currentServoAngle = 0;

// ================== WS2812 strip ==================
Adafruit_NeoPixel strip(NUM_LEDS, LED_PIN, NEO_GRB + NEO_KHZ800);

// ================== data structures ==================
struct BusInfo {
  String route;
  String londonAquatics;   // arrival_london_aquatics
  String stratfordCity;    // arrival_stratford_city
  bool   valid;
  bool   recommended;
};

BusInfo bus108Rec;   // 108: recommended
BusInfo bus108Next;  // 108: next
bool    hasBus108 = false;

BusInfo bus339Rec;   // 339: recommended
BusInfo bus339Next;  // 339: next
bool    hasBus339 = false;

// current zone: 0=108, 1=339
int currentZone = 0;
int lastZone    = -1;
bool displayDirty = true;

// ================== helpers ==================

String trim16(const String &s) {
  if (s.length() <= 16) return s;
  return s.substring(0, 16);
}

String makeLine(const BusInfo &b, bool highlight) {
  if (!b.valid) {
    return highlight ? ">No data" : " No data";
  }

  String line = b.route + " " + b.londonAquatics + " " + b.stratfordCity;
  if (highlight) {
    line = ">" + line;
  } else {
    line = " " + line;
  }
  return trim16(line);
}

void renderDisplay() {
  lcd.clear();

  BusInfo *b = nullptr;

  if (currentZone == 0) {
    if (!hasBus108 || !bus108Rec.valid) {
      lcd.setCursor(0, 0);
      lcd.print(trim16("Waiting 108"));
      lcd.setCursor(0, 1);
      lcd.print("");
      return;
    }
    b = &bus108Rec;
  } else {
    if (!hasBus339 || !bus339Rec.valid) {
      lcd.setCursor(0, 0);
      lcd.print(trim16("Waiting 339"));
      lcd.setCursor(0, 1);
      lcd.print("");
      return;
    }
    b = &bus339Rec;
  }

  String line1 = b->route + " Recommended";
  lcd.setCursor(0, 0);
  lcd.print(trim16(line1));

  String line2 = b->londonAquatics + "->" + b->stratfordCity;
  lcd.setCursor(0, 1);
  lcd.print(trim16(line2));
}

void updateZoneFromRotary() {
  int raw = analogRead(ROTARY_PIN);  // 0~1023
  int zone = (raw < 512) ? 0 : 1;    // 0:108, 1:339

  currentZone = zone;

  if (zone != lastZone) {
    lastZone = zone;
    displayDirty = true;
    Serial.print("Zone changed to: ");
    Serial.println(zone == 0 ? "108" : "339");
  }
}

// ================== WiFi & MQTT ==================
void setupWiFi() {
  delay(10);
  Serial.println();
  Serial.print("Connecting to ");
  Serial.println(ssid);

  WiFi.mode(WIFI_STA);
  WiFi.begin(ssid, wifiPass);

  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }

  Serial.println();
  Serial.print("WiFi connected, IP: ");
  Serial.println(WiFi.localIP());
}

// ================== JSON helpers ==================
bool extractRecommendedAndNextObjects(const String& jsonStr,
                                      String& recObjJson,
                                      String& nextObjJson) {
  recObjJson  = "";
  nextObjJson = "";

  int recPos = jsonStr.indexOf("\"recommended\":true");
  int recStart, recEnd;

  if (recPos != -1) {
    recStart = jsonStr.lastIndexOf('{', recPos);
    recEnd   = jsonStr.indexOf('}', recPos);
  } else {
    recStart = jsonStr.indexOf('{');
    if (recStart == -1) return false;
    recEnd = jsonStr.indexOf('}', recStart);
  }

  if (recStart == -1 || recEnd == -1) {
    return false;
  }

  recObjJson = jsonStr.substring(recStart, recEnd + 1);

  int nextStart = jsonStr.indexOf('{', recEnd + 1);
  if (nextStart != -1) {
    int nextEnd = jsonStr.indexOf('}', nextStart);
    if (nextEnd != -1) {
      nextObjJson = jsonStr.substring(nextStart, nextEnd + 1);
    }
  }

  return true;
}

void parseBusJson(const char* topic, const String& jsonStr) {
  String recObjJson;
  String nextObjJson;

  if (!extractRecommendedAndNextObjects(jsonStr, recObjJson, nextObjJson)) {
    Serial.println("Failed to extract objects from JSON string.");
    return;
  }

  Serial.println("Recommended Obj JSON:");
  Serial.println(recObjJson);
  if (nextObjJson.length() > 0) {
    Serial.println("Next Obj JSON:");
    Serial.println(nextObjJson);
  }

  BusInfo recBus;
  BusInfo nextBus;
  recBus.valid  = false;
  nextBus.valid = false;

  {
    StaticJsonDocument<256> doc;
    DeserializationError err = deserializeJson(doc, recObjJson);
    if (err) {
      Serial.print("JSON parse error (recommended): ");
      Serial.println(err.c_str());
    } else {
      recBus.route          = String((const char*)doc["route"]);
      recBus.londonAquatics = String((const char*)doc["arrival_london_aquatics"]);
      recBus.stratfordCity  = String((const char*)doc["arrival_stratford_city"]);
      recBus.valid          = true;
      recBus.recommended    = doc["recommended"] | true;
    }
  }

  if (nextObjJson.length() > 0) {
    StaticJsonDocument<256> docNext;
    DeserializationError err = deserializeJson(docNext, nextObjJson);
    if (err) {
      Serial.print("JSON parse error (next): ");
      Serial.println(err.c_str());
    } else {
      nextBus.route          = String((const char*)docNext["route"]);
      nextBus.londonAquatics = String((const char*)docNext["arrival_london_aquatics"]);
      nextBus.stratfordCity  = String((const char*)docNext["arrival_stratford_city"]);
      nextBus.valid          = true;
      nextBus.recommended    = docNext["recommended"] | false;
    }
  }

  if (!recBus.valid) {
    Serial.println("No valid recommended bus parsed.");
    return;
  }

  String t = String(topic);
  if (t == topicBus108) {
    bus108Rec  = recBus;
    bus108Next = nextBus;
    hasBus108  = true;
    Serial.println("✅ Updated data for 108");
  } else if (t == topicBus339) {
    bus339Rec  = recBus;
    bus339Next = nextBus;
    hasBus339  = true;
    Serial.println("✅ Updated data for 339");
  }

  displayDirty = true;
}

// ========== parse walk JSON and control servo ==========
void parseWalkJson(const String& jsonStr) {
  StaticJsonDocument<256> doc;
  DeserializationError err = deserializeJson(doc, jsonStr);

  if (err) {
    Serial.print("JSON parse error (walk): ");
    Serial.println(err.c_str());
    return;
  }

  int segmentIndex = doc["segmentIndex"] | 0;
  Serial.print("segmentIndex = ");
  Serial.println(segmentIndex);

  float rawAngle = 180.0f - 4.25f * segmentIndex;
  if (rawAngle < 0)   rawAngle = 0;
  if (rawAngle > 180) rawAngle = 180;

  int targetAngle = (int)(rawAngle + 0.5f);

  Serial.print("Move servo from 180 to ");
  Serial.print(targetAngle);
  Serial.println(" deg");

  curtainServo.write(180);
  delay(300);
  curtainServo.write(targetAngle);
  currentServoAngle = targetAngle;
}

// ================== MQTT callback ==================
void mqttCallback(char* topic, byte* payload, unsigned int length) {
  Serial.print("Message arrived [");
  Serial.print(topic);
  Serial.println("]");

  String msg;
  msg.reserve(length + 1);
  for (unsigned int i = 0; i < length; i++) {
    msg += (char)payload[i];
  }

  Serial.println("---- Raw JSON ----");
  Serial.println(msg);
  Serial.println("------------------");

  String t = String(topic);

  if (t == topicBus108 || t == topicBus339) {
    parseBusJson(topic, msg);
  } else if (t == topicWalk) {
    parseWalkJson(msg);
  } else {
    Serial.println("Ignoring topic.");
  }
}

// ================== MQTT connect ==================
void reconnectMQTT() {
  while (!client.connected()) {
    Serial.print("Attempting MQTT connection...");
    String clientId = "ESP8266-BUS-";
    clientId += String(ESP.getChipId(), HEX);

    if (client.connect(clientId.c_str(), mqttUser, mqttPass)) {
      Serial.println("connected");

      client.subscribe(topicBus108);
      client.subscribe(topicBus339);
      client.subscribe(topicWalk);

      Serial.print("Subscribed to: ");
      Serial.println(topicBus108);
      Serial.print("Subscribed to: ");
      Serial.println(topicBus339);
      Serial.print("Subscribed to: ");
      Serial.println(topicWalk);

      lcd.clear();
      lcd.setCursor(0, 0);
      lcd.print("MQTT connected");
      lcd.setCursor(0, 1);
      lcd.print("108&339&walk");

      displayDirty = true;

    } else {
      Serial.print("failed, rc=");
      Serial.print(client.state());
      Serial.println(" try again in 5 seconds");

      lcd.clear();
      lcd.setCursor(0, 0);
      lcd.print("MQTT fail rc ");
      lcd.print(client.state());
      lcd.setCursor(0, 1);
      lcd.print("Retry in 5s");

      delay(5000);
    }
  }
}

// ================== setup ==================
void setup() {
  Serial.begin(115200);
  delay(1000);

  // I2C (SDA=D2, SCL=D1)
  Wire.begin(D2, D1);

  // LCD
  lcd.begin(16, 2);
  lcd.setRGB(0, 100, 255);

  lcd.clear();
  lcd.setCursor(0, 0);
  lcd.print("Booting...");

  hasBus108 = false;
  hasBus339 = false;
  lastZone  = -1;
  currentZone = 0;
  displayDirty = true;

  // Servo
  curtainServo.attach(SERVO_PIN, 500, 2500);
  curtainServo.write(0);
  currentServoAngle = 0;

  // WS2812 strip init: light first 25 LEDs
  strip.begin();
  strip.show();  // clear
  for (int i = 0; i < NUM_LEDS; i++) {
    // set to a medium brightness blue (you can change the color)
    strip.setPixelColor(i, strip.Color(0, 0, 150));
  }
  strip.show();

  // WiFi & MQTT
  setupWiFi();
  client.setServer(mqttServer, mqttPort);
  client.setCallback(mqttCallback);

  lcd.clear();
  lcd.setCursor(0, 0);
  lcd.print("WiFi OK");
  lcd.setCursor(0, 1);
  lcd.print(WiFi.localIP().toString());

  delay(1000);
  lcd.clear();
  lcd.setCursor(0, 0);
  lcd.print("Waiting 108/339");
}
// ================== LED animation state ==================
unsigned long ledTimer = 0;
int ledStep = 0;        
bool ledHold = false;    // 正在全亮停顿阶段

// ================== loop ==================
void loop() {
  if (!client.connected()) {
    reconnectMQTT();
  }
  client.loop();

  updateZoneFromRotary();

  if (displayDirty) {
    renderDisplay();
    displayDirty = false;
  }

  // ================== LED animation ==================
  unsigned long now = millis();

  // 如果正在全亮等待
  if (ledHold) {
    if (now >= ledTimer) {
      // 等待结束 → 全灭，然后重新开始流水
      for (int i = 0; i < NUM_LEDS; i++) {
        strip.setPixelColor(i, strip.Color(0, 0, 0));  
      }
      strip.show();
      ledStep = 0;
      ledHold = false;
      ledTimer = now + 80;  // 下一次点亮间隔
    }
    return; // 不继续执行流水逻辑
  }

  // 每 80ms 点亮下一颗灯
  if (now >= ledTimer) {
    ledTimer = now + 80;

    if (ledStep < NUM_LEDS) {
      // 点亮当前灯
      strip.setPixelColor(ledStep, strip.Color(0, 0, 150));  // 蓝色
      strip.show();
      ledStep++;

      // 如果到达最后一颗 → 进入 2 秒全亮阶段
      if (ledStep == NUM_LEDS) {
        ledHold = true;
        ledTimer = now + 2000; // 全亮停 2 秒
      }

    }
  }

  delay(10);
}


