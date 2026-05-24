using System;
using System.IO;
using System.Text.Json;

namespace TestApp
{
    /// <summary>
    /// Статичний клас для автоматичного завантаження та збереження налаштувань
    /// </summary>
    public static class SettingsManager
    {
        private static readonly string SettingsFileName = "Settings.json";
        private static readonly string SettingsFilePath;
        private static AppSettings? _currentSettings;
        private static readonly object _lock = new object();

        static SettingsManager()
        {
            // Зберігаємо налаштування в папці додатку
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, "DirectionFindingApp");

            // Створюємо папку якщо не існує
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }

            SettingsFilePath = Path.Combine(appFolder, SettingsFileName);
        }

        /// <summary>
        /// Поточні налаштування додатку
        /// </summary>
        public static AppSettings Current
        {
            get
            {
                if (_currentSettings == null)
                {
                    lock (_lock)
                    {
                        if (_currentSettings == null)
                        {
                            _currentSettings = Load();
                        }
                    }
                }
                return _currentSettings;
            }
        }

        /// <summary>
        /// Завантажити налаштування з файлу
        /// </summary>
        /// <returns>Налаштування або нові налаштування за замовчуванням</returns>
        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);

                    if (settings != null)
                    {
                        Console.WriteLine($"Налаштування завантажено з {SettingsFilePath}");
                        return settings;
                    }
                }

                Console.WriteLine("Файл налаштувань не знайдено. Використовуються налаштування за замовчуванням.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при завантаженні налаштувань: {ex.Message}");
            }

            // Якщо файл не існує або виникла помилка, повертаємо налаштування за замовчуванням
            var defaultSettings = new AppSettings();
            Save(defaultSettings); // Зберігаємо налаштування за замовчуванням
            return defaultSettings;
        }

        /// <summary>
        /// Примусово перезавантажити налаштування з файлу.
        /// </summary>
        public static AppSettings Reload()
        {
            lock (_lock)
            {
                _currentSettings = Load();
                return _currentSettings;
            }
        }

        /// <summary>
        /// Зберегти налаштування у файл
        /// </summary>
        /// <param name="settings">Налаштування для збереження</param>
        public static void Save(AppSettings? settings = null)
        {
            lock (_lock)
            {
                try
                {
                    var settingsToSave = settings ?? _currentSettings ?? new AppSettings();

                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true, // Форматований JSON для читабельності
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    };

                    string json = JsonSerializer.Serialize(settingsToSave, options);
                    File.WriteAllText(SettingsFilePath, json);

                    _currentSettings = settingsToSave;
                    Console.WriteLine($"Налаштування збережено в {SettingsFilePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка при збереженні налаштувань: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Оновити конкретну частину налаштувань і одразу зберегти
        /// </summary>
        /// <param name="updateAction">Дія для оновлення налаштувань</param>
        public static void Update(Action<AppSettings> updateAction)
        {
            lock (_lock)
            {
                updateAction(Current);
                Save();
            }
        }

        /// <summary>
        /// Скинути налаштування до значень за замовчуванням
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _currentSettings = new AppSettings();
                Save();
                Console.WriteLine("Налаштування скинуто до значень за замовчуванням");
            }
        }

        /// <summary>
        /// Отримати шлях до файлу налаштувань
        /// </summary>
        public static string GetSettingsPath()
        {
            return SettingsFilePath;
        }
    }
}
