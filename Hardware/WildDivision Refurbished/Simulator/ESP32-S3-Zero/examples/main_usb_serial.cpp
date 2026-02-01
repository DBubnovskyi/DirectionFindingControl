// Приклад використання SerialProcessor з USB Serial порт
#include <Arduino.h>
#include <EEPROM.h>
#include "esp32led.h"
#include <Wire.h>
#include "mt6701.h"
#include "drv8871.h"
#include "AngleController.h"
#include "SerialProcessor.h"

#define Vector std::vector

ESP32LED led;
MT6701 angleSensor;
DRV8871 motorController;
AngleController *controller;
SerialProcessor *serialProc;

void setup()
{
    // Ініціалізація USB Serial
    Serial.begin(115200);
    EEPROM.begin(512);
    led.begin();

    delay(100);

    controller = new AngleController(motorController, angleSensor);

    // Використання USB Serial порту (Serial)
    serialProc = new SerialProcessor(*controller, angleSensor, Serial);

    serialProc->begin();
    motorController.begin(7, 6); // (IN1, IN2)

    led.begin();
    led.on();

    angleSensor.begin(1, 0); // (SDA, SCL)
    led.off();
    controller->moveToAngle(0);

    Serial.println("Sistema готова. Використовуємо USB Serial порт.");
}

void loop()
{
    serialProc->handleSerialCommands();
    controller->update();
    delay(10);
}
