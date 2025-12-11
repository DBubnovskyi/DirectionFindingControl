#include "SerialProcessor.h"

SerialProcessor::SerialProcessor(AngleController &controller, MT6701 &sensor, Stream &serial, ESP32LED &led, int dePin)
    : _controller(controller), _sensor(sensor), _serial(serial), _led(led), _dePin(dePin)
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
    // Увімкнення режиму передачі (якщо DE/RE контролюється)
    if (_dePin >= 0)
    {
        digitalWrite(_dePin, HIGH);
    }
    
    _serial.write(response.c_str());
    
    // Якщо використовуємо DE/RE, чекаємо завершення передачі
    if (_dePin >= 0)
    {
        _serial.flush(); // Чекаємо тільки якщо контролюємо DE/RE
        digitalWrite(_dePin, LOW);
    }
    // Інакше UART відправить дані в фоні
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
            float angle = _controller.getSensorAngle();
            AddToResponseBuffer("AN," + String(angle, 1));
        }
        else if (command == "#AZ") // GET AZIMUTH
        {
            float angle = _controller.getSensorAngle();
            float azimuth = _controller.angleToAzimuth(angle);
            AddToResponseBuffer("AZ," + String(azimuth, 1));
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
        else if (command == "#TOL") // GET TOLERANCE
        {
            float tolerance = _controller.getTolerance();
            AddToResponseBuffer("TOL," + String(tolerance, 1));
        }
        else if (command == "#MINS") // GET MIN SPEED
        {
            float minSpeed = _controller.getMinSpeed();
            AddToResponseBuffer("MINS," + String((int)minSpeed));
        }
        else if (command == "#MAXS") // GET MAX SPEED
        {
            float maxSpeed = _controller.getMaxSpeed();
            AddToResponseBuffer("MAXS," + String((int)maxSpeed));
        }
        else if (command == "#BRK") // GET BREACK ANGLE
        {
            float breackAngle = _controller.getBreackAngle();
            AddToResponseBuffer("BRK," + String(breackAngle, 1));
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
                value.trim(); // Видаляємо пробіли
                
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
            else if (command.startsWith("$TOL,")) // SET TOLERANCE
            {
                String value = command.substring(5);
                float tolerance = value.toFloat();
                _controller.setTolerance(tolerance);
                AddToResponseBuffer("TOL," + String(tolerance, 1));
            }
            else if (command.startsWith("$MINS,")) // SET MIN SPEED
            {
                int minSpeed = GetValue(command, 1);
                _controller.setMinSpeed(minSpeed);
                AddToResponseBuffer("MINS," + String(minSpeed));
            }
            else if (command.startsWith("$MAXS,")) // SET MAX SPEED
            {
                int maxSpeed = GetValue(command, 1);
                _controller.setMaxSpeed(maxSpeed);
                AddToResponseBuffer("MAXS," + String(maxSpeed));
            }
            else if (command.startsWith("$BRK,")) // SET BREACK ANGLE
            {
                String value = command.substring(5);
                float breackAngle = value.toFloat();
                _controller.setBreackAngle(breackAngle);
                AddToResponseBuffer("BRK," + String(breackAngle, 1));
            }
            return;
        }
    }
}

void SerialProcessor::handleSerialCommands()
{
    static bool wasRotating = false;
    static unsigned long lastStatusTime = 0;
    static unsigned long lastCommandTime = 0;
    
    bool isRotating = _controller.isRotating();
    unsigned long currentTime = millis();

    // Відправка статусу обертання (тільки якщо не було команд останні 500мс)
    bool canSendStatus = (currentTime - lastCommandTime) > 500;
    
    if (isRotating && canSendStatus && (currentTime - lastStatusTime >= 200))
    {
        sendRotationStatus();
        lastStatusTime = currentTime;
    }
    else if (wasRotating && !isRotating && canSendStatus)
    {
        sendRotationStatus();
    }

    // Перевірка завершення ініціалізації етапу 1
    if (_controller.getInitializationStage() == STAGE_1 && wasRotating && !isRotating)
    {
        String response = "IN,1;AN,0;ER,3;\n";
        SendResponse(response);
        _controller.setInitializationStage(STAGE_2);
    }

    wasRotating = isRotating;

    // Обробка вхідних команд (неблокуюче читання)
    _led.off();
    
    int bytesProcessed = 0;
    bool commandReceived = false;
    
    while (_serial.available() > 0 && bytesProcessed < 128)
    {
        char c = (char)_serial.read();
        bytesProcessed++;
        
        // Ігнорувати непечатні символи (крім \n, \r, ;)
        if ((c >= 32 && c <= 126) || c == '\n' || c == '\r' || c == ';')
        {
            // Завершення повідомлення
            if (c == '\n')
            {
                // Обробляємо останню команду в буфері
                if (_inputBuffer.length() > 0)
                {
                    if (isValidCommand(_inputBuffer))
                    {
                        processCommand(_inputBuffer);
                    }
                    _inputBuffer = "";
                }
                
                // Відправляємо всю накопичену відповідь
                if (_responseBuffer.length() > 0)
                {
                    _led.on();
                    SendResponse(_responseBuffer + '\n');
                    _responseBuffer = "";
                    _led.off();
                }
                
                commandReceived = true;
            }
            // Роздільник команд
            else if (c == ';')
            {
                if (_inputBuffer.length() > 0)
                {
                    if (isValidCommand(_inputBuffer))
                    {
                        processCommand(_inputBuffer);
                    }
                    _inputBuffer = "";
                }
            }
            else if (c != '\r') // Ігнорувати \r
            {
                _inputBuffer += c;
            }
        }
        
        // Захист від переповнення буфера
        if (_inputBuffer.length() > 256)
        {
            _inputBuffer = "";
        }
    }
    
    // Оновлюємо час останньої команди
    if (commandReceived)
    {
        lastCommandTime = currentTime;
    }
}

bool SerialProcessor::isValidCommand(const String &cmd)
{
    // Перевірка що команда починається з # або $
    if (cmd.length() < 2) return false;
    if (cmd[0] != '#' && cmd[0] != '$') return false;
    
    // Перевірка що команда містить тільки допустимі символи
    for (unsigned int i = 0; i < cmd.length(); i++)
    {
        char c = cmd[i];
        if (!((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || 
              c == '_' || c == ',' || c == '-' || c == '#' || c == '$' || c == '.'))
        {
            return false;
        }
    }
    
    return true;
}

void SerialProcessor::sendRotationStatus()
{
    int speed = (int)_controller.getCurrentSpeed();
    float angle = _controller.getSensorAngle();
    float azimuth = _controller.angleToAzimuth(angle);
    int rotatingStatus = _controller.isRotating() ? 1 : 0;
    
    String status = "SP," + String(speed) + ";AZ," + String(azimuth, 1) + ";AN," + String(angle, 1) + ";RT," + String(rotatingStatus) + ";\n";
    SendResponse(status);
}