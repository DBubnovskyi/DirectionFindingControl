#ifndef ANTENNA_STATE_H
#define ANTENNA_STATE_H

#include <Arduino.h>

class RotatorState
{
public:
    static bool isConnPC;
    static bool isConnRotator;
    static bool hasSensor;
    static bool isRotationEnabled;
    static int iniState;
    static int azimuth;
    static int angle;
    static int correction;
};

#endif
