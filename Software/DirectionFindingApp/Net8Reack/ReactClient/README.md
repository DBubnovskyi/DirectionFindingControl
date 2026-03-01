# Direction Finding Control

Веб-застосунок для управління системою пеленгації на React.

## Архітектура даних

- Клієнт не працює з серійними портами.
- Усі телеметричні дані, налаштування і логи завантажуються з серверного API.
- Центральний стан UI та даних зберігається в `AppContext`.
- Компоненти (`Connector`, `Settings`, `CompassControl`, `Logging`) відображають стан із `AppContext`.

## Запуск

### Встановлення залежностей
```bash
npm install
```

### Режим розробки
```bash
npm start
```

### Production-збірка
```bash
npm run build
```

## Технології

- React
- Webpack
- REST API (серверний бекенд)

## API (очікувані endpoint-и)

- `GET /api/state` — отримати поточний стан (angles, settings, logs, connection).
- `POST /api/settings` — зберегти налаштування ротатора.
- `POST /api/logs/clear` — очистити логи (опційно).
