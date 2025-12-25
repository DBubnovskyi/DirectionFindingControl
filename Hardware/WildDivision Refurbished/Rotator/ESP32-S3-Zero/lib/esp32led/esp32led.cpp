#include "esp32led.h"

#define LED_PIN 8 // Built-in LED on GPIO8 for ESP32-C3 SuperMini

ESP32LED::ESP32LED()
{
  pinMode(LED_PIN, OUTPUT);
}

void ESP32LED::begin()
{
  // Initialization code if needed
}

void ESP32LED::on(uint8_t brightness)
{
  analogWrite(LED_PIN, brightness);
}

void ESP32LED::off()
{
  analogWrite(LED_PIN, 0);
}