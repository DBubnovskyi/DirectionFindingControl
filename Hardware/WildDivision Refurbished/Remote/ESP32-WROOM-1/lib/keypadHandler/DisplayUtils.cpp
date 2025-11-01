#include "DisplayUtils.h"

void DisplayUtils::showLoadingAnimation(U8G2 &display, const char *text, int steps, int delay_ms)
{
    for (int i = 0; i <= steps; i++)
    {
        display.clearBuffer();

        String fullText = String(text);
        for (int j = 0; j < i; j++)
        {
            fullText += ".";
        }

        display.drawUTF8(0, 10, fullText.c_str());
        display.sendBuffer();
        delay(delay_ms);
    }
}

void DisplayUtils::displayInit(U8G2 &display, Keypad &keypad, int x, int y)
{
    display.clearBuffer();
    display.drawUTF8(x, y, "Підготовка роботи,");
    display.drawUTF8(x, y + 12, "після натискання #");
    display.drawUTF8(x, y + 24, "почнеться обертання");
    display.drawUTF8(x, y + 36, "в нульове положення");
    display.sendBuffer();
}

void DisplayUtils::displayConnectionCheck(U8G2 &display)
{
    display.clearBuffer();
    display.drawUTF8(0, 25, "Перевірка з'єднання");
    display.drawUTF8(0, 37, "з пристроєм...");
    display.sendBuffer();
}

void DisplayUtils::displayConnectionError(U8G2 &display)
{
    display.clearBuffer();
    display.drawUTF8(0, 25, "Помилка з'єднання");
    display.drawUTF8(0, 37, "Перевірте кабель");
    display.drawUTF8(0, 49, "Повторна перевірка");
    display.sendBuffer();
}

void DisplayUtils::displayErrorAdjustment(U8G2 &display, int error)
{
    display.clearBuffer();
    display.drawUTF8(0, 25, "Нульове положення");
    display.drawUTF8(0, 37, ("Похибка: " + String(error)).c_str());
    display.drawUTF8(0, 49, "4-ліво 6-право #-далі");
    display.sendBuffer();
}

void DisplayUtils::displayAzimuthInput(U8G2 &display, String input)
{
    display.clearBuffer();
    display.drawUTF8(0, 25, "Введіть азимут:");
    display.drawUTF8(0, 37, ("Азимут: " + input + "°").c_str());
    display.drawUTF8(0, 49, "Цифри 0-359, #-готово");
    display.sendBuffer();
}

void DisplayUtils::displayOperational(U8G2 &display, int angle, int azimuth)
{
    display.clearBuffer();
    display.drawUTF8(0, 25, ("Кут: " + String(angle) + "°").c_str());
    display.drawUTF8(0, 37, ("Азимут: " + String(azimuth) + "°").c_str());
    display.drawUTF8(0, 49, "<4     6> 0-аз 8-кут");
    display.sendBuffer();
}

void DisplayUtils::displayStatusError(U8G2 &display, String errorText)
{
    display.clearBuffer();
    display.drawUTF8(0, 25, "Помилка стану:");
    display.drawUTF8(0, 37, errorText.c_str());
    display.drawUTF8(0, 49, "Перевірте підключення");
    display.sendBuffer();
}
