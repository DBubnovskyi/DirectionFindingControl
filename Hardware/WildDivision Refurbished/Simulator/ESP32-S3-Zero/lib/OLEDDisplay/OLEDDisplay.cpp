#include "OLEDDisplay.h"

OLEDDisplay::OLEDDisplay()
{
    u8g2 = new U8G2_SSD1306_128X64_NONAME_F_SW_I2C(U8G2_R0, 12, 14, U8X8_PIN_NONE);
}

void OLEDDisplay::begin()
{
    u8g2->begin();
    u8g2->setFont(u8g2_font_6x10_tr);

    u8g2->clearBuffer();
    u8g2->setCursor(10, 10);
    u8g2->print(F("SIMULATOR"));
    u8g2->setCursor(5, 35);
    u8g2->print(F("ESP8266 12F"));
    u8g2->setCursor(5, 50);
    u8g2->print(F("Motor Control"));
    u8g2->sendBuffer();
    delay(2000);

    Serial.println(F("OLED initialized with U8g2"));
}

void OLEDDisplay::update(float currentAngle, float targetAngle, bool isRotating, int motorSpeed)
{
    u8g2->clearBuffer();
    u8g2->setFont(u8g2_font_6x10_tr);

    // Заголовок
    u8g2->setCursor(0, 10);
    u8g2->print(F("MOTOR SIMULATOR"));
    u8g2->drawLine(0, 12, 128, 12);

    // Поточний азимут (0-360)
    u8g2->setCursor(0, 23);
    u8g2->print(F("Azimuth: "));
    u8g2->print(currentAngle, 1);
    u8g2->print(F("*"));

    // Цільовий азимут (0-360)
    u8g2->setCursor(0, 33);
    u8g2->print(F("Target:  "));
    u8g2->print(targetAngle, 1);
    u8g2->print(F("*"));

    // Статус
    u8g2->setCursor(0, 43);
    u8g2->print(F("Status: "));
    u8g2->print(isRotating ? F("ROTATING") : F("STOPPED"));

    // Швидкість мотора
    u8g2->setCursor(0, 53);
    u8g2->print(F("Speed: "));
    u8g2->print(abs(motorSpeed));

    // Напрямок
    u8g2->setCursor(0, 63);
    u8g2->print(F("Dir: "));
    if (motorSpeed > 0)
    {
        u8g2->print(F("CW"));
    }
    else if (motorSpeed < 0)
    {
        u8g2->print(F("CCW"));
    }
    else
    {
        u8g2->print(F("---"));
    }

    // Візуальний індикатор обертання
    if (isRotating)
    {
        int barWidth = map(abs(motorSpeed), 0, 255, 0, 50);
        u8g2->drawBox(78, 56, barWidth, 7);
    }

    u8g2->sendBuffer();
}

void OLEDDisplay::displayError(String error)
{
    u8g2->clearBuffer();
    u8g2->setFont(u8g2_font_6x10_tr);
    u8g2->setCursor(0, 10);
    u8g2->print(F("ERROR:"));
    u8g2->setCursor(0, 30);
    u8g2->print(error);
    u8g2->sendBuffer();
}

void OLEDDisplay::displayInitialization(int stage)
{
    u8g2->clearBuffer();
    u8g2->setFont(u8g2_font_6x10_tr);
    u8g2->setCursor(0, 10);
    u8g2->print(F("INITIALIZATION"));
    u8g2->drawLine(0, 12, 128, 12);

    u8g2->setCursor(0, 25);
    u8g2->print(F("Stage: "));
    u8g2->print(stage);

    // Прогрес бар
    int progress = map(stage, 0, 4, 0, 128);
    u8g2->drawFrame(0, 40, 128, 10);
    u8g2->drawBox(0, 40, progress, 10);

    u8g2->sendBuffer();
}

void OLEDDisplay::displayStatus(String status)
{
    u8g2->clearBuffer();
    u8g2->setFont(u8g2_font_6x10_tr);
    u8g2->setCursor(0, 10);
    u8g2->print(F("STATUS:"));
    u8g2->setCursor(0, 30);
    u8g2->print(status);
    u8g2->sendBuffer();
}

void OLEDDisplay::clear()
{
    u8g2->clearBuffer();
    u8g2->sendBuffer();
}

void OLEDDisplay::drawHeader()
{
    u8g2->setFont(u8g2_font_6x10_tr);
    u8g2->setCursor(0, 10);
    u8g2->print(F("MOTOR SIMULATOR"));
    u8g2->drawLine(0, 12, 128, 12);
}
