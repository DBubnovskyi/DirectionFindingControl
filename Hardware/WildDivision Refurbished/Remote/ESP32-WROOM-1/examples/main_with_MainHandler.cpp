#include <Arduino.h>
#include "MainHandler.h"

// Головний обробник системи
MainHandler *mainHandler;

void setup()
{
    // Створення та ініціалізація головного обробника
    mainHandler = new MainHandler();
    mainHandler->begin();
}

void loop()
{
    // Головний цикл обробки
    mainHandler->handle();

    delay(10);
}
