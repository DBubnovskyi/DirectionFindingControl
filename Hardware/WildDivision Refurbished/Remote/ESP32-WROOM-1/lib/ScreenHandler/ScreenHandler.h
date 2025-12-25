#pragma once

#include <Arduino.h>
#include <U8g2lib.h>
#include <map>
#include <array>

#define MAX_LINES 5

class ScreenHandler
{
private:
    // Map екранів з іменованими ключами
    std::map<String, std::array<String, MAX_LINES>> screens;

    // Поточний активний екран
    String currentScreenKey;

    // Дисплей
    U8G2_SSD1306_128X64_NONAME_F_HW_I2C u8g2;

public:
    ScreenHandler(byte clock = 15, byte data = 16);
    void begin();
    void showScreen(String screenKey, String *data = nullptr);
    String getCurrentScreenKey() { return currentScreenKey; }

private:
    String replaceKeys(String text, String *data);
    void initializeScreens();
};