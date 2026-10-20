// Restored from the team's biofeedback source document.
#include <WiFi.h>
#include <WiFiUdp.h>
#include <PulseSensorPlayground.h>
#include <DHT.h>
#include "config.h" // Copy config.example.h; config.h is intentionally ignored by Git.

constexpr int DHT_PIN = 4;
constexpr int PULSE_PIN = 32;
constexpr int PULSE_THRESHOLD = 2000;
constexpr unsigned long ENVIRONMENT_INTERVAL_MS = 2000;
DHT dht(DHT_PIN, DHT22);
PulseSensorPlayground pulseSensor;
WiFiUDP udp;
float currentTemperature = 0.0f;
float currentHumidity = 0.0f;
int currentBpm = 0;
unsigned long previousEnvironmentRead = 0;

void setup() {
    Serial.begin(115200);
    WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
    while (WiFi.status() != WL_CONNECTED) {
        delay(500);
        Serial.print('.');
    }
    Serial.println(WiFi.localIP());
    dht.begin();
    pulseSensor.analogInput(PULSE_PIN);
    pulseSensor.setThreshold(PULSE_THRESHOLD);
    if (!pulseSensor.begin()) Serial.println("Pulse sensor initialization failed");
}

void loop() {
    if (pulseSensor.sawStartOfBeat()) currentBpm = pulseSensor.getBeatsPerMinute();
    unsigned long now = millis();
    if (now - previousEnvironmentRead >= ENVIRONMENT_INTERVAL_MS) {
        previousEnvironmentRead = now;
        float humidity = dht.readHumidity();
        float temperature = dht.readTemperature();
        if (!isnan(humidity) && !isnan(temperature)) {
            currentHumidity = humidity;
            currentTemperature = temperature;
        }
    }
    String packet = String(currentBpm) + "," + String(currentTemperature) + "," + String(currentHumidity);
    udp.beginPacket(PICO_IP, PICO_PORT);
    udp.print(packet);
    udp.endPacket();
    Serial.println(packet);
    delay(50);
}
