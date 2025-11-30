#include <Arduino.h>
#include <Servo.h>
#include <FastLED.h>

// -------------------- Pins --------------------
#define SERVO_PIN   D5      // MG90S signal
#define LED_PIN     D2      // LED strip data (green wire)
#define NUM_LEDS    30      // <-- change to the approximate number of LEDs
#define KNOB_PIN    A0      // rotary angle sensor SIG

// -------------------- Objects -----------------
Servo myServo;
CRGB leds[NUM_LEDS];

// -------------------- Servo sweep -------------
int servoAngle = 0;
int servoDir   = 1;  // 1 = forward, -1 = backward
unsigned long lastServoMove = 0;
const unsigned long SERVO_STEP_INTERVAL = 20;  // ms between angle steps

void setup() {
  Serial.begin(115200);
  delay(200);
  Serial.println("=== Combined test: Servo + Knob + LED strip ===");

  // ----- Servo -----
  myServo.attach(SERVO_PIN);
  myServo.write(90);   // start at middle

  // ----- LED strip -----
  FastLED.addLeds<WS2812, LED_PIN, GRB>(leds, NUM_LEDS);
  FastLED.clear();
  FastLED.show();

  // Light everything RED initially
  for (int i = 0; i < NUM_LEDS; i++) {
    leds[i] = CRGB::Red;
  }
  FastLED.show();

  // ----- Knob -----
  pinMode(KNOB_PIN, INPUT);  // SIG -> A0 (rotary angle sensor)
}

void loop() {
  // 1) Read knob
  int knobRaw = analogRead(KNOB_PIN);  // 0..1023 on ESP8266
  Serial.print("Knob raw: ");
  Serial.println(knobRaw);

  // 2) Use knob value to change LED color (just to see reaction)
  // low value -> green, mid -> blue, high -> red
  CRGB color;
  if (knobRaw < 300) {
    color = CRGB::Green;
  } else if (knobRaw < 700) {
    color = CRGB::Blue;
  } else {
    color = CRGB::Red;
  }

  for (int i = 0; i < NUM_LEDS; i++) {
    leds[i] = color;
  }
  FastLED.show();

  // 3) Sweep servo back and forth continuously
  unsigned long now = millis();
  if (now - lastServoMove > SERVO_STEP_INTERVAL) {
    lastServoMove = now;

    servoAngle += servoDir;
    if (servoAngle >= 180) {
      servoAngle = 180;
      servoDir = -1;
    } else if (servoAngle <= 0) {
      servoAngle = 0;
      servoDir = 1;
    }

    myServo.write(servoAngle);
  }

  delay(50);  // small delay so serial printing is readable
}
