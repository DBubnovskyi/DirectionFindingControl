# Direction Finding Control

Electron-додаток для управління системою пеленгації.

## Структура проекту

```
├── main.js          # Головний процес Electron
├── preload.js       # Preload скрипт для безпечної комунікації
├── index.html       # Головна HTML сторінка
├── style.css        # Стилі додатку
├── renderer.js      # Renderer процес
└── package.json     # Конфігурація проекту
```

## Запуск

### Встановлення залежностей
```bash
npm install
```

### Запуск додатку
```bash
npm start
```

### Запуск у режимі розробки з інспектором
```bash
npm run dev
```

## Технології

- **Electron**: Фреймворк для створення десктопних додатків
- **Node.js**: Середовище виконання JavaScript
- **HTML/CSS/JavaScript**: Веб-технології для інтерфейсу

## Безпека

Додаток використовує:
- `contextIsolation: true` - ізоляція контексту
- `nodeIntegration: false` - вимкнена інтеграція Node.js в renderer
- Content Security Policy для захисту від XSS атак
