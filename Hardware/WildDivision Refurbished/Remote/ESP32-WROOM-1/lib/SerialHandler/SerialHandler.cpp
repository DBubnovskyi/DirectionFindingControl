#include "SerialHandler.h"

// Ініціалізація статичних змінних
String SerialHandler::_responseBuffer = "";
HardwareSerial *SerialHandler::_rs485Serial = nullptr;
ScreenHandler *SerialHandler::_screenHandler = nullptr;

void SerialHandler::begin(HardwareSerial *rs485Serial, ScreenHandler *screenHandler)
{
    _rs485Serial = rs485Serial;
    _screenHandler = screenHandler;
}

void SerialHandler::SendResponse(const String &response)
{
    Serial.write(response.c_str());
}

void SerialHandler::AddToResponseBuffer(const String &response)
{
    _responseBuffer += response;
}

void SerialHandler::sendUsbSerial(const String &message)
{
    Serial.write(message.c_str());
    Serial.write('\n');
}

void SerialHandler::sendRsSerial(const String &message)
{
    if (_rs485Serial != nullptr)
    {
        _rs485Serial->write(message.c_str());
        _rs485Serial->write('\n');
    }
}

void SerialHandler::processCommand(const String &command)
{
    if (command.startsWith("$AZ")) // AZIMUTH
    {
        int azimuth = StringUtils::GetValue(command, 1);
        RotatorState::azimuth = azimuth;
    }
    else if (command.startsWith("$CRR")) // CORRECTION ERROR
    {
        int error = StringUtils::GetValue(command, 1);
        RotatorState::correction = error;
    }
    else if (command == "$ENBL") // ENABLE
    {
        Vector<String> words = StringUtils::Split(command, ',');
        RotatorState::isRotationEnabled = words[1] == "true";
    }
    else if (command == "$GENBL") // GET ENABLE VALUE
    {
    }
}

void SerialHandler::processResponce(const String &command)
{
    if (command.startsWith("ER")) // Похибка
    {
        int error = StringUtils::GetValue(command, 1);
        RotatorState::correction = error;
    }
    else if (command.startsWith("AZ")) // Азимут
    {
        int azimuth = StringUtils::GetValue(command, 1);
        RotatorState::azimuth = azimuth;
    }
    else if (command.startsWith("AN")) // Кут
    {
        int angle = StringUtils::GetValue(command, 1);
        RotatorState::angle = angle;
    }
}

void SerialHandler::handle()
{
    while (_rs485Serial->available() > 0)
    {
        String message = _rs485Serial->readStringUntil('\n');
        sendUsbSerial(message);

        Vector<String> words = StringUtils::Split(message, ';');
        for (const auto &word : words)
        {
            processResponce(word);
        }
    }

    while (Serial.available() > 0)
    {
        String message = Serial.readStringUntil('\n');

        Vector<String> words = StringUtils::Split(message, ';');
        _responseBuffer = "";
        for (const auto &word : words)
        {
            processCommand(word);
        }
        if (_responseBuffer.length() > 0)
        {
            SendResponse(_responseBuffer + '\n');
        }

        sendRsSerial(message);
    }
}