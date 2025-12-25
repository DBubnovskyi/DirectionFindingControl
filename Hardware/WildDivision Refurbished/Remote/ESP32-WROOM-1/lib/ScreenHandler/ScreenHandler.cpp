#include "ScreenHandler.h"

ScreenHandler::ScreenHandler(byte clock, byte data) : u8g2(U8G2_R0, /* reset=*/U8X8_PIN_NONE, /* clock=*/clock, /* data=*/data)
{
    currentScreenKey = "main";
}

void ScreenHandler::begin()
{
    u8g2.begin();
    u8g2.setFont(u8g2_font_6x12_t_cyrillic);

    initializeScreens();
    showScreen("init0");
}

void ScreenHandler::showScreen(String screenKey, String *data)
{
    if (screens.find(screenKey) == screens.end())
        return;

    currentScreenKey = screenKey;

    u8g2.clearBuffer();

    for (int line = 0; line < MAX_LINES; line++)
    {
        String displayText = screens[screenKey][line];

        if (data != nullptr)
        {
            displayText = replaceKeys(displayText, data);
        }

        u8g2.drawUTF8(0, (line + 1) * 12, displayText.c_str());
    }

    u8g2.sendBuffer();
}

String ScreenHandler::replaceKeys(String text, String *data)
{
    if (data == nullptr)
        return text;

    text.replace("{0}", data[0]);
    text.replace("{1}", data[1]);
    text.replace("{2}", data[2]);
    text.replace("{3}", data[3]);
    text.replace("{4}", data[4]);

    return text;
}

void ScreenHandler::initializeScreens()
{
    screens["init0"] = {"== ІНІЦІАЛІЗАЦІЯ 1 ==", "Підготовка до роботи", "після натискання #", "почнеться обертання", "в нульове положення"};
    screens["init1-2"] = {"== Поворот в нуль  ==", "Виконується поворот", "в нульове положення", "", "* назад"};
    screens["init2"] = {"== ІНІЦІАЛІЗАЦІЯ 2 ==", "Корекція датчика", "Похибка: {0}", "< 4 ліво    право 6 >", "             далі #"};
    screens["init3"] = {"== ІНІЦІАЛІЗАЦІЯ 3 ==", "Поточний азимут", "Задається цифрами", "Азимут: {0}", "             далі #"};
    screens["init4"] = {"== ІНІЦІАЛІЗАЦІЯ 4 ==", "Кут: {0}", "Азимут: {1}", "Похибка: {2}", "     для завершення #"};

    screens["main"] = {"--------МЕНЮ----------", "1-Азимут", "2-Кут", "3-Налаштування", "4-Інформація"};
    screens["azimuth"] = {"-------АЗИМУТ--------", "Поточний: {0}", "Задати азимут: {1}", "", "* назад    задати #"};
    screens["angle"] = {"--------КУТ----------", "Поточна: {0}", "Задати кут: {1}", "", "* назад    задати #"};
    screens["settings"] = {"----НАЛАШТУВАННЯ----", "", "", "", "* назад"};
    screens["info"] = {"-----ІНФОРМАЦІЯ------", "Кут: {0}", "Азимут: {1}", "Похибка: {2}", "* назад"};

    screens["test"] = {"1234567890_234567890", "Батарея: {0}%", "Зв'язок: {1}", "Версія: {2}", "* назад    задати #"};
}