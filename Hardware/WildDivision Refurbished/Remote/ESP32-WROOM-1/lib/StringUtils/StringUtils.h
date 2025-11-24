#pragma once

#include <Arduino.h>
#include <vector>

#define Vector std::vector

class StringUtils
{
public:
    static Vector<String> Split(const String &str, char delimiter);
    static bool Contains(const String &str, const String &substring);
    static int GetValue(const String &command, int index);
};
