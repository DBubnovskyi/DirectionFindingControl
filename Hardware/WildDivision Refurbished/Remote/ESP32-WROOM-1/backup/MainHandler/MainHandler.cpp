#include "MainHandler.h"

MainHandler::MainHandler(InteractionHeandler *keypadHandler, HardwareSerial *rs485Serial, ESP32LED *led)
    : _keypadHandler(keypadHandler),
      _rs485Serial(rs485Serial),
      _led(led)
{
}

MainHandler::~MainHandler()
{
}

void MainHandler::handle()
{
}
