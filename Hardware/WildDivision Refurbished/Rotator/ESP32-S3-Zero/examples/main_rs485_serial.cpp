// Приклад використання SerialProcessor з RS485
#include <Arduino.h>
#include <EEPROM.h>
#include "esp32led.h"
#include <Wire.h>
#include "mt6701.h"
#include "drv8871.h"
#include "AngleController.h"
#include "SerialProcessor.h"

#define Vector std::vector

// Налаштування RS485
#define RX_PIN 7
#define TX_PIN 8
HardwareSerial RS485Serial(1);

ESP32LED led;
MT6701 angleSensor;
DRV8871 motorController;
AngleController *controller;
SerialProcessor *serialProc;

void setup()
{
    // Ініціалізація USB Serial для налагодження
    Serial.begin(115200);
    EEPROM.begin(512);
    led.begin();

    // Ініціалізація RS485 Serial
    RS485Serial.begin(115200, SERIAL_8N1, RX_PIN, TX_PIN);

    delay(100);

    controller = new AngleController(motorController, angleSensor);

    // Використання RS485 Serial порту
    serialProc = new SerialProcessor(*controller, angleSensor, RS485Serial);

    serialProc->begin();
    motorController.begin(7, 6); // (IN1, IN2)

    led.begin();
    led.on();

    angleSensor.begin(1, 0); // (SDA, SCL)
    led.off();
    controller->moveToAngle(0);

    Serial.println("Sistema готова. Використовуємо RS485 Serial порт.");
}

void loop()
{
    serialProc->handleSerialCommands();
    controller->update();
    delay(10);
}
