#pragma once

#include <Arduino.h>
#include <vector>
#include <Stream.h>
#include "MT6701.h"
#include "AngleController.h"
#include "esp32led.h"

#define Vector std::vector

class SerialProcessor
{
private:
    AngleController &_controller;
    MT6701 &_sensor;
    Stream &_serial;
    ESP32LED &_led;
    String _responseBuffer; // Буфер для збору відповідей
    String _inputBuffer;    // Буфер для неблокуючого читання
    int _dePin;             // DE/RE пін (-1 = відключено)
    Vector<String> Split(const String &str, char delimiter);
    bool Contains(const String &str, const String &substring);
    int GetValue(const String &command, int index);
    void SendResponse(const String &response);
    void AddToResponseBuffer(const String &response);
    void sendRotationStatus(); // Відправка статусу обертання
    bool isValidCommand(const String &cmd); // Перевірка валідності команди

public:
    SerialProcessor(AngleController &controller, MT6701 &sensor, Stream &serial, ESP32LED &led, int dePin = -1);
    void handleSerialCommands();
    void processCommand(const String &command);
    void begin();
};