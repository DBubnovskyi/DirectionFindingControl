#pragma once

#include <Arduino.h>
#include <Keypad.h>
#include "ScreenHandler.h"

class KeypadHandler
{
private:
    // Конфігурація клавіатури
    static const byte ROWS = 4;
    static const byte COLS = 3;

    char keys[ROWS][COLS] = {
        {'1', '2', '3'},
        {'4', '5', '6'},
        {'7', '8', '9'},
        {'*', '0', '#'}};

    byte rowPins[ROWS] = {13, 16, 11, 34};
    byte colPins[COLS] = {18, 12, 15};

    Keypad keypad;

    // Посилання на екран
    ScreenHandler *screenHandler;

    // Буфер для введення числа
    String inputBuffer;

public:
    KeypadHandler();

    void begin(ScreenHandler *screen);
    void handle();
};