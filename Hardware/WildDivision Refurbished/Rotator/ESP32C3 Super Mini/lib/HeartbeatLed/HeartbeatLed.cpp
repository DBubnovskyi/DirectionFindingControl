#include "HeartbeatLed.h"

void HeartbeatLed::ensureInitialized()
{
    if (!_initialized)
    {
        pinMode(LED_PIN, OUTPUT);
        _initialized = true;
    }
}

void HeartbeatLed::on()
{
    ensureInitialized();
    digitalWrite(LED_PIN, HIGH);
}

void HeartbeatLed::off()
{
    ensureInitialized();
    digitalWrite(LED_PIN, LOW);
}

void HeartbeatLed::setBrightness(uint8_t brightness)
{
    ensureInitialized();
    analogWrite(LED_PIN, brightness);
}
