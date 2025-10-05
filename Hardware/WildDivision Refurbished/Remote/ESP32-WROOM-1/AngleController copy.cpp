#include "AngleController.h"

float AngleController::SensorError = 0.0f;
const int EEPROM_SENSOR_ERROR_ADDR = 0;

AngleController::AngleController(DRV8871 &motor, MT6701 &sensor)
    : _motor(motor), _sensor(sensor)
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

float AngleController::flipAngle(float angle)
{
    return fmodf(360.0f - angle, 360.0f);
}

float AngleController::getSensorAngle()
{
    float value = _sensor.getAngleDegrees() + SensorError;
    value = value > 360 ? value - 360 : value < 0 ? 360 - value
                                                  : value;
    return flipAngle(value);
}

float AngleController::computeError(float target, float current)
{
    float error = target - current;
    while (error > 180.0)
        error -= 360.0;
    while (error < -180.0)
        error += 360.0;
    return error;
}

float AngleController::getInvAngle()
{
    float angle = getSensorAngle();
    return 360 - angle;
}

void AngleController::printAngle(float targetAngle, float currentAngle, float error, int speed)
{
    Serial.print("ANGLES,");
    Serial.print(currentAngle, 2);
    Serial.print(",");
    Serial.print(SensorError, 2);
    float s = 0;
    EEPROM.get(EEPROM_SENSOR_ERROR_ADDR, s);
    Serial.print(",");
    Serial.println(s, 2);
}

float AngleController::waveFunction(float x)
{
    if (x > 1.0)
        return 1.0;
    float numerator = std::sin(M_PI * x / 2.0);
    float denominator = std::sin(M_PI / 2.0);
    float y = std::pow(numerator / denominator, 2.0);
    return y;
}

void AngleController::moveToAngle(float targetAngle)
{
    const float startAngle = getSensorAngle();
    const float tolerance = 0.3;
    const int maxSpeed = 255;
    const int minSpeed = 175;
    const float angleRange = 15.0;

    float angle = getSensorAngle();
    float error = computeError(targetAngle, angle);

    while (true)
    {
        angle = getSensorAngle();
        error = computeError(targetAngle, angle);
        float initError = computeError(angle, startAngle);

        float axelerationStart = map(waveFunction(map(std::abs(error), 0, angleRange, 0, 1)), 0, 1, 0, maxSpeed - minSpeed);
        float axelerationEnd = map(waveFunction(map(std::abs(initError), 0, angleRange, 0, 1)), 0, 1, 0, maxSpeed - minSpeed);
        float axeleration = std::min(axelerationStart, axelerationEnd);
        float speed = axeleration + minSpeed;

        if ((error > 0 && !(targetAngle > 180 && startAngle < 180)) ||
            (error < 0 && (targetAngle < 180 && startAngle > 180)))
        {
        }
        else
        {
            speed = -speed;
        }

        if (speed > 255)
        {
            speed = 255;
        }
        if (speed < -255)
        {
            speed = -255;
        }

        if (error > tolerance || error < -tolerance)
        {
            _motor.setSpeed((int)speed);
            printAngle(targetAngle, angle, error, speed);
            delay(10);
        }
        else
        {
            Serial.println("Stop");
            speed = 0;
            _motor.setSpeed(speed);
            printAngle(targetAngle, angle, error, speed);
            _motor.stop();
            break;
        }

        printAngle(targetAngle, angle, error, speed);
    }

    _motor.setSpeed(0);
}