#include "SerialProcessor.h"

SerialProcessor::SerialProcessor(AngleController &controller, MT6701 &sensor, Stream &serial, ESP32LED &led)
    : _controller(controller), _sensor(sensor), _serial(serial), _led(led)
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
    _responseBuffer += response + ";";
}

void SerialProcessor::processCommand(const String &command)
{
    InitializationStage currentStage = _controller.getInitializationStage();

    // GET команди (починаються з #) доступні завжди
    if (command.startsWith("#"))
    {
        if (command == "#IN") // GET INITIALIZATION STAGE
        {
            AddToResponseBuffer("IN," + String((int)currentStage));
        }
        else if (command == "#AN") // GET ANGLE
        {
            int angle = _controller.getSensorAngle();
            AddToResponseBuffer("AN," + String(angle));
        }
        else if (command == "#AZ") // GET AZIMUTH
        {
            float angle = _controller.getSensorAngle();
            float azimuth = _controller.angleToAzimuth(angle);
            AddToResponseBuffer("AZ," + String((int)azimuth));
        }
        else if (command == "#ER") // GET SENSOR ERROR
        {
            int error = _controller.getSensorError();
            AddToResponseBuffer("ER," + String(error));
        }
        else if (command == "#EN") // GET ENABLE VALUE
        {
            String status = _controller.isMovementEnabled() ? "1" : "0";
            AddToResponseBuffer("EN," + status);
        }
        else if (command == "#ST") // GET STATUS
        {
            AddToResponseBuffer(_controller.getStatusString());
        }
        else if (command == "#ERROR") // GET ERROR STATUS (залишаємо без змін)
        {
            AddToResponseBuffer(_controller.getErrorString());
        }
        else if (command == "#AC") // GET ROTATION STATUS
        {
            String active = _controller.isActive() ? "1" : "0";
            AddToResponseBuffer("AC," + active);
        }
        else if (command == "#SP") // GET MOTOR SPEED
        {
            float speed = _controller.getCurrentSpeed();
            AddToResponseBuffer("SP," + String((int)speed));
        }
        return;
    }

    // SET команди (починаються з $) залежать від етапу ініціалізації
    if (command.startsWith("$"))
    {
        // Команди ініціалізації доступні завжди
        if (command.startsWith("$IN,"))
        {
            int stage = GetValue(command, 1);
            if (_controller.startInitialization(stage))
            {
                if (stage == 1)
                {
                    // Етап 1: рух до 0° позиції
                    // Відповідь відправиться коли рух завершиться
                }
                else if (stage == 2)
                {
                    AddToResponseBuffer("IN,1;AN,0;ER," + String((int)_controller.getSensorError()));
                }
                else if (stage == 3)
                {
                    AddToResponseBuffer("IN,2;AN,0;ER," + String((int)_controller.getSensorError()));
                }
            }
            return;
        }

        // Команди етапу 2: налаштування похибки
        if (currentStage == STAGE_2)
        {
            if (command.startsWith("$ER,"))
            {
                String value = command.substring(4); // Отримуємо частину після "$ER,"
                if (value == "L")
                {
                    _controller.adjustSensorError('L');
                }
                else if (value == "R")
                {
                    _controller.adjustSensorError('R');
                }
                else
                {
                    int error = value.toInt();
                    _controller.setSensorError(error);
                }

                // Після зміни похибки повертаємо антену до 0° та відправляємо оновлену похибку
                _controller.moveToAngle(0);
                float angle = _controller.getSensorAngle();
                float azimuth = _controller.angleToAzimuth(angle);
                AddToResponseBuffer("ER," + String((int)_controller.getSensorError()));
                AddToResponseBuffer("AZ," + String((int)azimuth));
            }
            return;
        }

        // Команди етапу 3: налаштування азимуту
        if (currentStage == STAGE_3)
        {
            if (command.startsWith("$AN_AZ,")) // Команда означає: поточний кут 0° це азимут X
            {
                int azimuth = GetValue(command, 1);
                // Встановлюємо offset так, щоб поточний кут 0° відповідав заданому азимуту
                _controller.setAzimuthOffset(azimuth);
                AddToResponseBuffer("IN,3;AN,0;ER," + String((int)_controller.getSensorError()));
                AddToResponseBuffer("AZ," + String((int)azimuth));
                // Після налаштування азимуту переходимо до завершеного стану
                _controller.setInitializationStage(STAGE_4);
            }
            return;
        }

        // Команди після завершення ініціалізації (STAGE_4)
        if (currentStage == STAGE_4)
        {
            if (command.startsWith("$AN,")) // SET ANGLE
            {
                int angle = GetValue(command, 1);
                _controller.moveToAngle(angle);
            }
            else if (command.startsWith("$AZ,")) // SET AZIMUTH
            {
                int azimuth = GetValue(command, 1);
                _controller.moveToAzimuth(azimuth);
            }
            else if (command == "$EN,1") // SET ENABLE
            {
                _controller.enableMovement();
            }
            else if (command == "$EN,0") // SET DISABLE
            {
                _controller.disableMovement();
            }
            return;
        }
    }
}

void SerialProcessor::handleSerialCommands()
{
    _led.off();
    static bool wasActive = false;

    // Перевіряємо завершення руху в етапі 1
    InitializationStage currentStage = _controller.getInitializationStage();
    bool isActive = _controller.isActive();

    if (currentStage == STAGE_1 && wasActive && !isActive)
    {
        // Рух до 0° завершено, відправляємо відповідь
        String response = "IN,1;AN,0;ER,3;\n";
        SendResponse(response);
        _controller.setInitializationStage(STAGE_2); // Переходимо до етапу 2
    }

    wasActive = isActive;

    while (_serial.available() > 0)
    {
        _led.on(); // Засвічуємо діод під час читання

        String message = _serial.readStringUntil('\n');

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

        _led.off(); // Вимикаємо діод після обробки
    }
}