#include "StringUtils.h"

Vector<String> StringUtils::Split(const String &str, char delimiter)
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

bool StringUtils::Contains(const String &str, const String &substring)
{
    return str.indexOf(substring) != -1;
}

int StringUtils::GetValue(const String &command, int index)
{
    Vector<String> words = Split(command, ',');

    if (index >= 0 && index < (int)words.size())
    {
        return words[index].toInt();
    }

    return 0;
}
