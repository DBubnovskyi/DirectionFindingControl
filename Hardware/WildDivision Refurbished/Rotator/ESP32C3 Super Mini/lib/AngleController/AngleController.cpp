#include "AngleController.h"

float AngleController::SensorError = 0.0f;

AngleController::AngleController(DRV8871 &motor, MT6701 &sensor)
    : _motor(motor), _sensor(sensor), _targetAngle(0.0f), _currentSpeed(0.0f)
{
    EEPROM.get(EEPROM_SENSOR_ERROR_ADDR, SensorError);
    if (isnan(SensorError) || SensorError < -180.0f || SensorError > 180.0f)
    {
        SensorError = 0.0f;
        EEPROM.put(EEPROM_SENSOR_ERROR_ADDR, SensorError);
        EEPROM.commit();
    }
}

void AngleController::setSensorError(float value)
{
    SensorError = value;
    EEPROM.put(EEPROM_SENSOR_ERROR_ADDR, SensorError);
    EEPROM.commit();
}

float AngleController::getSensorError()
{
    return SensorError;
}

float AngleController::getSensorAngle()
{
    float rawAngle = _sensor.getAngleDegrees();
    float corrected = rawAngle + SensorError;
    if (corrected >= 360.0f)
        corrected -= 360.0f;
    if (corrected < 0.0f)
        corrected += 360.0f;
    return mirrorAngle(corrected);
}

float AngleController::mirrorAngle(float angle)
{
    angle = fmod(angle, 360.0f); // нормалізація у 0..360
    if (angle < 0)
        angle += 360.0f;
    return fmod(360.0f - angle, 360.0f);
}

float AngleController::computeError(float target, float current)
{
    float error = target - current;
    if (error > 180.0f)
        error -= 360.0f;
    if (error < -180.0f)
        error += 360.0f;
    return error;
}

float AngleController::waveFunction(float x)
{
    return (x >= 1.0f) ? 1.0f : pow(sin(M_PI * x / 2.0f), 2);
}

float normalizeAngle(float angle)
{
    angle = fmod(angle, 360.0f); // нормалізація у 0..360
    if (angle < 0.0f)
        angle += 360.0f;
    if (angle > 180.0f)
        angle -= 360.0f;

    if (angle < -178.0f)
        angle = -178.0f;
    if (angle > 178.0f)
        angle = 178.0f;

    return angle;
}

void AngleController::moveToAngle(float targetAngle)
{
    // Перевірка дозволу на рух
    if (!_movementEnabled)
    {
        return;
    }

    _targetAngle = normalizeAngle(targetAngle);
    _startAngle = normalizeAngle(getSensorAngle());
    _active = true;
}

// Система дозволів
void AngleController::enableMovement()
{
    _movementEnabled = true;

    // Якщо є відкладене переміщення до 0°
    if (_pendingZeroMove)
    {
        _pendingZeroMove = false;
        moveToAngle(0);
    }
}

void AngleController::disableMovement()
{
    _movementEnabled = false;
    _pendingZeroMove = false;

    // Зупиняємо поточний рух
    _motor.stop();
    _active = false;
    _currentSpeed = 0.0f;
}

bool AngleController::isMovementEnabled()
{
    return _movementEnabled;
}

void AngleController::moveToZeroWhenEnabled()
{
    if (_movementEnabled)
    {
        moveToAngle(0);
    }
    else
    {
        _pendingZeroMove = true;
    }
}

void AngleController::update()
{
    // Перевірка дозволу на рух
    if (!_movementEnabled && _active)
    {
        _motor.stop();
        _active = false;
        _currentSpeed = 0.0f;
        return;
    }

    if (_active)
    {
        const float currentAngle = normalizeAngle(getSensorAngle());
        const float error = computeError(_targetAngle, currentAngle);
        const float passed = fabs(computeError(currentAngle, _startAngle));

        if (fabs(error) < Tolerance)
        {
            _motor.stop();
            _active = false;
            _currentSpeed = 0.0f;
            return;
        }

        // прискорення від поточної та пройденої помилки
        float accelStart = map(waveFunction(map(fabs(error), 0, BreackAngle, 0, 1)), 0, 1, 0, MaxSpeed - MinSpeed);
        float accelEnd = map(waveFunction(map(passed, 0, BreackAngle, 0, 1)), 0, 1, 0, MaxSpeed - MinSpeed);

        _currentSpeed = std::min(accelStart, accelEnd) + MinSpeed;

        // напрямок задає знак error
        _currentSpeed = (error >= 0) ? _currentSpeed : -_currentSpeed;

        // перевірка чи потрібно інвертувати напрямок щоб уникнути проходу через 180°
        float sign_current = (currentAngle > 0.0f) ? 1.0f : ((currentAngle < 0.0f) ? -1.0f : 0.0f);
        float sign_target = (_targetAngle > 0.0f) ? 1.0f : ((_targetAngle < 0.0f) ? -1.0f : 0.0f);
        float sign_error = (error > 0.0f) ? 1.0f : ((error < 0.0f) ? -1.0f : 0.0f);
        if (sign_current * sign_target < 0.0f && sign_error == sign_current)
        {
            _currentSpeed = -_currentSpeed;
        }

        // обмеження [-255; 255]
        _currentSpeed = std::max(-255.0f, std::min(255.0f, _currentSpeed));
        _motor.setSpeed(_currentSpeed);
    }
}