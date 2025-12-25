// DRV8871.cpp
#include "DRV8871.h"

#define PWM_FREQ 1000
#define PWM_RES 8

DRV8871::DRV8871() {}

void DRV8871::begin(uint8_t in1Pin, uint8_t in2Pin)
{
    _in1Pin = in1Pin;
    _in2Pin = in2Pin;

    pinMode(_in1Pin, OUTPUT);
    pinMode(_in2Pin, OUTPUT);

    _channelA = 0;
    _channelB = 1;

    ledcSetup(_channelA, PWM_FREQ, PWM_RES);
    ledcSetup(_channelB, PWM_FREQ, PWM_RES);

    ledcAttachPin(_in1Pin, _channelA);
    ledcAttachPin(_in2Pin, _channelB);

    stop();
}

void DRV8871::setSpeed(int speed)
{
    speed = constrain(speed, -255, 255);

    if (speed > 0)
    {
        ledcWrite(_channelA, speed);
        ledcWrite(_channelB, 0);
    }
    else if (speed < 0)
    {
        ledcWrite(_channelA, 0);
        ledcWrite(_channelB, -speed);
    }
    else
    {
        stop();
    }
}

void DRV8871::stop()
{
    ledcWrite(_channelA, 0);
    ledcWrite(_channelB, 0);
}