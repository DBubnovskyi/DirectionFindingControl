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
#define RX_PIN 2
#define TX_PIN 3
HardwareSerial RS485Serial(1);

ESP32LED led;
MT6701 angleSensor;
DRV8871 motorController;
AngleController *controller;
SerialProcessor *serialProc;

void setup()
{
  // Ініціалізація USB Serial
  // Serial.begin(115200);
  EEPROM.begin(512);
  led.begin();

  delay(100);

  controller = new AngleController(motorController, angleSensor);

  RS485Serial.begin(115200, SERIAL_8N1, RX_PIN, TX_PIN);
  // Використання USB Serial порту (Serial)
  // Тепер SerialProcessor приймає будь-який Stream об'єкт!
  serialProc = new SerialProcessor(*controller, angleSensor, RS485Serial);

  serialProc->begin();
  motorController.begin(6, 7); // (IN1, IN2)

  led.begin();
  led.on();

  angleSensor.begin(1, 0); // (SDA, SCL)
  led.off();

  // controller->moveToAngle(0); // НЕ переміщуємо одразу!
}

void loop()
{
  serialProc->handleSerialCommands();
  controller->update();
  delay(10);
}