#pragma once
#include <vector>
#ifndef PI
#define PI 3.1415926535897932384626433832795
#endif

// Симулятор MT6701 для ESP8266
// Імітує роботу датчика кута з точністю ±1°
class MT6701
{
public:
    static constexpr uint8_t DEFAULT_ADDRESS = 0x06;
    static constexpr int UPDATE_INTERVAL = 50;
    static constexpr int COUNTS_PER_REVOLUTION = 16384;
    static constexpr float COUNTS_TO_RADIANS = 2.0 * PI / COUNTS_PER_REVOLUTION;
    static constexpr float COUNTS_TO_DEGREES = 360.0 / COUNTS_PER_REVOLUTION;
    static constexpr float SECONDS_PER_MINUTE = 60.0f;
    static constexpr int RPM_THRESHOLD = 1000;
    static constexpr int RPM_FILTER_SIZE = 20;
    static constexpr float SIMULATION_ACCURACY = 1.0; // ±1° точність

    MT6701(uint8_t device_address = DEFAULT_ADDRESS,
           int update_interval = UPDATE_INTERVAL,
           int rpm_threshold = RPM_THRESHOLD,
           int rpm_filter_size = RPM_FILTER_SIZE);
    ~MT6701();
    void begin(uint8_t sda_pin, uint8_t scl_pin);
    float getAngleRadians();
    float getAngleDegrees();
    int getFullTurns();
    float getTurns();
    int getAccumulator();
    int getCount();
    float getRPM();
    void updateCount();
    bool isConnected();

    // Методи для симуляції
    void setSimulatedAngle(float angle);   // Встановити симульований кут
    void updateSimulation(int motorSpeed); // Оновити симуляцію на основі швидкості мотора

private:
    uint8_t address;
    int updateIntervalMillis;
    unsigned long lastUpdateTime;
    int count;
    int accumulator;
    float rpm;
    bool sensorConnected;
    std::vector<float> rpmFilter;
    int rpmFilterIndex;
    int rpmFilterSize;
    int rpmThreshold;

    // Симуляція
    float _simulatedAngle; // Поточний симульований кут
    float _lastReadAngle;  // Останнє показання датчика (стабільне коли немає руху)
    unsigned long _lastSimulationUpdate;

    int readCount();
    bool testConnection();
    void updateRPMFilter(float newRPM);
};