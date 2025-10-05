#ifndef DRV8871_H
#define DRV8871_H

#pragma once
#include <Arduino.h>

class DRV8871
{
public:
    DRV8871();
    void begin(uint8_t in1Pin, uint8_t in2Pin);
    void setSpeed(int speed);
    void stop();

private:
    uint8_t _in1Pin;
    uint8_t _in2Pin;
    uint8_t _channelA;
    uint8_t _channelB;
};

#endif