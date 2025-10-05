#ifndef ANTENNA_STATE_H
#define ANTENNA_STATE_H

#include <Arduino.h>

class RotatorState
{
public:
    static bool isConnected;
    static bool hasSensor;
    static bool isRotationEnabled;
    static int azimuth;
    static int correction;
};

#endif
