#include "interactionHeandler.h"
#include "DisplayUtils.h"

InteractionHeandler::InteractionHeandler() : u8g2(U8G2_R0, /* reset=*/U8X8_PIN_NONE, /* clock=*/15, /* data=*/16),
                                             keypad(makeKeymap(keys), rowPins, colPins, ROWS, COLS)
{
}

void InteractionHeandler::begin()
{
    for (int i = 0; i < ROWS; i++)
    {
        pinMode(rowPins[i], INPUT_PULLUP);
    }
    u8g2.begin();
    u8g2.setFont(u8g2_font_6x12_t_cyrillic);

    keypad.setDebounceTime(100); // Збільшуємо дебаунс
    keypad.setHoldTime(500);     // Час утримання

    // Використовуємо утиліту для анімації завантаження
    DisplayUtils::showLoadingAnimation(u8g2, "Завантаження", 4, 500);

    delay(500);
    DisplayUtils::displayInit(u8g2, keypad);
}

void InteractionHeandler::handle()
{
    char key = keypad.getKey();
    if (key)
    {
        if (key == '#')
        {
            u8g2.clearBuffer();
            u8g2.drawUTF8(5, 25, "#");
            u8g2.sendBuffer();
            SerialProcessor::sendRsSerial("$SAZ," + String(90) + ";$GENBL;");
            SerialProcessor::sendUsbSerial("$SAZ," + String(270) + ";$GENBL;");
        }
        else if (key == '*')
        {
            u8g2.clearBuffer();
            u8g2.drawUTF8(5, 25, "*");
            u8g2.sendBuffer();
            SerialProcessor::sendRsSerial("$SAZ," + String(270) + ";$GENBL;");
            SerialProcessor::sendUsbSerial("$SAZ," + String(270) + ";$GENBL;");
        }
        else if (key == '0')
        {
            u8g2.clearBuffer();
            u8g2.drawUTF8(5, 25, "0");
            u8g2.sendBuffer();
        }
        else if (key == '1')
        {
            SerialProcessor::sendRsSerial("$SENBL;");
        }

        KeyState state = keypad.getState();
        if (state == HOLD)
        {
        }
        delay(10);
    }
}