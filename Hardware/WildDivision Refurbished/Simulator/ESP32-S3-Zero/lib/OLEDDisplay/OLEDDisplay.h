#pragma once
#include <Arduino.h>
#include <U8g2lib.h>
#include <Wire.h>

class OLEDDisplay
{
public:
    OLEDDisplay();
    void begin();
    void update(float currentAngle, float targetAngle, bool isRotating, int motorSpeed);
    void displayError(String error);
    void displayInitialization(int stage);
    void displayStatus(String status);
    void clear();

private:
    U8G2_SSD1306_128X64_NONAME_F_SW_I2C *u8g2;
    void drawHeader();
};
