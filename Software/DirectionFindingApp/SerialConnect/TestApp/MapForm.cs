using System;
using System.Drawing;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;

namespace TestApp
{
    public partial class MapForm : Form
    {
        private Form1 _parentForm;
        public GMapOverlay? MarkersOverlay { get; private set; }
        public GMapOverlay? RoutesOverlay { get; private set; }
        private ToolTip _mapToolTip = new ToolTip();
        private MapPreviewRenderer? _mapPreviewRenderer = null;
        private bool _isUpdating = false;

        public MapForm(Form1 parentForm)
        {
            InitializeComponent();
            _parentForm = parentForm;

            // Налаштування форми
            this.Text = "Мапа";
            this.Size = new System.Drawing.Size(900, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Підключаємо обробники кнопок
            buttonSetCoords.Click += buttonSetCoords_Click;
            buttonSaveAz.Click += buttonSaveAz_Click;
            buttonClearAz.Click += buttonClearAz_Click;

            InitializeMap();

            // Обробник закриття форми
            this.FormClosing += MapForm_FormClosing;
        }

        private void buttonSetCoords_Click(object? sender, EventArgs e)
        {
            Console.WriteLine("MapForm.buttonSetCoords_Click: Button clicked");
            if (_parentForm != null)
            {
                // Викликаємо через рефлексію для уникнення проблем компіляції
                var method = _parentForm.GetType().GetMethod("SetStationCoordinatesFromExternal");
                method?.Invoke(_parentForm, null);
                Console.WriteLine("MapForm.buttonSetCoords_Click: SetStationCoordinatesFromExternal called, now updating map");
                UpdateMapData();
            }
        }

        private void buttonSaveAz_Click(object? sender, EventArgs e)
        {
            Console.WriteLine("MapForm.buttonSaveAz_Click: Button clicked");
            if (_parentForm != null)
            {
                var method = _parentForm.GetType().GetMethod("SaveAzimuthFromExternal");
                method?.Invoke(_parentForm, null);
                Console.WriteLine("MapForm.buttonSaveAz_Click: SaveAzimuthFromExternal called, now updating map");
                UpdateMapData();
            }
        }

        private void buttonClearAz_Click(object? sender, EventArgs e)
        {
            Console.WriteLine("MapForm.buttonClearAz_Click: Button clicked");
            if (_parentForm != null)
            {
                var method = _parentForm.GetType().GetMethod("ClearAzimuthsFromExternal");
                method?.Invoke(_parentForm, null);
                Console.WriteLine("MapForm.buttonClearAz_Click: ClearAzimuthsFromExternal called, now updating map");
                UpdateMapData();
            }
        }

        private void InitializeMap()
        {
            var selectedProvider = MapProvidersCatalog.Resolve(SettingsManager.Current.Map.MapProvider);

            // Налаштування GMap.NET
            gMapControl.MapProvider = selectedProvider.Provider;
            GMaps.Instance.Mode = AccessMode.ServerAndCache;

            // Отримуємо налаштування з Form1
            var settings = SettingsManager.Current;
            gMapControl.Position = new PointLatLng(settings.Map.Latitude, settings.Map.Longitude);
            gMapControl.MinZoom = 2;
            gMapControl.MaxZoom = 18;
            gMapControl.Zoom = settings.Map.Zoom;

            // Налаштування відображення
            gMapControl.ShowCenter = true;
            gMapControl.DragButton = MouseButtons.Left;

            // Створення оверлеїв
            MarkersOverlay = new GMapOverlay("markers");
            RoutesOverlay = new GMapOverlay("routes");
            gMapControl.Overlays.Add(RoutesOverlay);
            gMapControl.Overlays.Add(MarkersOverlay);

            // Ініціалізуємо MapPreviewRenderer для малювання preview лінії
            _mapPreviewRenderer = new MapPreviewRenderer(gMapControl, _mapToolTip);

            // Копіюємо дані з основної карти Form1
            RefreshMapData();

            // Обробники подій
            gMapControl.OnMarkerEnter += (marker) =>
            {
                if (marker?.Tag?.ToString() == "station")
                {
                    gMapControl.Cursor = Cursors.Hand;
                }
            };

            gMapControl.OnMarkerLeave += (marker) =>
            {
                gMapControl.Cursor = Cursors.Default;
            };

            // Обробник для оновлення після переміщення маркера
            gMapControl.MouseUp += (sender, e) =>
            {
                if (gMapControl.IsMouseOverMarker && MarkersOverlay != null)
                {
                    // Знаходимо маркер станції
                    GMapMarker? stationMarker = null;
                    foreach (var marker in MarkersOverlay.Markers)
                    {
                        if (marker.Tag?.ToString() == "station")
                        {
                            stationMarker = marker;
                            break;
                        }
                    }

                    if (stationMarker != null && _parentForm != null)
                    {
                        // Повідомляємо Form1 про нову позицію
                        _parentForm.UpdateStationPositionFromExternal(stationMarker.Position);
                        // Оновлюємо карту
                        UpdateMapData();
                    }
                }
            };

            gMapControl.MouseMove += GMapControl_MouseMove;
            gMapControl.MouseLeave += GMapControl_MouseLeave;
            gMapControl.MouseClick += GMapControl_MouseClick;
        }

        private void GMapControl_MouseMove(object? sender, MouseEventArgs e)
        {
            if (MarkersOverlay == null) return;

            try
            {
                // Знаходимо маркер станції
                GMapMarker? stationMarker = null;
                foreach (var marker in MarkersOverlay.Markers)
                {
                    if (marker.Tag?.ToString() == "station")
                    {
                        stationMarker = marker;
                        break;
                    }
                }

                if (stationMarker != null)
                {
                    int anAzValue = _parentForm.GetAnAzValue();
                    _mapPreviewRenderer?.HandleMouseMove(stationMarker, e, anAzValue);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MapForm MouseMove: {ex.Message}");
            }
        }

        private void GMapControl_MouseLeave(object? sender, EventArgs e)
        {
            _mapPreviewRenderer?.HandleMouseLeave();
        }

        private void GMapControl_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && MarkersOverlay != null)
            {
                try
                {
                    // Знаходимо маркер станції
                    GMapMarker? stationMarker = null;
                    foreach (var marker in MarkersOverlay.Markers)
                    {
                        if (marker.Tag?.ToString() == "station")
                        {
                            stationMarker = marker;
                            break;
                        }
                    }

                    if (stationMarker == null) return;

                    // Обчислюємо азимут
                    PointLatLng clickPosition = gMapControl.FromLocalToLatLng(e.X, e.Y);
                    double azimuth = MapPreviewRenderer.CalculateAzimuth(stationMarker.Position, clickPosition);
                    int azimuthInt = (int)Math.Round(azimuth);
                    azimuthInt = azimuthInt == 360 ? 0 : azimuthInt;

                    // Повідомляємо Form1
                    _parentForm.SetAzimuthFromExternalMap(azimuthInt);

                    // Оновлюємо карту
                    UpdateMapData();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in MapForm MouseClick: {ex.Message}");
                }
            }
        }

