using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;

namespace TestApp
{
    /// <summary>
    /// Клас для керування всіма операціями з картою
    /// </summary>
    public class MapController
    {
        private readonly GMapControl _mapControl;
        private readonly GMapOverlay _routesOverlay;
        private GMapRoute? _azimuthLine = null;
        private GMapPolygon? _azimuthPolygon = null;
        private GMapPolygon? _forbiddenPolygon = null;
        private readonly List<GMapRoute> _savedBearingLines = new List<GMapRoute>();

        public GMapMarker? StationMarker { get; set; }
        public double LastKnownAzimuth { get; set; }
        public int AnAzValue { get; set; }
        public int ForbiddenStartAN { get; set; }
        public int ForbiddenEndAN { get; set; }

        public MapController(GMapControl mapControl, GMapOverlay routesOverlay)
        {
            _mapControl = mapControl ?? throw new ArgumentNullException(nameof(mapControl));
            _routesOverlay = routesOverlay ?? throw new ArgumentNullException(nameof(routesOverlay));
        }

        /// <summary>
        /// Оновлює лінію азимуту та полігон
        /// </summary>
        public void UpdateAzimuthLine()
        {
            if (StationMarker == null) return;

            try
            {
                // Видаляємо стару лінію
                if (_azimuthLine != null)
                {
                    _routesOverlay.Routes.Remove(_azimuthLine);
                    _azimuthLine = null;
                }

                // Видаляємо старий полігон
                if (_azimuthPolygon != null)
                {
                    _routesOverlay.Polygons.Remove(_azimuthPolygon);
                    _azimuthPolygon = null;
                }

                // Обчислюємо кінцеву точку лінії на відстані 150 км
                PointLatLng startPoint = StationMarker.Position;
                PointLatLng endPoint = CalculateDestinationPoint(startPoint, LastKnownAzimuth, 150.0);

                // Створюємо нову лінію
                List<PointLatLng> points = new List<PointLatLng> { startPoint, endPoint };
                _azimuthLine = new GMapRoute(points, "azimuth");
                _azimuthLine.Stroke = new Pen(Color.BlueViolet, 3);

                _routesOverlay.Routes.Add(_azimuthLine);

                // Створюємо полігон: центр, ліва точка (-7°), права точка (+7°)
                double leftAzimuth = (LastKnownAzimuth - 7 + 360) % 360;
                double rightAzimuth = (LastKnownAzimuth + 7) % 360;

                PointLatLng leftPoint = CalculateDestinationPoint(startPoint, leftAzimuth, 150.0);
                PointLatLng rightPoint = CalculateDestinationPoint(startPoint, rightAzimuth, 150.0);

                List<PointLatLng> polygonPoints = new List<PointLatLng>
                {
                    startPoint,
                    leftPoint,
                    rightPoint
                };

                _azimuthPolygon = new GMapPolygon(polygonPoints, "azimuthPolygon");
                _azimuthPolygon.Fill = new SolidBrush(Color.FromArgb(102, Color.BlueViolet)); // 0.4 opacity = 102/255
                _azimuthPolygon.Stroke = new Pen(Color.BlueViolet, 1);

                _routesOverlay.Polygons.Add(_azimuthPolygon);

                // Оновлюємо полігон сліпої зони
                UpdateForbiddenZonePolygon();

                if (_mapControl != null && !_mapControl.IsDisposed)
                {
                    _mapControl.Refresh();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MapController.UpdateAzimuthLine: {ex.Message}");
            }
        }

        /// <summary>
        /// Зберігає поточний пеленг
        /// </summary>
        public void SaveAzimuth()
        {
            if (StationMarker == null) return;

            try
            {
                Console.WriteLine($"MapController.SaveAzimuth: Saving azimuth {LastKnownAzimuth}°");
                
                // Обчислюємо кінцеву точку лінії на відстані 150 км
                PointLatLng startPoint = StationMarker.Position;
                PointLatLng endPoint = CalculateDestinationPoint(startPoint, LastKnownAzimuth, 150.0);

                // Створюємо нову пунктирну напівпрозору лінію для збереженого пеленгу
                List<PointLatLng> points = new List<PointLatLng> { startPoint, endPoint };
                GMapRoute savedLine = new GMapRoute(points, $"saved_azimuth_{LastKnownAzimuth:000.0}");

                // Напівпрозорий червоний колір (128 = 50% прозорості)
                Pen dashedPen = new Pen(Color.FromArgb(128, Color.Red), 2);
                dashedPen.DashStyle = DashStyle.Dash;
                savedLine.Stroke = dashedPen;

                _routesOverlay.Routes.Add(savedLine);
                _savedBearingLines.Add(savedLine);

                Console.WriteLine($"MapController.SaveAzimuth: Line added. Total routes in overlay: {_routesOverlay.Routes.Count}, Total saved bearings: {_savedBearingLines.Count}");

                _mapControl.Refresh();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MapController.SaveAzimuth: {ex.Message}");
            }
        }

        /// <summary>
        /// Очищає всі збережені пеленги
        /// </summary>
        public void ClearAzimuths()
        {
            try
            {
                // Видаляємо всі збережені лінії пеленгів
                foreach (var line in _savedBearingLines)
                {
                    _routesOverlay.Routes.Remove(line);
                }
                _savedBearingLines.Clear();

                _mapControl.Refresh();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MapController.ClearAzimuths: {ex.Message}");
            }
        }

        /// <summary>
        /// Повертає кількість збережених пеленгів
        /// </summary>
        public int GetSavedBearingsCount()
        {
            return _savedBearingLines.Count;
        }

        /// <summary>
        /// Оновлює полігон сліпої зони
        /// </summary>
        private void UpdateForbiddenZonePolygon()
        {
            if (StationMarker == null) return;

            try
            {
                // Видаляємо старий полігон
                if (_forbiddenPolygon != null)
                {
                    _routesOverlay.Polygons.Remove(_forbiddenPolygon);
                    _forbiddenPolygon = null;
                }

                // Конвертуємо заборонену зону з кута антени в азимут
                int startAngleAZ = ((ForbiddenStartAN + AnAzValue - 180) + 360) % 360;
                int endAngleAZ = ((ForbiddenEndAN + AnAzValue - 180) + 360) % 360;

                // Відстань полігону (150 км)
                double distance = 150.0;
                PointLatLng center = StationMarker.Position;

                // Створюємо точки полігону
                List<PointLatLng> polygonPoints = new List<PointLatLng>();
                polygonPoints.Add(center);

                // Визначаємо напрямок обходу (від endAngleAZ до startAngleAZ)
                int currentAngle = endAngleAZ;

                while (true)
                {
                    PointLatLng point = CalculateDestinationPoint(center, currentAngle, distance);
                    polygonPoints.Add(point);

                    if (currentAngle == startAngleAZ)
                        break;

                    // Йдемо проти годинникової стрілки
                    currentAngle--;
                    if (currentAngle < 0)
                        currentAngle = 359;

                    // Захист від безкінечного циклу
                    if (polygonPoints.Count > 360)
                        break;
                }

                // Створюємо полігон
                _forbiddenPolygon = new GMapPolygon(polygonPoints, "forbiddenZone");
                _forbiddenPolygon.Fill = new SolidBrush(Color.FromArgb(102, Color.Red)); // 40% прозорості
                _forbiddenPolygon.Stroke = new Pen(Color.FromArgb(180, Color.Red), 1); // Напівпрозорий бордер

                _routesOverlay.Polygons.Add(_forbiddenPolygon);

                if (_mapControl != null && !_mapControl.IsDisposed)
                {
                    _mapControl.Refresh();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MapController.UpdateForbiddenZonePolygon: {ex.Message}");
            }
        }

        /// <summary>
        /// Обчислює точку призначення за заданим азимутом та відстанню
        /// </summary>
        public static PointLatLng CalculateDestinationPoint(PointLatLng start, double bearing, double distanceKm)
        {
            const double earthRadius = 6371.0;

            double lat1 = start.Lat * Math.PI / 180.0;
            double lon1 = start.Lng * Math.PI / 180.0;
            double bearingRad = bearing * Math.PI / 180.0;
            double distRad = distanceKm / earthRadius;

            double lat2 = Math.Asin(
                Math.Sin(lat1) * Math.Cos(distRad) +
                Math.Cos(lat1) * Math.Sin(distRad) * Math.Cos(bearingRad)
            );

            double lon2 = lon1 + Math.Atan2(
                Math.Sin(bearingRad) * Math.Sin(distRad) * Math.Cos(lat1),
                Math.Cos(distRad) - Math.Sin(lat1) * Math.Sin(lat2)
            );

            double lat2Deg = lat2 * 180.0 / Math.PI;
            double lon2Deg = lon2 * 180.0 / Math.PI;

            return new PointLatLng(lat2Deg, lon2Deg);
        }

        /// <summary>
        /// Копіює дані карти до зовнішніх оверлеїв
        /// </summary>
        public void CopyMapDataToExternal(GMapOverlay targetMarkersOverlay, GMapOverlay targetRoutesOverlay, GMapOverlay sourceMarkersOverlay)
        {
            Console.WriteLine($"CopyMapDataToExternal: Starting copy. Source routes count: {_routesOverlay.Routes.Count}, Saved bearings count: {_savedBearingLines.Count}");
            
            // Копіюємо маркери
            if (sourceMarkersOverlay != null)
            {
                foreach (var marker in sourceMarkersOverlay.Markers)
                {
                    var newMarker = new GMarkerGoogle(marker.Position, GMarkerGoogleType.red_small)
                    {
                        Tag = marker.Tag,
                        ToolTipText = marker.ToolTipText,
                        IsHitTestVisible = true
                    };
                    targetMarkersOverlay.Markers.Add(newMarker);
                }
            }

            // Копіюємо маршрути та полігони
            int copiedRoutes = 0;
            foreach (var route in _routesOverlay.Routes)
            {
                if (route.Name == "mousePreview") continue; // Пропускаємо тимчасові лінії

                var newRoute = new GMapRoute(route.Points, route.Name);
                
                // Клонуємо Pen з усіма властивостями
                if (route.Stroke != null)
                {
                    Pen clonedPen = new Pen(route.Stroke.Color, route.Stroke.Width);
                    clonedPen.DashStyle = route.Stroke.DashStyle;
                    
                    // Копіюємо DashPattern тільки якщо стиль Custom
                    if (route.Stroke.DashStyle == DashStyle.Custom && route.Stroke.DashPattern != null)
                    {
                        clonedPen.DashPattern = (float[])route.Stroke.DashPattern.Clone();
                    }
                    
                    clonedPen.StartCap = route.Stroke.StartCap;
                    clonedPen.EndCap = route.Stroke.EndCap;
                    newRoute.Stroke = clonedPen;
                }
                
                targetRoutesOverlay.Routes.Add(newRoute);
                copiedRoutes++;
                Console.WriteLine($"CopyMapDataToExternal: Copied route '{route.Name}' with stroke color {route.Stroke?.Color} and dash style {route.Stroke?.DashStyle}");
            }

            int copiedPolygons = 0;
            foreach (var polygon in _routesOverlay.Polygons)
            {
                var newPolygon = new GMapPolygon(polygon.Points, polygon.Name);
                
                // Клонуємо Fill brush
                if (polygon.Fill != null && polygon.Fill is SolidBrush solidBrush)
                {
                    newPolygon.Fill = new SolidBrush(solidBrush.Color);
                }
                
                // Клонуємо Stroke pen з усіма властивостями
                if (polygon.Stroke != null)
                {
                    Pen clonedPen = new Pen(polygon.Stroke.Color, polygon.Stroke.Width);
                    clonedPen.DashStyle = polygon.Stroke.DashStyle;
                    
                    // Копіюємо DashPattern тільки якщо стиль Custom
                    if (polygon.Stroke.DashStyle == DashStyle.Custom && polygon.Stroke.DashPattern != null)
                    {
                        clonedPen.DashPattern = (float[])polygon.Stroke.DashPattern.Clone();
                    }
                    
                    clonedPen.StartCap = polygon.Stroke.StartCap;
                    clonedPen.EndCap = polygon.Stroke.EndCap;
                    newPolygon.Stroke = clonedPen;
                }
                
                targetRoutesOverlay.Polygons.Add(newPolygon);
                copiedPolygons++;
            }
            
            Console.WriteLine($"CopyMapDataToExternal: Finished. Copied {copiedRoutes} routes and {copiedPolygons} polygons");
        }

        /// <summary>
        /// Перевірка чи знаходиться азимут у сліпій зоні
        /// </summary>
        public bool IsInForbiddenZone(int azimuth)
        {
            // Конвертуємо заборонену зону з кута антени в азимут
            int forbiddenStartAZ = ((ForbiddenStartAN + AnAzValue - 180) + 360) % 360;
            int forbiddenEndAZ = ((ForbiddenEndAN + AnAzValue - 180) + 360) % 360;

            // Нормалізуємо азимут до 0-359
            azimuth = ((azimuth % 360) + 360) % 360;

            // Перевіряємо чи потрапляє в заборонений діапазон
            if (forbiddenStartAZ <= forbiddenEndAZ)
            {
                return azimuth >= forbiddenStartAZ && azimuth <= forbiddenEndAZ;
            }
            else
            {
                return azimuth >= forbiddenStartAZ || azimuth <= forbiddenEndAZ;
            }
        }
    }
}
