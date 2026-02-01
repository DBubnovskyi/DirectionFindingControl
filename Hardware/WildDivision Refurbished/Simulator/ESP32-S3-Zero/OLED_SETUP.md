# Налаштування OLED екрану

## Підтримувані екрани

Цей симулятор підтримує OLED екрани з контролером SSD1306:
- 128x64 пікселі (рекомендовано)
- 128x32 пікселі (потребує модифікації коду)
- I2C інтерфейс

## Визначення адреси OLED

Якщо екран не працює, можливо потрібно змінити адресу I2C.

### Метод 1: I2C Scanner

Завантажте цей скетч для визначення адреси:

```cpp
#include <Wire.h>

void setup() {
  Serial.begin(115200);
  Wire.begin(4, 5); // SDA=4, SCL=5
  Serial.println("I2C Scanner");
}

void loop() {
  byte count = 0;
  for (byte i = 1; i < 127; i++) {
    Wire.beginTransmission(i);
    if (Wire.endTransmission() == 0) {
      Serial.print("Found address: 0x");
      Serial.println(i, HEX);
      count++;
    }
  }
  if (count == 0) Serial.println("No I2C devices found");
  delay(5000);
}
```

### Метод 2: Спробувати обидві адреси

Більшість OLED використовують одну з двох адрес:
- **0x3C** (найчастіше)
- **0x3D** (рідше)

Змініть адресу в файлі `lib/OLEDDisplay/OLEDDisplay.h`:

```cpp
#define SCREEN_ADDRESS 0x3D  // Замість 0x3C
```

## Типові проблеми та рішення

### Проблема: Білий екран або мерехтіння

**Рішення:**
1. Перевірте підключення:
   - VCC → 3.3V (НЕ 5V!)
   - GND → GND
   - SDA → GPIO4 (D2)
   - SCL → GPIO5 (D1)

2. Переконайтеся що використовується 3.3V, а не 5V

### Проблема: "SSD1306 allocation failed"

**Рішення:**
1. Перевірте адресу I2C (0x3C або 0x3D)
2. Перевірте підключення
3. Переконайтеся що екран отримує живлення
4. Спробуйте зменшити швидкість I2C:

У файлі `src/main.cpp`, після `Wire.begin(I2C_SDA, I2C_SCL);` додайте:

```cpp
Wire.setClock(100000); // Зменшити до 100kHz
```

### Проблема: Текст відображається неправильно

**Рішення:**
Якщо у вас екран 128x32, змініть у `lib/OLEDDisplay/OLEDDisplay.h`:

```cpp
#define SCREEN_HEIGHT 32  // Замість 64
```

І адаптуйте координати у файлі `lib/OLEDDisplay/OLEDDisplay.cpp`.

## Підключення різних модулів

### 4-pin OLED (найчастіше)
```
OLED    ESP8266
────    ───────
GND  →  GND
VCC  →  3.3V
SCL  →  GPIO5 (D1)
SDA  →  GPIO4 (D2)
```

### 7-pin OLED (SPI - не підтримується)
Якщо у вас SPI OLED, потрібно використовувати інший драйвер.

## Перевірка підключення

Після завантаження коду, відкрийте Serial Monitor (115200 baud).
Якщо OLED не працює, ви побачите:

```
SSD1306 allocation failed
```

Якщо OLED працює, ви побачите:
```
ESP8266 Motor Simulator
========================
Initialization complete
```

## Налаштування розміру шрифту

Якщо текст занадто великий або малий, відредагуйте у 
`lib/OLEDDisplay/OLEDDisplay.cpp`:

```cpp
display.setTextSize(1); // Спробуйте 1 або 2
```

## Pull-up резистори

Більшість OLED модулів мають вбудовані pull-up резистори для I2C.
ESP8266 також має внутрішні pull-up.

Якщо є проблеми, можна спробувати:
- Додати зовнішні pull-up резистори 4.7kΩ на SDA та SCL до 3.3V
- Або вимкнути внутрішні pull-up (не рекомендується)
