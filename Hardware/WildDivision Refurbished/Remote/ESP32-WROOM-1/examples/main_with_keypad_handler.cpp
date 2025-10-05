#include <Arduino.h>
#include <U8g2lib.h>
#include <HardwareSerial.h>
#include <Keypad.h>
#include "esp32LED.h"
#include "keypadHandler.h"

U8G2_SSD1306_128X64_NONAME_F_HW_I2C u8g2(U8G2_R0, /* reset=*/U8X8_PIN_NONE, /* clock=*/15, /* data=*/16);
ESP32LED led;

// Налаштування клавіатури
const byte ROWS = 4;
const byte COLS = 3;

char keys[ROWS][COLS] = {
    {'1', '2', '3'},
    {'4', '5', '6'},
    {'7', '8', '9'},
    {'*', '0', '#'}};

byte rowPins[ROWS] = {5, 21, 22, 23};
byte colPins[COLS] = {17, 18, 19};

Keypad keypad = Keypad(makeKeymap(keys), rowPins, colPins, ROWS, COLS);

// Створення обробника клавіатури
KeypadHandler keypadHandler(&keypad, &u8g2);

// RS485 налаштування
#define RX 27
#define TX 26
HardwareSerial RS485Serial(1);

// Callback функції для обробки клавіш
void onKeyPressedCallback(char key)
{
    Serial.print("Key pressed: ");
    Serial.println(key);

    // Можна додати логіку для відправки команд по RS485
    if (keypadHandler.getBufferLength() > 0)
    {
        String command = String("KEY:") + key;
        RS485Serial.println(command);
    }
}

void onKeyHeldCallback(char key)
{
    Serial.print("Key held: ");
    Serial.println(key);

    // Логіка для довгого натискання
    led.on();
}

void onSpecialKeyCallback(char key)
{
    Serial.print("Special key: ");
    Serial.println(key);

    if (key == '*')
    {
        Serial.println("Buffer cleared");
        led.off();
    }
    else if (key == '#')
    {
        String buffer = keypadHandler.getBuffer();
        Serial.print("Enter pressed. Buffer: ");
        Serial.println(buffer);

        // Відправка буфера по RS485
        if (buffer.length() > 0)
        {
            RS485Serial.println("CMD:" + buffer);
        }

        // Очистка буфера після відправки
        keypadHandler.clearBuffer();
    }
}

void setup()
{
    // Ініціалізація RS485
    RS485Serial.begin(115200, SERIAL_8N1, RX, TX);
    RS485Serial.setTimeout(100);

    // Ініціалізація пінів клавіатури
    for (int i = 0; i < ROWS; i++)
    {
        pinMode(rowPins[i], INPUT_PULLUP);
    }

    // Ініціалізація Serial
    Serial.begin(115200);
    Serial.setTimeout(100);

    // Ініціалізація LED
    led.begin();
    led.off();

    // Ініціалізація дисплея
    u8g2.begin();
    u8g2.clearBuffer();
    u8g2.setFont(u8g2_font_9x15_t_cyrillic);
    u8g2.drawStr(0, 10, "Keypad Handler");
    u8g2.sendBuffer();

    // Ініціалізація обробника клавіатури
    keypadHandler.begin();
    keypadHandler.setDisplayPosition(0, 30, 50);
    keypadHandler.setDisplayTimeout(5000); // 5 секунд

    // Встановлення callback функцій
    keypadHandler.setOnKeyPressed(onKeyPressedCallback);
    keypadHandler.setOnKeyHeld(onKeyHeldCallback);
    keypadHandler.setOnSpecialKey(onSpecialKeyCallback);

    delay(1000);
    u8g2.clearBuffer();
    u8g2.drawStr(0, 10, "Ready...");
    u8g2.sendBuffer();
    delay(500);
}

void loop()
{
    // Основна обробка клавіатури
    keypadHandler.handle();

    // Передача даних з RS485 на USB (безпечна версія)
    if (RS485Serial.available() > 0)
    {
        String message = RS485Serial.readStringUntil('\n');
        if (message.length() > 0)
        {
            message.trim();
            Serial.println("RS485: " + message);

            u8g2.clearBuffer();
            u8g2.drawStr(0, 10, "RS485:");
            u8g2.drawStr(0, 25, message.c_str());
            u8g2.sendBuffer();

            // Автоматичне очищення через 2 секунди
            delay(2000);
        }
    }

    // Передача даних з USB на RS485 (безпечна версія)
    if (Serial.available() > 0)
    {
        led.on();
        String message = Serial.readStringUntil('\n');
        if (message.length() > 0)
        {
            message.trim();
            RS485Serial.println(message);

            u8g2.clearBuffer();
            u8g2.drawStr(0, 10, "USB:");
            u8g2.drawStr(0, 25, message.c_str());
            u8g2.sendBuffer();

            // Автоматичне очищення через 2 секунди
            delay(2000);
        }
        led.off();
    }

    delay(10);
}
