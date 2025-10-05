#include "SerialProcessor.h"

SerialProcessor::SerialProcessor(AngleController &controller, MT6701 &sensor, Stream &serial)
    : _controller(controller), _sensor(sensor), _serial(serial)
{
}

void SerialProcessor::begin()
{
    // Ініціалізація послідовного порту буде виконуватися ззовні
    // Цей метод можна використовувати для додаткових налаштувань
}

Vector<String>
SerialProcessor::Split(const String &str, char delimiter)
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
    _serial.write(response.c_str());
}

void SerialProcessor::AddToResponseBuffer(const String &response)
{
    _responseBuffer += response;
}

void SerialProcessor::processCommand(const String &command)
{
    if (command.startsWith("$SAZ")) // SET AZIMUTH
    {
        int azimuth = GetValue(command, 1);
        _controller.moveToAngle(azimuth);
    }
    else if (command.startsWith("$GAZ")) // GET AZIMUTH
    {
        int azimuth = _controller.getSensorAngle();
        AddToResponseBuffer("$AZ," + String(azimuth) + ";");
    }
    else if (command.startsWith("$SCRR")) // SET CORRECTION ERROR
    {
        int error = GetValue(command, 1);
        _controller.setSensorError(error);
    }
    else if (command.startsWith("$GCRR")) // GET CORRECTION
    {
        int error = _controller.getSensorError();
        AddToResponseBuffer("$CRR," + String(error) + ";");
    }
    else if (command == "$SENBL") // SET ENABLE
    {
        _controller.enableMovement();
    }
    else if (command == "$SDSBL") // SET DISABLE
    {
        _controller.disableMovement();
    }
    else if (command == "$GENBL") // GET ENABLE VALUE
    {
        String status = _controller.isMovementEnabled() ? "true" : "false";
        AddToResponseBuffer("$ENBL," + status + ";");
    }
}

void SerialProcessor::handleSerialCommands()
{
    while (_serial.available() > 0)
    {
        String message = _serial.readStringUntil('\n');
        Vector<String> words = Split(message, ';');

        // Очищаємо буфер відповідей перед обробкою нових команд
        _responseBuffer = "";

        // Обробляємо всі команди, збираючи відповіді в буфер
        for (const auto &word : words)
        {
            processCommand(word);
        }

        // Відправляємо всі накопичені відповіді одним блоком
        if (_responseBuffer.length() > 0)
        {
            SendResponse(_responseBuffer + '\n');
        }
    }
}