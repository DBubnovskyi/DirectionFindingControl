#pragma once

#include <Arduino.h>

class HeartbeatLed
{
public:
    void on();
    void off();
    void setBrightness(uint8_t brightness);

private:
    static constexpr uint8_t LED_PIN = 8;
    bool _initialized = false;
    void ensureInitialized();
};
