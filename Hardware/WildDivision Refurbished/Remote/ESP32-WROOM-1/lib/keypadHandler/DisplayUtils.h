#ifndef DISPLAY_UTILS_H
#define DISPLAY_UTILS_H

#include <Arduino.h>
#include <U8g2lib.h>
#include <Keypad.h>

class DisplayUtils
{
public:
    // Анімація завантаження з крапками
    static void showLoadingAnimation(U8G2 &display, const char *text, int steps = 4, int delay_ms = 500);
    static void displayInit(U8G2 &display, Keypad &keypad, int x = 0, int y = 25);

private:
};

#endif
