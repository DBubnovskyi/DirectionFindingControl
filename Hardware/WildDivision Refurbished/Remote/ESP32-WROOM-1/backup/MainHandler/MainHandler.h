#pragma once

#include <Arduino.h>
#include <U8g2lib.h>
#include <Keypad.h>
#include <HardwareSerial.h>
#include "interactionHeandler.h"
#include "esp32LED.h"

class MainHandler
{
private:
    InteractionHeandler *_keypadHandler;
    HardwareSerial *_rs485Serial;
    ESP32LED *_led;

public:
    MainHandler(InteractionHeandler *keypadHandler, HardwareSerial *rs485Serial, ESP32LED *led);
    ~MainHandler();
    void handle();
};
