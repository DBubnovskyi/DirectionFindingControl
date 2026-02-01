#include "esp32led.h"

#define LED_PIN 2 // Built-in LED on GPIO2 for ESP8266

ESP32LED::ESP32LED()
{
}

void ESP32LED::begin()
{
  pinMode(LED_PIN, OUTPUT);
  digitalWrite(LED_PIN, HIGH); // ESP8266 LED is active LOW
}

void ESP32LED::on(uint8_t brightness)
{
  digitalWrite(LED_PIN, LOW); // ESP8266 LED is active LOW
}

void ESP32LED::off()
{
  digitalWrite(LED_PIN, HIGH); // ESP8266 LED is active LOW
}