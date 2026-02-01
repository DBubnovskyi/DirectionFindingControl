#include <Arduino.h>
#include <Wire.h>
#include "MT6701.h"

// Симулятор MT6701 для ESP8266
// Імітує роботу датчика кута з точністю ±1°

MT6701::MT6701(uint8_t device_address, int update_interval, int rpm_threshold, int rpm_filter_size)
    : address(device_address),
      updateIntervalMillis(update_interval),
      lastUpdateTime(0),
      count(0),
      accumulator(0),
      rpm(0),
      sensorConnected(true),
      rpmFilterIndex(0),
      rpmFilterSize(rpm_filter_size),
      rpmThreshold(rpm_threshold),
      _simulatedAngle(0),
      _lastReadAngle(0),
      _lastSimulationUpdate(0)
{
}

MT6701::~MT6701()
{
}

void MT6701::begin(uint8_t sda_pin, uint8_t scl_pin)
{
    // Симуляція - не ініціалізуємо I2C
    sensorConnected = true;
    rpmFilter.resize(rpmFilterSize);
    _simulatedAngle = 0;
    _lastReadAngle = 0;
    _lastSimulationUpdate = millis();
    lastUpdateTime = millis();
}

bool MT6701::isConnected()
{
    return sensorConnected;
}

bool MT6701::testConnection()
{
    sensorConnected = true;
    return sensorConnected;
}

float MT6701::getAngleRadians()
{
    return _simulatedAngle * PI / 180.0;
}

float MT6701::getAngleDegrees()
{
    // Датчик видає стабільне значення коли немає руху
    // Показання змінюється тільки в updateSimulation()
    return _lastReadAngle;
}

int MT6701::getFullTurns()
{
    return accumulator / COUNTS_PER_REVOLUTION;
}

float MT6701::getTurns()
{
    return (float)accumulator / (float)COUNTS_PER_REVOLUTION;
}

int MT6701::getAccumulator()
{
    return accumulator;
}

float MT6701::getRPM()
{
    float sum = 0;
    for (float value : rpmFilter)
    {
        sum += value;
    }
    return rpmFilter.size() > 0 ? sum / rpmFilter.size() : 0;
}

int MT6701::getCount()
{
    return count;
}

void MT6701::updateCount()
{
    // В симуляції цей метод не потрібен
}

int MT6701::readCount()
{
    // Конвертуємо кут в count
    return (int)((_simulatedAngle / 360.0) * COUNTS_PER_REVOLUTION);
}

void MT6701::updateRPMFilter(float newRPM)
{
    rpmFilter[rpmFilterIndex] = newRPM;
    rpmFilterIndex = (rpmFilterIndex + 1) % rpmFilterSize;
}

void MT6701::setSimulatedAngle(float angle)
{
    // Нормалізуємо кут
    while (angle < 0)
        angle += 360.0;
    while (angle >= 360.0)
        angle -= 360.0;

    _simulatedAngle = angle;
    _lastReadAngle = angle;
    count = (int)((angle / 360.0) * COUNTS_PER_REVOLUTION);
}

void MT6701::updateSimulation(int motorSpeed)
{
    // motorSpeed: -255 до 255
    // Максимальна швидкість: 20°/с при швидкості 255

    unsigned long currentTime = millis();
    unsigned long elapsed = currentTime - _lastSimulationUpdate;

    // Завжди оновлюємо час
    _lastSimulationUpdate = currentTime;

    // Оновлюємо показання датчика тільки якщо є рух
    if (elapsed > 0 && abs(motorSpeed) > 0)
    {
        // Обчислюємо зміну кута
        // 20°/с при motorSpeed = 255
        float maxDegreesPerSecond = 20.0;
        float degreesPerSecond = (motorSpeed / 255.0) * maxDegreesPerSecond;
        float deltaAngle = degreesPerSecond * (elapsed / 1000.0);

        // Оновлюємо симульований кут
        _simulatedAngle += deltaAngle;

        // Нормалізуємо
        while (_simulatedAngle < 0)
            _simulatedAngle += 360.0;
        while (_simulatedAngle >= 360.0)
            _simulatedAngle -= 360.0;

        // Додаємо випадкову помилку ±1° з точністю 0.1°
        float error = (random(21) - 10) / 10.0;
        _lastReadAngle = _simulatedAngle + error;

        // Нормалізуємо показання
        while (_lastReadAngle < 0)
            _lastReadAngle += 360.0;
        while (_lastReadAngle >= 360.0)
            _lastReadAngle -= 360.0;

        // Оновлюємо count
        int oldCount = count;
        count = (int)((_simulatedAngle / 360.0) * COUNTS_PER_REVOLUTION);

        // Оновлюємо accumulator
        int diff = count - oldCount;
        if (diff > COUNTS_PER_REVOLUTION / 2)
        {
            diff -= COUNTS_PER_REVOLUTION;
        }
        else if (diff < -COUNTS_PER_REVOLUTION / 2)
        {
            diff += COUNTS_PER_REVOLUTION;
        }
        accumulator += diff;

        // Обчислюємо RPM
        if (elapsed > 0)
        {
            rpm = (diff / (float)COUNTS_PER_REVOLUTION) * (SECONDS_PER_MINUTE * 1000 / (float)elapsed);
            if (abs(rpm) < rpmThreshold)
            {
                updateRPMFilter(rpm);
            }
        }
    }
}