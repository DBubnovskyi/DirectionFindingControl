# SerialProcessor - Приклади використання

Клас `SerialProcessor` тепер підтримує будь-який тип серійного порту через базовий клас `Stream`. Це дозволяє використовувати:

- **USB Serial** (`Serial`)
- **Hardware Serial** (`HardwareSerial`)
- **Software Serial** (якщо потрібно)
- **Будь-який інший клас, що наслідує `Stream`**

## Основні переваги нової реалізації:

1. **Гнучкість** - можна використовувати будь-який серійний порт
2. **Простота** - один клас для всіх типів портів
3. **Універсальність** - легке перемикання між типами портів

## Приклади використання:

### 1. USB Serial (main_usb_serial.cpp)
```cpp
// Простий приклад з USB Serial
SerialProcessor *serialProc = new SerialProcessor(*controller, angleSensor, Serial);
```

### 2. RS485 Serial (main_rs485_serial.cpp)
```cpp
// Приклад з RS485
HardwareSerial RS485Serial(1);
RS485Serial.begin(115200, SERIAL_8N1, 7, 8); // RX=7, TX=8
SerialProcessor *serialProc = new SerialProcessor(*controller, angleSensor, RS485Serial);
```

### 3. Перемикання між портами (main_switchable_serial.cpp)
```cpp
// Використання макросів для вибору порту
#define USE_USB_SERIAL        // або #define USE_RS485_SERIAL

#ifdef USE_RS485_SERIAL
    serialProc = new SerialProcessor(*controller, angleSensor, RS485Serial);
#else
    serialProc = new SerialProcessor(*controller, angleSensor, Serial);
#endif
```

## Як використовувати:

1. Скопіюйте один з файлів прикладів до `src/main.cpp`
2. Змініть налаштування за потребою (піни, швидкість передачі)
3. Скомпілюйте та завантажте код

## Налаштування RS485:
- **RX Pin**: 7
- **TX Pin**: 8
- **Швидкість**: 115200 baud
- **Формат**: 8N1 (8 біт даних, без парності, 1 стоп біт)

## Налаштування USB Serial:
- **Швидкість**: 115200 baud
- Використовується стандартний `Serial` об'єкт ESP32
