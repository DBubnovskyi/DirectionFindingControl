#pragma once

#include <Arduino.h>
#include <EEPROM.h>
#include <cmath>
#include "DRV8871.h"
#include "MT6701.h"

#define EEPROM_SENSOR_ERROR_ADDR 0   // Адреса EEPROM для збереження SensorError
#define EEPROM_AZIMUTH_OFFSET_ADDR 4 // Адреса EEPROM для збереження AzimuthOffset
#define EEPROM_TOLERANCE_ADDR 8      // Адреса EEPROM для збереження Tolerance
#define EEPROM_MIN_SPEED_ADDR 12     // Адреса EEPROM для збереження MinSpeed
#define EEPROM_MAX_SPEED_ADDR 16     // Адреса EEPROM для збереження MaxSpeed
#define EEPROM_BREACK_ANGLE_ADDR 20  // Адреса EEPROM для збереження BreackAngle

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

    static float Tolerance;    // Допуск для зупинки
    static float MinSpeed;     // Мінімальна швидкість руху
    static float MaxSpeed;     // Максимальна швидкість руху
    static float BreackAngle;  // Кут початку гальмування

    float getSensorAngle();
    void moveToAngle(float targetAngle); // Задати нову ціль
    void update();                       // Викликати в loop() для оновлення руху

    void setSensorError(float value); // Задати поправку сенсора
    float getSensorError();           // Отримати поправку сенсора

    // Параметри руху
    void setTolerance(float value);    // Встановити допуск зупинки
    float getTolerance();              // Отримати допуск зупинки
    void setMinSpeed(float value);     // Встановити мінімальну швидкість
    float getMinSpeed();               // Отримати мінімальну швидкість
    void setMaxSpeed(float value);     // Встановити максимальну швидкість
    float getMaxSpeed();               // Отримати максимальну швидкість
    void setBreackAngle(float value);  // Встановити кут гальмування
    float getBreackAngle();            // Отримати кут гальмування

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
    bool isActive();         // Чи дозволені операції (після ініціалізації)
    bool isRotating();       // Чи відбувається обертання
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
    bool _active = false;      // Дозвіл на операції (після ініціалізації)
    bool _isRotating = false;  // Статус обертання

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
    // Статичні параметри руху для збереження в EEPROM
};