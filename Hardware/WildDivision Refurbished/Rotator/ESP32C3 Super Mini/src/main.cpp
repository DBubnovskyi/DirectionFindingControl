#include <Arduino.h>
#include <EEPROM.h>
#include "esp32led.h"
#include <Wire.h>
#include "mt6701.h"
#include "drv8871.h"
#include "AngleController.h"
#include "SerialProcessor.h"

#define Vector std::vector

// Налаштування сенсора
#define SENSOR_SDA 1
#define SENSOR_SCL 0

// Налаштування драйвера мотору
#define IN1_PIN 10
#define IN2_PIN 7

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
  EEPROM.begin(512);
  led.begin();

  delay(100);

  controller = new AngleController(motorController, angleSensor);

  RS485Serial.begin(9600, SERIAL_8N1, RX_PIN, TX_PIN);
  Serial.begin(115200);
  // SerialProcessor приймає будь-який Stream об'єкт Serial чи HardwareSerial
  serialProc = new SerialProcessor(*controller, angleSensor, RS485Serial, led);

  serialProc->begin();
  motorController.begin(IN1_PIN, IN2_PIN); // (IN1, IN2)

  led.begin();
  led.on();

  angleSensor.begin(SENSOR_SDA, SENSOR_SCL); // (SDA, SCL)
  led.off();

  // controller->moveToAngle(0); // НЕ переміщуємо одразу!
}

void loop()
{
  serialProc->handleSerialCommands();
  controller->update();
  delay(10);
}