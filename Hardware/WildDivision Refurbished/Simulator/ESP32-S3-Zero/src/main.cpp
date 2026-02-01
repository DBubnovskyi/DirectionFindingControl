#include <Arduino.h>
#include <EEPROM.h>
#include "esp32led.h"
#include "mt6701.h"
#include "drv8871.h"
#include "AngleController.h"
#include "SerialProcessor.h"
#include "OLEDDisplay.h"

#define Vector std::vector

// Симуляція - піни не використовуються фізично
#define SENSOR_SDA 3
#define SENSOR_SCL 4
#define IN1_PIN 1
#define IN2_PIN 2

ESP32LED led;
MT6701 angleSensor;
DRV8871 motorController;
AngleController *controller;
SerialProcessor *serialProc;
OLEDDisplay *oledDisplay;

unsigned long lastDisplayUpdate = 0;
const unsigned long DISPLAY_UPDATE_INTERVAL = 100; // Оновлення дисплея кожні 100мс

void setup()
{
  EEPROM.begin(512);

  Serial.begin(115200);
  Serial.println(F("\n\nESP8266 Motor Simulator"));
  Serial.println(F("========================"));

  // Ініціалізація LED
  led.begin();
  led.on();

  delay(100);

  // Ініціалізація OLED дисплея (U8g2 сам керує I2C)
  oledDisplay = new OLEDDisplay();
  oledDisplay->begin();

  delay(500);

  // Ініціалізація симульованих пристроїв
  motorController.begin(IN1_PIN, IN2_PIN);
  angleSensor.begin(SENSOR_SDA, SENSOR_SCL);

  // Створення контролера
  controller = new AngleController(motorController, angleSensor);

  // SerialProcessor через UART (USB)
  serialProc = new SerialProcessor(*controller, angleSensor, Serial, led, -1);
  serialProc->begin();

  led.off();

  Serial.println(F("Initialization complete"));
  Serial.println(F("Commands: #AN (angle), #ST (status), etc."));
  Serial.println(F("Max rotation speed: 5 deg/sec"));
  Serial.println(F("Positioning accuracy: ±1 degree"));
}

void loop()
{
  // Обробка команд
  serialProc->handleSerialCommands();

  // Оновлення контролера
  controller->update();

  // Оновлення симуляції датчика на основі швидкості мотора
  int motorSpeed = motorController.getSpeed();
  angleSensor.updateSimulation(motorSpeed);

  // Оновлення дисплея
  unsigned long currentMillis = millis();
  if (currentMillis - lastDisplayUpdate >= DISPLAY_UPDATE_INTERVAL)
  {
    lastDisplayUpdate = currentMillis;

    // Отримуємо азимути для відображення (0-360)
    float currentAngle = controller->getSensorAngle();
    float targetAngle = controller->getTargetAngle();
    float currentAzimuth = controller->angleToAzimuth(currentAngle);
    float targetAzimuth = controller->angleToAzimuth(targetAngle);
    bool isRotating = controller->isRotating();

    oledDisplay->update(currentAzimuth, targetAzimuth, isRotating, motorSpeed);
  }

  delay(10);
}