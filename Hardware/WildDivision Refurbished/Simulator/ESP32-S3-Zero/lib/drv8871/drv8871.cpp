// DRV8871.cpp - Симулятор для ESP8266
#include "DRV8871.h"

DRV8871::DRV8871() : _currentSpeed(0) {}

void DRV8871::begin(uint8_t in1Pin, uint8_t in2Pin)
{
    _in1Pin = in1Pin;
    _in2Pin = in2Pin;
    _currentSpeed = 0;
    // Не ініціалізуємо реальні піни, тільки зберігаємо значення
}

void DRV8871::setSpeed(int speed)
{
    _currentSpeed = constrain(speed, -255, 255);
    // Симуляція - просто зберігаємо швидкість
}

void DRV8871::stop()
{
    _currentSpeed = 0;
}

int DRV8871::getSpeed()
{
    return _currentSpeed;
}