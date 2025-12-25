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
        // return;
    }

    String currentScreen = screenHandler->getCurrentScreenKey();

    if (currentScreen == "init0")
    {
        if (key == '#')
        {
            SerialHandler::sendRsSerial("$IN,1;#ER,#AZ;#AN;#IN;");
            screenHandler->showScreen("init1-2");
        }
    }
    else if (currentScreen == "init1-2")
    {
        if (key == '*')
        {
            SerialHandler::sendRsSerial("$IN,0;#ER,#AZ;#AN;#IN");
            screenHandler->showScreen("init0");
        }
    }
    else if (currentScreen == "init2")
    {
        if (key == '4' || key == '6')
        {
            String correctionValue = (key == '4') ? "L" : "R";
            String message = "$ER," + correctionValue + ";#ER;";
            SerialHandler::sendRsSerial(message);
        }
        else if (key == '#')
        {
            SerialHandler::sendRsSerial("$IN,3;#ER,#AZ;#AN;#IN");
            screenHandler->showScreen("init3");
        }
        else if (key == '*')
        {
            SerialHandler::sendRsSerial("$IN,0;#ER,#AZ;#AN;#IN");
            screenHandler->showScreen("init0");
        }
        else
        {
            String init2Data[] = {String(RotatorState::correction), "", "", "", ""};
            screenHandler->showScreen("init2", init2Data);
        }
    }
    else if (currentScreen == "init3")
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
            String init3Data[] = {inputBuffer, "", "", "", ""};
            screenHandler->showScreen("init3", init3Data);
        }
        else if (key == '#')
        {
            int value = inputBuffer.toInt();
            if (value >= 0 && value <= 359)
            {
                String init3Data[] = {String(RotatorState::angle), inputBuffer, String(RotatorState::correction), "", ""};
                screenHandler->showScreen("init4", init3Data);
                String message = "$AN_AZ," + inputBuffer + ";$IN,3;#IN;";
                SerialHandler::sendRsSerial(message);
                inputBuffer = "0";
            }
            else
            {
                inputBuffer = "0";
                String init3Data[] = {inputBuffer, "", "", "", ""};
                screenHandler->showScreen("init3", init3Data);
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
            String init3Data[] = {inputBuffer, "", "", "", ""};
            screenHandler->showScreen("init3", init3Data);
        }
    }
    else if (currentScreen == "init4")
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
                String message = "$AZ," + inputBuffer + ";#AZ;";
                SerialHandler::sendRsSerial(message);
                inputBuffer = "0";
                String azimuthData[] = {String(RotatorState::azimuth), inputBuffer, "", "", ""};
                screenHandler->showScreen("azimuth", azimuthData);
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
        else
        {
            String azimuthData[] = {String(RotatorState::azimuth), inputBuffer, "", "", ""};
            screenHandler->showScreen("azimuth", azimuthData);
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
                String message = "$AN," + inputBuffer + ";#AN;";
                SerialHandler::sendRsSerial(message);
                inputBuffer = "0";
                String angleData[] = {String(RotatorState::angle), inputBuffer, "", "", ""};
                screenHandler->showScreen("angle", angleData);
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
        else
        {
            String angleData[] = {String(RotatorState::angle), inputBuffer, "", "", ""};
            screenHandler->showScreen("angle", angleData);
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