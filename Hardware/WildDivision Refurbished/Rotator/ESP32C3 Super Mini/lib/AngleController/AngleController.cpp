#include "AngleController.h"

float AngleController::SensorError = 0.0f;
float AngleController::AzimuthOffset = 0.0f;

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

    EEPROM.get(EEPROM_AZIMUTH_OFFSET_ADDR, AzimuthOffset);
    if (isnan(AzimuthOffset) || AzimuthOffset < 0.0f || AzimuthOffset >= 360.0f)
    {
        AzimuthOffset = 0.0f;
        EEPROM.put(EEPROM_AZIMUTH_OFFSET_ADDR, AzimuthOffset);
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
    // Перевірка дозволу на рух та ініціалізації
    if (!_movementEnabled || !_active)
    {
        return;
    }

    _targetAngle = normalizeAngle(targetAngle);
    _startAngle = normalizeAngle(getSensorAngle());
    _isRotating = true;
}

// Система дозволів
void AngleController::enableMovement()
{
    _movementEnabled = true;
    _active = true;  // Дозволяємо операції

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
    _active = false;  // Забороняємо операції

    // Зупиняємо поточний рух
    _motor.stop();
    _isRotating = false;
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
    // Перевірка дозволу на рух або ініціалізації
    if ((!_movementEnabled || !_active) && _isRotating)
    {
        _motor.stop();
        _isRotating = false;
        _currentSpeed = 0.0f;
        return;
    }

    if (_isRotating)
    {
        const float currentAngle = normalizeAngle(getSensorAngle());
        const float error = computeError(_targetAngle, currentAngle);
        const float passed = fabs(computeError(currentAngle, _startAngle));

        if (fabs(error) < Tolerance)
        {
            _motor.stop();
            _isRotating = false;
            _currentSpeed = 0.0f;
            return;
        }

        // прискорення від поточної та пройденої помилки (синусоїдальне)
        float errorNorm = constrain(fabs(error) / BreackAngle, 0.0f, 1.0f);
        float passedNorm = constrain(passed / BreackAngle, 0.0f, 1.0f);
        
        float accelStart = waveFunction(errorNorm) * (MaxSpeed - MinSpeed);
        float accelEnd = waveFunction(passedNorm) * (MaxSpeed - MinSpeed);

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

// Система ініціалізації
InitializationStage AngleController::getInitializationStage()
{
    return _initStage;
}

void AngleController::setInitializationStage(InitializationStage stage)
{
    _initStage = stage;
}

bool AngleController::startInitialization(int stage)
{
    if (stage == 1)
    {
        _initStage = STAGE_1;
        _active = true;  // Дозволяємо операції для ініціалізації
        enableMovement();
        moveToAngle(0);
        return true;
    }
    else if (stage == 2)
    {
        _initStage = STAGE_2;
        return true;
    }
    else if (stage == 3)
    {
        _initStage = STAGE_3;
        return true;
    }
    else if (stage == 4)
    {
        _initStage = STAGE_4;
        return true;
    }
    return false;
}

void AngleController::adjustSensorError(char direction)
{
    if (direction == 'L')
    {
        setSensorError(SensorError - 1.0f);
    }
    else if (direction == 'R')
    {
        setSensorError(SensorError + 1.0f);
    }
}

// Система азимуту
void AngleController::setAzimuthOffset(float azimuth)
{
    AzimuthOffset = fmod(azimuth, 360.0f);
    if (AzimuthOffset < 0.0f)
        AzimuthOffset += 360.0f;
    EEPROM.put(EEPROM_AZIMUTH_OFFSET_ADDR, AzimuthOffset);
    EEPROM.commit();
}

float AngleController::getAzimuthOffset()
{
    return AzimuthOffset;
}

float AngleController::angleToAzimuth(float angle)
{
    float azimuth = angle + AzimuthOffset;
    azimuth = fmod(azimuth, 360.0f);
    if (azimuth < 0.0f)
        azimuth += 360.0f;
    return azimuth;
}

float AngleController::azimuthToAngle(float azimuth)
{
    float angle = azimuth - AzimuthOffset;
    angle = fmod(angle, 360.0f);
    if (angle < 0.0f)
        angle += 360.0f;
    return angle;
}

void AngleController::moveToAzimuth(float azimuth)
{
    float angle = azimuthToAngle(azimuth);
    moveToAngle(angle);
}

// Система помилок
void AngleController::setError(ErrorType sensorError, ErrorType motorError)
{
    _sensorError = sensorError;
    _motorError = motorError;

    // При будь-якій помилці відключаємо рух
    if (_sensorError != ERROR_NONE || _motorError != ERROR_NONE)
    {
        disableMovement();
        Serial.print("ERROR,");
        Serial.print((int)_sensorError);
        Serial.print(",");
        Serial.println((int)_motorError);
    }
}

ErrorType AngleController::getSensorErrorType()
{
    return _sensorError;
}

ErrorType AngleController::getMotorError()
{
    return _motorError;
}

String AngleController::getStatusString()
{
    String status = "ST,ROTATOR_ESP-32C3_V0.1.0;ERROR,";
    status += String((int)_sensorError);
    status += ",";
    status += String((int)_motorError);
    return status;
}

String AngleController::getErrorString()
{
    String error = "ERROR,";
    error += String((int)_sensorError);
    error += ",";
    error += String((int)_motorError);
    return error;
}

// Статус системи
bool AngleController::isActive()
{
    return _active;
}

bool AngleController::isRotating()
{
    return _isRotating;
}

float AngleController::getCurrentSpeed()
{
    return _currentSpeed;
}