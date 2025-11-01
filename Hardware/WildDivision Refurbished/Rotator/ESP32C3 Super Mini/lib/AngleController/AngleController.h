#pragma once

#include <Arduino.h>
#include <EEPROM.h>
#include <cmath>
#include "DRV8871.h"
#include "MT6701.h"

#define EEPROM_SENSOR_ERROR_ADDR 0   // Адреса EEPROM для збереження SensorError
#define EEPROM_AZIMUTH_OFFSET_ADDR 4 // Адреса EEPROM для збереження AzimuthOffset

// Етапи ініціалізації
enum InitializationStage
{
    STAGE_0 = 0, // Початковий стан
    STAGE_1 = 1, // Рух до 0° позиції
    STAGE_2 = 2, // Налаштування похибки датчика
    STAGE_3 = 3, // Налаштування азимуту
    STAGE_4 = 4  // Ініціалізація завершена
};

// Типи помилок
enum ErrorType
{
    ERROR_NONE = 0,
    ERROR_SENSOR = 1,               // Помилка датчика
    ERROR_MOTOR_NOT_MOVING = 2,     // Мотор не обертається
    ERROR_MOTOR_WRONG_DIRECTION = 3 // Мотор обертається в неправильному напрямку
};

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

    // Система ініціалізації
    InitializationStage getInitializationStage();           // Отримати поточний етап ініціалізації
    void setInitializationStage(InitializationStage stage); // Встановити етап ініціалізації
    bool startInitialization(int stage);                    // Розпочати етап ініціалізації
    void adjustSensorError(char direction);                 // Коригувати похибку: 'L' = -1, 'R' = +1

    // Система азимуту
    void setAzimuthOffset(float azimuth); // Встановити offset азимуту
    float getAzimuthOffset();             // Отримати offset азимуту
    float angleToAzimuth(float angle);    // Перетворити кут в азимут
    float azimuthToAngle(float azimuth);  // Перетворити азимут в кут
    void moveToAzimuth(float azimuth);    // Рух до азимуту

    // Система помилок
    void setError(ErrorType sensorError, ErrorType motorError); // Встановити помилки
    ErrorType getSensorErrorType();                             // Отримати помилку датчика
    ErrorType getMotorError();                                  // Отримати помилку мотора
    String getStatusString();                                   // Отримати рядок статусу
    String getErrorString();                                    // Отримати рядок помилок

    // Система дозволів
    void enableMovement();        // Дозволити рух механізму
    void disableMovement();       // Заборонити рух механізму
    bool isMovementEnabled();     // Перевірити чи дозволено рух
    void moveToZeroWhenEnabled(); // Перемістити до 0° коли буде дозвіл

    // Статус системи
    bool isActive();         // Чи активне обертання
    float getCurrentSpeed(); // Поточна швидкість мотора

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

    // Система ініціалізації
    InitializationStage _initStage = STAGE_0; // Поточний етап ініціалізації

    // Система помилок
    ErrorType _sensorError = ERROR_NONE; // Помилка датчика
    ErrorType _motorError = ERROR_NONE;  // Помилка мотора

    static float SensorError;
    static float AzimuthOffset;
};