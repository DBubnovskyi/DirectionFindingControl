// Конфігурація симулятора ESP8266
// Редагуйте ці значення для налаштування симулятора

#ifndef SIMULATOR_CONFIG_H
#define SIMULATOR_CONFIG_H

// ===== Налаштування I2C =====
#define I2C_SDA 4 // GPIO4 (D2)
#define I2C_SCL 5 // GPIO5 (D1)

// ===== Налаштування OLED =====
#define SCREEN_WIDTH 128
#define SCREEN_HEIGHT 64
#define OLED_ADDRESS 0x3C     // Змініть на 0x3D якщо потрібно
#define DISPLAY_UPDATE_MS 100 // Частота оновлення дисплея (мс)

// ===== Параметри симуляції мотора =====
#define MAX_MOTOR_SPEED_DEG_PER_SEC 5.0 // Максимальна швидкість обертання
#define MOTOR_SIMULATION_PRECISION 1.0  // Точність позиціонування (±градуси)

// ===== Налаштування Serial =====
#define SERIAL_BAUD_RATE 115200

// ===== Налаштування LED =====
#define LED_PIN 2 // Вбудований LED на GPIO2

// ===== Налаштування EEPROM =====
#define EEPROM_SIZE 512

// ===== Режим відладки =====
// Розкоментуйте для додаткових повідомлень в Serial
// #define DEBUG_MODE 1

#endif // SIMULATOR_CONFIG_H
