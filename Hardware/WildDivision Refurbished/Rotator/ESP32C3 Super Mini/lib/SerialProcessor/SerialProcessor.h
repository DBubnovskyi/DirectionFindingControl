#pragma once

#include <Arduino.h>
#include <vector>
#include <Stream.h>
#include "MT6701.h"
#include "AngleController.h"

#define Vector std::vector

class SerialProcessor
{
private:
    AngleController &_controller;
    MT6701 &_sensor;
    Stream &_serial;
    String _responseBuffer; // Буфер для збору відповідей
    Vector<String> Split(const String &str, char delimiter);
    bool Contains(const String &str, const String &substring);
    int GetValue(const String &command, int index);
    void SendResponse(const String &response);
    void AddToResponseBuffer(const String &response);

public:
    SerialProcessor(AngleController &controller, MT6701 &sensor, Stream &serial);
    void handleSerialCommands();
    void processCommand(const String &command);
    void sendCurrentAngles();
    void begin();
};