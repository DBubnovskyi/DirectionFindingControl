#include <Arduino.h>
#include "ScreenHandler.h"
#include "KeypadHandler.h"
#include "SerialHandler.h"

ScreenHandler *screenHandler = new ScreenHandler(4, 2);
KeypadHandler *keypadHandler = new KeypadHandler();
SerialHandler *serialHandler = new SerialHandler();
HardwareSerial RS485Serial(1);

void setup()
{
    Serial.begin(115200);
    RS485Serial.begin(115200, SERIAL_8N1, 9, 10);
    screenHandler->begin();

    keypadHandler->begin(screenHandler);

    serialHandler->begin(&RS485Serial, screenHandler);
}

void loop()
{
    keypadHandler->handle();
    serialHandler->handle();
    delay(1);
}