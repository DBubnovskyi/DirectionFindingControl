#ifndef DISPLAY_UTILS_H
#define DISPLAY_UTILS_H

#include <Arduino.h>
#include <U8g2lib.h>
#include <Keypad.h>

class DisplayUtils
{
public:
    static void showLoadingAnimation(U8G2 &display, const char *text, int steps = 4, int delay_ms = 500);
    static void displayInit(U8G2 &display, Keypad &keypad, int x = 0, int y = 25);
    static void displayConnectionCheck(U8G2 &display);
    static void displayConnectionError(U8G2 &display);
    static void displayErrorAdjustment(U8G2 &display, int error);
    static void displayAzimuthInput(U8G2 &display, String input);
    static void displayOperational(U8G2 &display, int angle, int azimuth);
    static void displayStatusError(U8G2 &display, String errorText);

private:
};

#endif
