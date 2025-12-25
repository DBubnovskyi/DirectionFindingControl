#pragma once
#include <Arduino.h>
#include <U8g2lib.h>
#include <Keypad.h>
#include "SerialProcessor.h"

class InteractionHeandler
{
private:
    U8G2_SSD1306_128X64_NONAME_F_HW_I2C u8g2;

    static const byte ROWS = 4;
    static const byte COLS = 3;

    char keys[ROWS][COLS] = {
        {'1', '2', '3'},
        {'4', '5', '6'},
        {'7', '8', '9'},
        {'*', '0', '#'}};

    byte rowPins[ROWS] = {5, 21, 22, 23};
    byte colPins[COLS] = {17, 18, 19};

    Keypad keypad;

public:
    InteractionHeandler();
    void begin();
    void handle();
};