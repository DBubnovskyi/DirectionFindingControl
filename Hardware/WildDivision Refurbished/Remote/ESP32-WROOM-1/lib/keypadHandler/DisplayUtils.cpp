#include "DisplayUtils.h"

void DisplayUtils::showLoadingAnimation(U8G2 &display, const char *text, int steps, int delay_ms)
{
    for (int i = 0; i <= steps; i++)
    {
        display.clearBuffer();

        // Створюємо повний рядок з текстом та крапками
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
