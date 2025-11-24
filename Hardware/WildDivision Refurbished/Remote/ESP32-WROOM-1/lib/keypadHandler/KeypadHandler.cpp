#include "KeypadHandler.h"
#include "SerialHandler.h"

KeypadHandler::KeypadHandler()
    : keypad(makeKeymap(keys), rowPins, colPins, ROWS, COLS), inputBuffer("0")
{
}

void KeypadHandler::begin(ScreenHandler *screen)
{
    screenHandler = screen;

    for (int i = 0; i < ROWS; i++)
    {
        pinMode(rowPins[i], INPUT_PULLUP);
    }

    keypad.setDebounceTime(100);
    keypad.setHoldTime(500);
}

void KeypadHandler::handle()
{
    char key = keypad.getKey();

    if (!key)
    {
        return;
    }

    String currentScreen = screenHandler->getCurrentScreenKey();

    if (currentScreen == "init0")
    {
        if (key == '#')
        {
            SerialHandler::sendRsSerial("$IN,1;#ER,#AZ;#AN;");
            String init1Data[] = {String(RotatorState::correction), "", "", "", ""};
            screenHandler->showScreen("init1", init1Data);
        }
    }
    else if (currentScreen == "init1")
    {
        if (key == '4' || key == '6')
        {
            String correctionValue = (key == '4') ? "L" : "R";
            String message = "$ER," + correctionValue + ";";
            SerialHandler::sendRsSerial(message);
            String init1Data[] = {String(RotatorState::correction), "", "", "", ""};
            screenHandler->showScreen("init1", init1Data);
        }
        else if (key == '#')
        {
            SerialHandler::sendRsSerial("$IN,2;");
            screenHandler->showScreen("init2");
        }
    }
    else if (currentScreen == "init2")
    {
        if (key >= '0' && key <= '9')
        {
            if (inputBuffer == "0")
            {
                inputBuffer = String(key);
            }
            else if (inputBuffer.length() < 3)
            {
                inputBuffer += key;
            }
            String init2Data[] = {inputBuffer, "", "", "", ""};
            screenHandler->showScreen("init2", init2Data);
        }
        else if (key == '#')
        {
            int value = inputBuffer.toInt();
            if (value >= 0 && value <= 359)
            {
                String init3Data[] = {String(RotatorState::angle), inputBuffer, String(RotatorState::correction), "", ""};
                screenHandler->showScreen("init3", init3Data);
                String message = "$AN_AZ," + inputBuffer + ";$IN,3;";
                SerialHandler::sendRsSerial(message);
                inputBuffer = "0";
            }
            else
            {
                inputBuffer = "0";
                String init2Data[] = {inputBuffer, "", "", "", ""};
                screenHandler->showScreen("init2", init2Data);
            }
        }
        else if (key == '*')
        {
            if (inputBuffer.length() > 1)
            {
                inputBuffer.remove(inputBuffer.length() - 1);
            }
            else
            {
                inputBuffer = "0";
            }
            String init2Data[] = {inputBuffer, "", "", "", ""};
            screenHandler->showScreen("init2", init2Data);
        }
    }
    else if (currentScreen == "init3")
    {
        if (key == '#')
        {
            SerialHandler::sendRsSerial("$IN,4;");
            screenHandler->showScreen("main");
        }
    }
    else if (currentScreen == "main")
    {
        switch (key)
        {
        case '1':
        {
            inputBuffer = "0";
            String azimuthData[] = {String(RotatorState::azimuth), "0", "", "", ""};
            screenHandler->showScreen("azimuth", azimuthData);
            break;
        }
        case '2':
        {
            inputBuffer = "0";
            String angleData[] = {String(RotatorState::angle), "0", "", "", ""};
            screenHandler->showScreen("angle", angleData);
            break;
        }
        case '3':
        {
            screenHandler->showScreen("settings");
            break;
        }
        case '4':
        {
            String infoData[] = {String(RotatorState::angle), String(RotatorState::azimuth), String(RotatorState::correction), "", ""};
            screenHandler->showScreen("info", infoData);
            break;
        }
        }
    }
    else if (currentScreen == "azimuth")
    {
        if (key >= '0' && key <= '9')
        {
            if (inputBuffer == "0")
            {
                inputBuffer = String(key);
            }
            else if (inputBuffer.length() < 3)
            {
                inputBuffer += key;
            }
            String azimuthData[] = {String(RotatorState::azimuth), inputBuffer, "", "", ""};
            screenHandler->showScreen("azimuth", azimuthData);
        }
        else if (key == '#')
        {
            int value = inputBuffer.toInt();
            if (value >= 0 && value <= 359)
            {
                String azimuthData[] = {String(RotatorState::angle), inputBuffer, "", "", ""};
                screenHandler->showScreen("azimuth", azimuthData);
                String message = "$AZ," + inputBuffer + ";";
                SerialHandler::sendRsSerial(message);
                inputBuffer = "0";
            }
            else
            {
                inputBuffer = "0";
                String azimuthData[] = {String(RotatorState::azimuth), "0", "", "", ""};
                screenHandler->showScreen("azimuth", azimuthData);
            }
        }
        else if (key == '*')
        {
            screenHandler->showScreen("main");
        }
    }
    else if (currentScreen == "angle")
    {
        if (key >= '0' && key <= '9')
        {
            if (inputBuffer == "0")
            {
                inputBuffer = String(key);
            }
            else if (inputBuffer.length() < 3)
            {
                inputBuffer += key;
            }
            String angleData[] = {String(RotatorState::angle), inputBuffer, "", "", ""};
            screenHandler->showScreen("angle", angleData);
        }
        else if (key == '#')
        {
            int value = inputBuffer.toInt();
            if (value >= 0 && value <= 359)
            {
                String angleData[] = {String(RotatorState::angle), inputBuffer, "", "", ""};
                screenHandler->showScreen("angle", angleData);
                String message = "$AN," + inputBuffer + ";";
                SerialHandler::sendRsSerial(message);
                inputBuffer = "0";
            }
            else
            {
                inputBuffer = "0";
                String angleData[] = {String(RotatorState::angle), "0", "", "", ""};
                screenHandler->showScreen("angle", angleData);
            }
        }
        else if (key == '*')
        {
            screenHandler->showScreen("main");
        }
    }
    else if (currentScreen == "settings")
    {
        switch (key)
        {
        case '*':
            screenHandler->showScreen("main");
            break;
        }
    }
    else if (currentScreen == "info")
    {
        switch (key)
        {
        case '*':
            screenHandler->showScreen("main");
            break;
        }
    }
}