        public void RefreshMapData()
        {
            if (_isUpdating)
            {
                Console.WriteLine("MapForm.RefreshMapData: Already updating, skipping");
                return;
            }

            if (MarkersOverlay != null && RoutesOverlay != null && _parentForm != null)
            {
                try
                {
                    _isUpdating = true;
                    Console.WriteLine("MapForm.RefreshMapData: Starting refresh");

                    // Очищаємо старі дані
                    MarkersOverlay.Markers.Clear();
                    RoutesOverlay.Routes.Clear();
                    RoutesOverlay.Polygons.Clear();

                    // Копіюємо дані з Form1
                    _parentForm.CopyMapDataToExternal(MarkersOverlay, RoutesOverlay);

                    Console.WriteLine($"MapForm.RefreshMapData: After copy - Routes: {RoutesOverlay.Routes.Count}, Polygons: {RoutesOverlay.Polygons.Count}");

                    gMapControl.Refresh();
                }
                finally
                {
                    _isUpdating = false;
                }
            }
        }

        public void UpdateMapData()
        {
            RefreshMapData();
        }

        public void UpdateMapProvider(string providerId)
        {
            var descriptor = MapProvidersCatalog.Resolve(providerId);
            gMapControl.MapProvider = descriptor.Provider;
            gMapControl.ReloadMap();
            gMapControl.Refresh();
        }

        private void MapForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _mapPreviewRenderer?.Dispose();
            _mapPreviewRenderer = null;
            _mapToolTip?.Dispose();

            if (_parentForm != null)
            {
                // Повідомляємо Form1 що форма закривається
                try
                {
                    // Використовуємо рефлексію щоб викликати метод
                    var method = _parentForm.GetType().GetMethod("OnMapFormClosed");
                    method?.Invoke(_parentForm, null);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error calling OnMapFormClosed: {ex.Message}");
                }
            }
        }
    }
}
