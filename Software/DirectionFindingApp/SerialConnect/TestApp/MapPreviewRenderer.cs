using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.WindowsForms;

namespace TestApp
{
    /// <summary>
    /// Клас для малювання preview лінії на карті та відображення підказок
    /// </summary>
    public class MapPreviewRenderer
    {
        private readonly GMapControl _mapControl;
        private readonly ToolTip _tooltip;
        private readonly Pen _previewLinePen;
        private PointLatLng? _previewLineStart;
        private PointLatLng? _previewLineEnd;

        public MapPreviewRenderer(GMapControl mapControl, ToolTip tooltip)
        {
            _mapControl = mapControl ?? throw new ArgumentNullException(nameof(mapControl));
            _tooltip = tooltip ?? throw new ArgumentNullException(nameof(tooltip));
            
            _previewLinePen = new Pen(Color.Blue, 1)
            {
                DashStyle = DashStyle.Dash
            };

            // Підписуємось на події карти
            _mapControl.Paint += OnMapPaint;
        }

        /// <summary>
        /// Обробник руху миші на карті - показує preview лінію та підказку
        /// </summary>
        public void HandleMouseMove(GMapMarker? stationMarker, MouseEventArgs e, int anAzValue)
        {
            if (stationMarker == null) return;

            try
            {
                // Отримуємо координати миші
                PointLatLng mousePosition = _mapControl.FromLocalToLatLng(e.X, e.Y);

                // Обчислюємо азимут та відстань
                double azimuth = CalculateAzimuth(stationMarker.Position, mousePosition);
                double distance = CalculateDistance(stationMarker.Position, mousePosition);

                // Оновлюємо preview лінію
                _previewLineStart = stationMarker.Position;
                _previewLineEnd = mousePosition;
                _mapControl.Invalidate();

                // Обчислюємо кут AN (антена)
                int anAngle = ((int)Math.Round(azimuth) - anAzValue + 180 + 360) % 360;

                // Форматуємо підказку
                string tooltipText = $"аз     {(int)Math.Round(azimuth)}°\nкут   {anAngle}°\nд       {distance:0.0} км";
                _tooltip.SetToolTip(_mapControl, tooltipText);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MapPreviewRenderer.HandleMouseMove: {ex.Message}");
            }
        }

        /// <summary>
        /// Обробник виходу миші з карти - очищає preview лінію та підказку
        /// </summary>
        public void HandleMouseLeave()
        {
            try
            {
                _previewLineStart = null;
                _previewLineEnd = null;
                _mapControl.Invalidate();
                _tooltip.SetToolTip(_mapControl, "");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MapPreviewRenderer.HandleMouseLeave: {ex.Message}");
            }
        }

        /// <summary>
        /// Обчислює азимут від точки from до точки to
        /// </summary>
        public static double CalculateAzimuth(PointLatLng from, PointLatLng to)
        {
            double lat1 = from.Lat * Math.PI / 180.0;
            double lon1 = from.Lng * Math.PI / 180.0;
            double lat2 = to.Lat * Math.PI / 180.0;
            double lon2 = to.Lng * Math.PI / 180.0;

            double dLon = lon2 - lon1;
            double y = Math.Sin(dLon) * Math.Cos(lat2);
            double x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);

            double azimuthRad = Math.Atan2(y, x);
            double azimuthDeg = azimuthRad * 180.0 / Math.PI;

            // Нормалізуємо до діапазону 0-360
            return (azimuthDeg + 360.0) % 360.0;
        }

        /// <summary>
        /// Обчислює відстань між двома точками в км (формула Haversine)
        /// </summary>
        public static double CalculateDistance(PointLatLng from, PointLatLng to)
        {
            const double earthRadius = 6371.0;

            double lat1 = from.Lat * Math.PI / 180.0;
            double lon1 = from.Lng * Math.PI / 180.0;
            double lat2 = to.Lat * Math.PI / 180.0;
            double lon2 = to.Lng * Math.PI / 180.0;

            double dLat = lat2 - lat1;
            double dLon = lon2 - lon1;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadius * c;
        }

        /// <summary>
        /// Малює preview лінію на карті
        /// </summary>
        private void OnMapPaint(object? sender, PaintEventArgs e)
        {
            try
            {
                if (_previewLineStart != null && _previewLineEnd != null)
                {
                    // Конвертуємо географічні координати в екранні
                    GPoint p1 = _mapControl.FromLatLngToLocal(_previewLineStart.Value);
                    GPoint p2 = _mapControl.FromLatLngToLocal(_previewLineEnd.Value);

                    // Малюємо лінію
                    e.Graphics.DrawLine(_previewLinePen, (int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y);
                }
            }
            catch { }
        }

        /// <summary>
        /// Очищення ресурсів
        /// </summary>
        public void Dispose()
        {
            _mapControl.Paint -= OnMapPaint;
            _previewLinePen?.Dispose();
        }
    }
}
