using System;

namespace TestApp
{
    /// <summary>
    /// Клас для збереження налаштувань додатку
    /// </summary>
    public class AppSettings
    {
        // Налаштування карти
        public MapSettings Map { get; set; } = new MapSettings();
        
        // Налаштування станції
        public StationSettings Station { get; set; } = new StationSettings();
        
        // Налаштування COM-порту
        public SerialSettings Serial { get; set; } = new SerialSettings();
        
        // Налаштування сканування
        public ScanSettings Scan { get; set; } = new ScanSettings();
    }

    public class MapSettings
    {
        /// <summary>
        /// Широта центру карти
        /// </summary>
        public double Latitude { get; set; } = 50.4501;
        
        /// <summary>
        /// Довгота центру карти
        /// </summary>
        public double Longitude { get; set; } = 30.5234;
        
        /// <summary>
        /// Рівень зуму
        /// </summary>
        public double Zoom { get; set; } = 10;
        
        /// <summary>
        /// Провайдер карти (OpenStreetMap, GoogleMap, і т.д.)
        /// </summary>
        public string MapProvider { get; set; } = "OpenStreetMap";
    }

    public class StationSettings
    {
        /// <summary>
        /// Широта станції
        /// </summary>
        public double Latitude { get; set; } = 50.4501;
        
        /// <summary>
        /// Довгота станції
        /// </summary>
        public double Longitude { get; set; } = 30.5234;
        
        /// <summary>
        /// Чи встановлені координати станції
        /// </summary>
        public bool IsSet { get; set; } = false;
    }

    public class SerialSettings
    {
        /// <summary>
        /// Останній використаний COM-порт
        /// </summary>
        public string? LastPort { get; set; }
        
        /// <summary>
        /// Використовувати симулятор
        /// </summary>
        public bool UseSimulator { get; set; } = false;
    }

    public class ScanSettings
    {
        /// <summary>
        /// Початковий азимут для сканування
        /// </summary>
        public int StartAzimuth { get; set; } = 0;
        
        /// <summary>
        /// Кінцевий азимут для сканування
        /// </summary>
        public int EndAzimuth { get; set; } = 360;
        
        /// <summary>
        /// Крок сканування
        /// </summary>
        public int Step { get; set; } = 10;
        
        /// <summary>
        /// Режим маятника (прямо-назад)
        /// </summary>
        public bool PendulumMode { get; set; } = true;
        
        /// <summary>
        /// Початок забороненої зони (азимут)
        /// </summary>
        public int ForbiddenZoneStart { get; set; } = 175;
        
        /// <summary>
        /// Кінець забороненої зони (азимут)
        /// </summary>
        public int ForbiddenZoneEnd { get; set; } = 185;
    }
}
