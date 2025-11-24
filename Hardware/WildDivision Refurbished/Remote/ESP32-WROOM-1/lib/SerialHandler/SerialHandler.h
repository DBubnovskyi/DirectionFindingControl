#pragma once

#include <Arduino.h>
#include <vector>
#include <Stream.h>
#include <HardwareSerial.h>
#include "RotatorState.h"
#include "StringUtils.h"
#include "ScreenHandler.h"

#define Vector std::vector
#define RX 27
#define TX 26

class SerialHandler
{
private:
    static String _responseBuffer;        // Буфер для збору відповідей
    static HardwareSerial *_rs485Serial;  // Статичне посилання на RS485 порт
    static ScreenHandler *_screenHandler; // Статичне посилання на ScreenHandler
    static void SendResponse(const String &response);
    static void AddToResponseBuffer(const String &response);

public:
    static void begin(HardwareSerial *rs485Serial, ScreenHandler *screenHandler);
    static void handle();
    static void processCommand(const String &command);
    static void processResponce(const String &command);
    static void sendUsbSerial(const String &message);
    static void sendRsSerial(const String &message);
};