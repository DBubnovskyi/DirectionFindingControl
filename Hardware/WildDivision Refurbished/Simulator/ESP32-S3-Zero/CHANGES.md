# Зміни в коді для симуляції

## Огляд змін

Цей документ описує основні зміни, зроблені для адаптації коду ESP32-S3 
для роботи в режимі симулятора на ESP8266 12F.

## 1. PlatformIO Configuration (platformio.ini)

### Було (ESP32-S3):
```ini
[env:esp32-s3-devkitc-1]
platform = espressif32
board = esp32-s3-devkitc-1
lib_deps = 
    adafruit/Adafruit NeoPixel@^1.15.1
```

### Стало (ESP8266):
```ini
[env:esp12e]
platform = espressif8266
board = esp12e
lib_deps = 
    adafruit/Adafruit GFX Library@^1.11.9
    adafruit/Adafruit SSD1306@^2.5.9
```

## 2. Драйвер мотора (DRV8871)

### Зміни:
- Видалено залежність від `ledcSetup()` (ESP32-specific)
- Додано метод `getSpeed()` для отримання поточної швидкості
- Симуляція: швидкість просто зберігається, піни не контролюються

### Нова функціональність:
```cpp
int DRV8871::getSpeed() {
    return _currentSpeed;
}
```

## 3. Датчик кута (MT6701)

### Основні зміни:
- Видалено залежність від FreeRTOS (ESP32-specific)
- Видалено `std::atomic` (не потрібно на однопоточному ESP8266)
- Додано методи симуляції:
  - `setSimulatedAngle(float angle)` - встановити симульований кут
  - `updateSimulation(int motorSpeed)` - оновити симуляцію на основі швидкості мотора

### Алгоритм симуляції:
```cpp
void MT6701::updateSimulation(int motorSpeed) {
    // motorSpeed: -255 до 255
    // Максимальна швидкість: 5°/с при швидкості 255
    
    float maxDegreesPerSecond = 5.0;
    float degreesPerSecond = (motorSpeed / 255.0) * maxDegreesPerSecond;
    float deltaAngle = degreesPerSecond * (elapsed / 1000.0);
    
    _simulatedAngle += deltaAngle;
    // + нормалізація та додавання випадкової помилки ±1°
}
```

### Імітація помилки:
```cpp
float MT6701::getAngleDegrees() {
    float error = (random(-100, 100) / 100.0); // ±1°
    return _simulatedAngle + error;
}
```

## 4. LED контролер (ESP32LED)

### Зміни:
- Видалено залежність від Adafruit NeoPixel
- Використовується вбудований LED на GPIO2
- ESP8266 LED має інверсну логіку (активний LOW)

### Було (ESP32):
```cpp
void ESP32LED::on() {
    digitalWrite(LED_PIN, HIGH);
}
```

### Стало (ESP8266):
```cpp
void ESP32LED::on() {
    digitalWrite(LED_PIN, LOW); // Інверсна логіка
}
```

## 5. Новий компонент: OLED Display

### Додано новий клас OLEDDisplay:
- Відображення поточного кута
- Відображення цільового кута
- Відображення статусу обертання
- Відображення швидкості мотора
- Візуальні індикатори

### Основні методи:
```cpp
void update(float currentAngle, float targetAngle, 
            bool isRotating, int motorSpeed);
void displayError(String error);
void displayInitialization(int stage);
```

## 6. Головний файл (main.cpp)

### Зміни в налаштуванні:
- Видалено HardwareSerial для RS485
- Додано I2C для OLED (GPIO4, GPIO5)
- Додано ініціалізацію OLED дисплея

### Додано в loop():
```cpp
// Оновлення симуляції датчика
angleSensor.updateSimulation(motorSpeed);

// Оновлення OLED дисплея
oledDisplay->update(currentAngle, targetAngle, 
                    isRotating, motorSpeed);
```

## 7. AngleController

### Додано методи:
```cpp
float getTargetAngle(); // Для відображення на OLED
```

### Без змін:
- Вся логіка керування залишилася без змін
- Всі команди працюють ідентично
- EEPROM використовується так само

## 8. SerialProcessor

### Без змін:
- Всі команди підтримуються повністю
- Протокол залишився незмінним
- Сумісність з оригінальним кодом

## Сумісність команд

Всі команди з оригінального коду працюють у симуляторі:

| Команда | Статус | Примітка |
|---------|--------|----------|
| #AN | ✅ | Працює, додає ±1° помилку |
| #AZ | ✅ | Працює |
| #ST | ✅ | Працює |
| AN,X | ✅ | Працює, швидкість 5°/с |
| EN,X | ✅ | Працює |
| TOL,X | ✅ | Працює |
| Всі інші | ✅ | Всі команди підтримуються |

## Відмінності в роботі

### Швидкість обертання:
- **Реальний пристрій**: Залежить від мотора та драйвера
- **Симулятор**: Фіксована 5°/с максимум

### Точність:
- **Реальний пристрій**: Залежить від датчика MT6701
- **Симулятор**: ±1° з випадковою помилкою

### Час відгуку:
- **Реальний пристрій**: Миттєвий
- **Симулятор**: Реалістична затримка через симуляцію швидкості

## Тестування

Симулятор пройшов тестування на:
- ✅ Обертання на різні кути
- ✅ Зміна напрямку
- ✅ Зупинка та відновлення руху
- ✅ Збереження параметрів в EEPROM
- ✅ Всі серійні команди
- ✅ Відображення на OLED

## Наступні кроки для реального пристрою

Щоб повернутися до реального пристрою:
1. Замінити platformio.ini на ESP32
2. Прибрати `updateSimulation()` виклики
3. Відновити реальні драйвери MT6701 та DRV8871
4. Видалити або адаптувати OLED код (якщо потрібно)
