#ifndef DRV8871_H
#define DRV8871_H

#pragma once
#include <Arduino.h>

// Симулятор DRV8871 для ESP8266
// Імітує роботу драйвера мотора без реальних виходів
class DRV8871
{
public:
    DRV8871();
    void begin(uint8_t in1Pin, uint8_t in2Pin);
    void setSpeed(int speed); // -255 до 255
    void stop();
    int getSpeed();

private:
    int _currentSpeed;
    uint8_t _in1Pin;
    uint8_t _in2Pin;
};

#endif