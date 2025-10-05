#pragma once

#include <Arduino.h>
#include <EEPROM.h>
#include <cmath>
#include "DRV8871.h"
#include "MT6701.h"

#define EEPROM_SENSOR_ERROR_ADDR 0 // Адреса EEPROM для збереження SensorError

class AngleController
{
public:
    AngleController(DRV8871 &motor, MT6701 &sensor);

    float Tolerance = 0.5f;    // Допуск для зупинки
    float MinSpeed = 155.0f;   // Мінімальна швидкість руху
    float MaxSpeed = 255.0f;   // Максимальна швидкість руху
    float BreackAngle = 15.0f; // Максимальна швидкість руху

    float getSensorAngle();
    void moveToAngle(float targetAngle); // Задати нову ціль
    void update();                       // Викликати в loop() для оновлення руху

    void setSensorError(float value); // Задати поправку сенсора
    float getSensorError();           // Отримати поправку сенсора

    // Система дозволів
    void enableMovement();        // Дозволити рух механізму
    void disableMovement();       // Заборонити рух механізму
    bool isMovementEnabled();     // Перевірити чи дозволено рух
    void moveToZeroWhenEnabled(); // Перемістити до 0° коли буде дозвіл

private:                                             // Отримати поточний кут з сенсора
    float computeError(float target, float current); // Обчислити помилку
    float waveFunction(float x);                     // Функція для плавного прискорення
    float mirrorAngle(float angle);

    DRV8871 &_motor;
    MT6701 &_sensor;

    float _targetAngle;
    float _startAngle;
    float _currentSpeed;
    float _lastDirection;
    float _brakeAngle;
    bool _isBraking;
    bool _active = false;

    // Система дозволів
    bool _movementEnabled = false; // За замовчуванням рух заборонено
    bool _pendingZeroMove = false; // Чи очікує переміщення до 0°

    static float SensorError;
};