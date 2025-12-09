#include <ESP8266WiFi.h>
#include <PubSubClient.h>
#include <Wire.h>
#include <rgb_lcd.h>

// ================== WiFi 配置 ==================
const char* ssid        = "CE-Hub-Student";
const char* wifiPass    = "casa-ce-gagarin-public-service";

// ================== MQTT 配置 ==================
const char* mqttServer  = "mqtt.cetools.org";
const int   mqttPort    = 1884;
const char* mqttUser    = "student";
const char* mqttPass    = "ce2021-mqtt-forget-whale";
const char* subTopic    = "student/MUJI/qingshan/walk";

// ================== Grove LCD RGB ==================
rgb_lcd lcd;

// ================== MQTT & 滚动显示状态 ==================
WiFiClient espClient;
PubSubClient client(espClient);

String lastMessage = "";          // 最新收到的完整消息
int    totalPages = 1;            // 一共有多少页
int    currentPage = 0;           // 当前显示哪一页
unsigned long lastPageChange = 0; // 上一次换页时间
const unsigned long pageInterval = 3000;  // 3 秒换一页

// ---------- 把 currentPage 对应的内容刷新到 LCD ----------
void updateLCDPage() {
  if (lastMessage.length() == 0) return;

  // 每页 32 个字符（2 行 * 16 列）
  int startIndex = currentPage * 32;
  int endIndex   = startIndex + 32;
  if (startIndex >= lastMessage.length()) {
    startIndex = 0;
    endIndex   = 32;
  }
  if (endIndex > lastMessage.length()) {
    endIndex = lastMessage.length();
  }

  String pageText = lastMessage.substring(startIndex, endIndex);

  String line1, line2;
  if (pageText.length() <= 16) {
    line1 = pageText;
    line2 = "";
  } else {
    line1 = pageText.substring(0, 16);
    line2 = pageText.substring(16);  // 最多到 32
  }

  lcd.clear();
  lcd.setCursor(0, 0);
  lcd.print(line1);
  lcd.setCursor(0, 1);
  lcd.print(line2);
}

// ---------- 连接 WiFi ----------
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

// ---------- MQTT 收到消息时的回调 ----------
void mqttCallback(char* topic, byte* payload, unsigned int length) {
  Serial.print("Message arrived [");
  Serial.print(topic);
  Serial.print("] ");

  lastMessage = "";
  for (unsigned int i = 0; i < length; i++) {
    lastMessage += (char)payload[i];
  }
  Serial.println(lastMessage);

  // 计算页数：每页 32 字符
  totalPages = (lastMessage.length() + 31) / 32;
  if (totalPages < 1) totalPages = 1;

  currentPage = 0;
  lastPageChange = millis();

  updateLCDPage();
}

// ---------- 连接 MQTT ----------
void reconnectMQTT() {
  while (!client.connected()) {
    Serial.print("Attempting MQTT connection...");
    String clientId = "ESP8266-LCD-";
    clientId += String(ESP.getChipId(), HEX);

    if (client.connect(clientId.c_str(), mqttUser, mqttPass)) {
      Serial.println("connected");
      client.subscribe(subTopic);
      Serial.print("Subscribed to: ");
      Serial.println(subTopic);

      lcd.clear();
      lcd.setCursor(0, 0);
      lcd.print("MQTT connected");
      lcd.setCursor(0, 1);
      lcd.print("Sub:");
      lcd.print(subTopic);

      lastMessage = "";
      totalPages  = 1;
      currentPage = 0;

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

  // I2C 初始化（SDA=D2, SCL=D1）
  Wire.begin(D2, D1);

  // Grove LCD 初始化
  lcd.begin(16, 2);
  // 设置一个好认一点的背光颜色（蓝绿）
  lcd.setRGB(0, 100, 255);

  lcd.clear();
  lcd.setCursor(0, 0);
  lcd.print("Booting...");

  // WiFi & MQTT
  setupWiFi();
  client.setServer(mqttServer, mqttPort);
  client.setCallback(mqttCallback);

  lcd.clear();
  lcd.setCursor(0, 0);
  lcd.print("WiFi OK");
  lcd.setCursor(0, 1);
  lcd.print(WiFi.localIP().toString());

  lastMessage = "";
  totalPages  = 1;
  currentPage = 0;
  lastPageChange = millis();
}

// ================== loop ==================
void loop() {
  if (!client.connected()) {
    reconnectMQTT();
  }
  client.loop();

  // 每 3 秒切换一页
  if (lastMessage.length() > 0 && totalPages > 1) {
    unsigned long now = millis();
    if (now - lastPageChange >= pageInterval) {
      currentPage++;
      if (currentPage >= totalPages) currentPage = 0;
      lastPageChange = now;
      updateLCDPage();
    }
  }
}
