#include <Arduino.h>
#include "interactionHeandler.h"
#include "esp32LED.h"
#include "SerialProcessor.h"
#include "MainHandler.h"

ESP32LED led;
HardwareSerial RS485Serial(1);
InteractionHeandler *keypadHandler;
MainHandler *mainHandler;

void setup()
{
    Serial.begin(115200);
    RS485Serial.begin(9600, SERIAL_8N1, 27, 26); // RX=27, TX=26
    SerialProcessor::begin(&RS485Serial);

    keypadHandler = new InteractionHeandler();
    keypadHandler->begin();
    led.begin();
    led.off();

    mainHandler = new MainHandler(keypadHandler, &RS485Serial, &led);
}

void loop()
{
    mainHandler->handle();

    delay(10);
}