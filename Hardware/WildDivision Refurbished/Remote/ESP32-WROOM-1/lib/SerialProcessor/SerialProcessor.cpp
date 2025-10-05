#include "SerialProcessor.h"

// Ініціалізація статичних змінних
String SerialProcessor::_responseBuffer = "";
HardwareSerial *SerialProcessor::_rs485Serial = nullptr;

void SerialProcessor::begin(HardwareSerial *rs485Serial)
{
    _rs485Serial = rs485Serial;
    // Тут можна додати додаткові налаштування послідовного порту
}

Vector<String> SerialProcessor::Split(const String &str, char delimiter)
{
    Vector<String> tokens;
    String token = "";

    for (unsigned int i = 0; i < str.length(); i++)
    {
        if (str[i] == delimiter)
        {
            if (token.length() > 0)
            {
                tokens.push_back(token);
            }
            token = "";
        }
        else
        {
            token += str[i];
        }
    }

    if (token.length() > 0)
    {
        tokens.push_back(token);
    }

    return tokens;
}

bool SerialProcessor::Contains(const String &str, const String &substring)
{
    return str.indexOf(substring) != -1;
}

int SerialProcessor::GetValue(const String &command, int index)
{
    Vector<String> words = Split(command, ',');

    // Перевірка чи існує елемент за вказаним індексом
    if (index >= 0 && index < (int)words.size())
    {
        return words[index].toInt();
    }

    // Якщо індекс недійсний, повертаємо 0 як значення за замовчуванням
    return 0;
}

void SerialProcessor::SendResponse(const String &response)
{
    Serial.write(response.c_str());
}

void SerialProcessor::AddToResponseBuffer(const String &response)
{
    _responseBuffer += response;
}

void SerialProcessor::sendUsbSerial(const String &message)
{
    Serial.write(message.c_str());
    Serial.write('\n');
}

void SerialProcessor::sendRsSerial(const String &message)
{
    if (_rs485Serial != nullptr)
    {
        _rs485Serial->write(message.c_str());
        _rs485Serial->write('\n');
    }
}

void SerialProcessor::processCommand(const String &command)
{
    if (command.startsWith("$AZ")) // AZIMUTH
    {
        int azimuth = GetValue(command, 1);
        RotatorState::azimuth = azimuth;
    }
    else if (command.startsWith("$CRR")) // CORRECTION ERROR
    {
        int error = GetValue(command, 1);
        RotatorState::correction = error;
    }
    else if (command == "$ENBL") // ENABLE
    {
        Vector<String> words = Split(command, ',');
        RotatorState::isRotationEnabled = words[1] == "true";
    }
    else if (command == "$GENBL") // GET ENABLE VALUE
    {
    }
}

void SerialProcessor::handleSerialCommands()
{
    if (_rs485Serial == nullptr)
        return; // Перевіряємо чи ініціалізована RS485

    while (_rs485Serial->available() > 0)
    {
        String message = _rs485Serial->readStringUntil('\n');
        sendUsbSerial(message);
    }

    while (Serial.available() > 0)
    {
        String message = Serial.readStringUntil('\n');

        Vector<String> words = Split(message, ';');
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