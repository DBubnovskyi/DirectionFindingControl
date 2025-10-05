#pragma once

#include <Arduino.h>
#include <vector>
#include <Stream.h>
#include <HardwareSerial.h>
#include "RotatorState.h"

#define Vector std::vector
#define RX 27
#define TX 26

class SerialProcessor
{
private:
    static String _responseBuffer;       // Буфер для збору відповідей
    static HardwareSerial *_rs485Serial; // Статичне посилання на RS485 порт
    static Vector<String> Split(const String &str, char delimiter);
    static bool Contains(const String &str, const String &substring);
    static int GetValue(const String &command, int index);
    static void SendResponse(const String &response);
    static void AddToResponseBuffer(const String &response);

public:
    static void begin(HardwareSerial *rs485Serial); // Ініціалізація з RS485 портом
    static void handleSerialCommands();
    static void processCommand(const String &command);
    static void sendUsbSerial(const String &message);
    static void sendRsSerial(const String &message);
};