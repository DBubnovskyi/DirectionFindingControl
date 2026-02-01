#pragma once
#include <Arduino.h>

#define LED_PIN 2

class ESP32LED
{
public:
  ESP32LED();
  void begin();
  void on(uint8_t hexColor = 255);
  void off();
};