using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SpectrozirRotatorImitator.Models;

namespace SpectrozirRotatorImitator
{
    public class RotatorServer
    {
        private readonly object _sync = new object();
        private readonly List<WebSocket> _clients = new List<WebSocket>();

        private HttpListener _listener;
        private CancellationTokenSource _cts;
        private Settings _settings;

        private readonly string _settingsFilePath = "settings.json";
        private readonly string[] _htmlFileCandidates = { "Antenna Rotator.mhtml", "Antenna Rotator.html" };

        private double _currentAngle;
        private double _targetAngle;
        private string _lastBroadcastPayload;
        private int _azimuth;
        private int _dac;
        private int _activePort;

        public RotatorServer()
        {
            LoadSettings();
            _azimuth = _settings.Azimuth;
            _dac = _settings.Calibration0;
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _activePort = StartHttpListenerStrict(_settings.Port);

            Task.Run(() => HttpLoopAsync(_cts.Token));
            Task.Run(() => RotationLoopAsync(_cts.Token));

            PrintStartupInfo();
        }

        public void Stop()
        {
            try
            {
                _cts?.Cancel();
            }
            catch
            {
            }

            CloseAllSocketsAsync().GetAwaiter().GetResult();

            if (_listener != null)
            {
                try { _listener.Stop(); } catch { }
                try { _listener.Close(); } catch { }
            }
        }

        public void SetTargetAngle(double angle)
        {
            lock (_sync)
            {
                _targetAngle = Math.Max(-(_settings.Sweep / 2.0), Math.Min(_settings.Sweep / 2.0, angle));
            }
            LogConsole("[ROTATOR] Target angle set to: " + Math.Round(_targetAngle, 2) + "°", ConsoleColor.Magenta);
        }

        private int StartHttpListenerStrict(int configuredPort)
        {
            try
            {
                _listener = BuildListener(configuredPort);
                _listener.Start();
                LogConsole("[HTTP] Listener started on configured port " + configuredPort, ConsoleColor.Cyan);
                return configuredPort;
            }
            catch (Exception ex)
            {
                LogConsole("[HTTP] Failed to start on configured port " + configuredPort + ": " + ex.Message, ConsoleColor.Red);
                throw new InvalidOperationException("Cannot start listener on configured port " + configuredPort + ". Run as Administrator or reserve URL ACL for this port.", ex);
            }
        }

        private HttpListener BuildListener(int port)
        {
            var listener = new HttpListener();

            string ip = string.IsNullOrWhiteSpace(_settings.IP) ? "127.0.0.1" : _settings.IP.Trim();
            if (ip == "0.0.0.0")
            {
                ip = "+";
            }

            listener.Prefixes.Add("http://" + ip + ":" + port + "/");

            return listener;
        }

        private async Task HttpLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                HttpListenerContext context = null;
                try
                {
                    context = await _listener.GetContextAsync();
                    _ = Task.Run(() => ProcessRequestAsync(context, token), token);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    LogConsole("[HTTP] Loop error: " + ex.Message, ConsoleColor.Red);
                }
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext context, CancellationToken token)
        {
            var request = context.Request;
            var response = context.Response;
            string path = (request.Url?.AbsolutePath ?? "/").ToLowerInvariant();

            try
            {
                LogConsole("[HTTP] " + request.HttpMethod + " " + path, ConsoleColor.Yellow);

                if (path == "/ws" && request.IsWebSocketRequest)
                {
                    await HandleWebSocketAsync(context, token);
                    return;
                }

                if (path == "/" && request.HttpMethod == "GET")
                {
                    ServeRootHtml(response);
                }
                else if (path == "/settings" && request.HttpMethod == "GET")
                {
                    HandleGetSettings(response);
                }
                else if (path == "/settings/angle" && request.HttpMethod == "POST")
                {
                    HandlePostSettingsAngle(request, response);
                }
                else if (path == "/settings/geo" && request.HttpMethod == "POST")
                {
                    HandlePostSettingsGeo(request, response);
                }
                else if (path == "/settings/ip" && request.HttpMethod == "POST")
                {
                    HandlePostSettingsIP(request, response);
                }
                else if (path == "/settings/cal" && request.HttpMethod == "POST")
                {
                    HandlePostSettingsCal(request, response);
                }
                else if (path == "/favicon.png")
                {
                    response.StatusCode = 204;
                }
                else
                {
                    Send404(response);
                }
            }
            catch (Exception ex)
            {
                LogConsole("[HTTP] Request error: " + ex.Message, ConsoleColor.Red);
                TrySend500(response);
            }
            finally
            {
                try { response.Close(); } catch { }
            }
        }

        private async Task HandleWebSocketAsync(HttpListenerContext context, CancellationToken token)
        {
            HttpListenerWebSocketContext wsContext;
            try
            {
                wsContext = await context.AcceptWebSocketAsync(null);
            }
            catch (Exception ex)
            {
                LogConsole("[WS] Upgrade failed: " + ex.Message, ConsoleColor.Red);
                context.Response.StatusCode = 400;
                context.Response.Close();
                return;
            }

            var socket = wsContext.WebSocket;
            lock (_sync)
            {
                _clients.Add(socket);
            }

            LogConsole("[WS] Client connected", ConsoleColor.Cyan);
            await SendStateToSocketAsync(socket, token);

            try
            {
                await ReceiveLoopAsync(socket, token);
            }
            finally
            {
                lock (_sync)
                {
                    _clients.Remove(socket);
                }

                if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
                {
                    try
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutdown", CancellationToken.None);
                    }
                    catch
                    {
                    }
                }

                socket.Dispose();
                LogConsole("[WS] Client disconnected", ConsoleColor.Cyan);
            }
        }

        private async Task ReceiveLoopAsync(WebSocket socket, CancellationToken token)
        {
            var buffer = new byte[4096];

            while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                using (var ms = new MemoryStream())
                {
                    do
                    {
                        result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            return;
                        }

                        ms.Write(buffer, 0, result.Count);
                    }
                    while (!result.EndOfMessage);

                    if (result.MessageType != WebSocketMessageType.Text)
                    {
                        continue;
                    }

                    string message = Encoding.UTF8.GetString(ms.ToArray());
                    LogConsole("[WS] Received: " + message, ConsoleColor.Yellow);
                    ProcessWebSocketCommand(message);
                }
            }
        }

        private void ProcessWebSocketCommand(string message)
        {
            try
            {
                dynamic json = JsonConvert.DeserializeObject(message);
                string command = json.command;

                if (string.Equals(command, "setAngle", StringComparison.OrdinalIgnoreCase))
                {
                    double value = (double)json.value;
                    SetTargetAngle(value);
                }
            }
            catch (Exception ex)
            {
                LogConsole("[WS] Parse error: " + ex.Message, ConsoleColor.Red);
            }
        }

        private async Task RotationLoopAsync(CancellationToken token)
        {
            const double rotationSpeedPerTick = 2.5;
            const int tickMs = 50;

            while (!token.IsCancellationRequested)
            {
                bool changed = false;

                lock (_sync)
                {
                    if (Math.Abs(_currentAngle - _targetAngle) > 0.1)
                    {
                        double diff = _targetAngle - _currentAngle;
                        double step = Math.Sign(diff) * rotationSpeedPerTick;
                        if (Math.Abs(step) > Math.Abs(diff))
                        {
                            _currentAngle = _targetAngle;
                        }
                        else
                        {
                            _currentAngle += step;
                        }

                        _dac = CalculateDac(_currentAngle);
                        changed = true;
                    }
                }

                if (changed)
                {
                    await BroadcastStateAsync(token);
                }

                await Task.Delay(tickMs, token);
            }
        }

        private int CalculateDac(double angle)
        {
            if (angle <= -180) return _settings.Calibration180;
            if (angle >= 180) return _settings.CalibrationNeg180;

            if (angle <= -90)
            {
                double t = (angle + 180) / 90;
                return (int)Math.Round(_settings.Calibration180 + (_settings.CalibrationNeg90 - _settings.Calibration180) * t);
            }

            if (angle <= 0)
            {
                double t = (angle + 90) / 90;
                return (int)Math.Round(_settings.CalibrationNeg90 + (_settings.Calibration0 - _settings.CalibrationNeg90) * t);
            }

            if (angle <= 90)
            {
                double t = angle / 90;
                return (int)Math.Round(_settings.Calibration0 + (_settings.Calibration90 - _settings.Calibration0) * t);
            }

            double x = (angle - 90) / 90;
            return (int)Math.Round(_settings.Calibration90 + (_settings.CalibrationNeg180 - _settings.Calibration90) * x);
        }

        private async Task SendStateToSocketAsync(WebSocket socket, CancellationToken token)
        {
            if (socket == null || socket.State != WebSocketState.Open)
            {
                return;
            }

            string payload = BuildStatePayload();

            byte[] bytes = Encoding.UTF8.GetBytes(payload);
            await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);
            LogConsole("[WS] Sent: " + payload, ConsoleColor.Green);
        }

        private async Task BroadcastStateAsync(CancellationToken token)
        {
            string payload = BuildStatePayload();

            lock (_sync)
            {
                if (string.Equals(payload, _lastBroadcastPayload, StringComparison.Ordinal))
                {
                    return;
                }

                _lastBroadcastPayload = payload;
            }

            WebSocket[] sockets;
            lock (_sync)
            {
                sockets = _clients.Where(c => c != null && c.State == WebSocketState.Open).ToArray();
            }

            foreach (var socket in sockets)
            {
                try
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(payload);
                    await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);
                    LogConsole("[WS] Sent: " + payload, ConsoleColor.Green);
                }
                catch (Exception ex)
                {
                    LogConsole("[WS] Broadcast error: " + ex.Message, ConsoleColor.Red);
                }
            }
        }

        private string BuildStatePayload()
        {
            lock (_sync)
            {
                return JsonConvert.SerializeObject(new RotatorState
                {
                    Command = "angle",
                    CurrentAngle = Math.Round(_currentAngle, 2),
                    RequestedAngle = Math.Round(_targetAngle, 2),
                    AzimuthAngle = _azimuth,
                    DAC = _dac
                });
            }
        }

        private async Task CloseAllSocketsAsync()
        {
            WebSocket[] sockets;
            lock (_sync)
            {
                sockets = _clients.ToArray();
                _clients.Clear();
            }

            foreach (var socket in sockets)
            {
                try
                {
                    if (socket != null && socket.State == WebSocketState.Open)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Stopping", CancellationToken.None);
                    }
                }
                catch
                {
                }
                finally
                {
                    try { socket?.Dispose(); } catch { }
                }
            }
        }

        private void ServeRootHtml(HttpListenerResponse response)
        {
            string htmlPath = _htmlFileCandidates.FirstOrDefault(File.Exists);
            if (string.IsNullOrEmpty(htmlPath))
            {
                response.StatusCode = 404;
                SendJsonResponse(response, "{\"error\":\"Antenna Rotator html file not found\"}");
                return;
            }

            byte[] data = File.ReadAllBytes(htmlPath);
            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength64 = data.Length;
            response.OutputStream.Write(data, 0, data.Length);

            LogConsole("[HTTP] Sent HTML content", ConsoleColor.Green);
        }

        private void HandleGetSettings(HttpListenerResponse response)
        {
            var payload = new
            {
                sweep = _settings.Sweep,
                max_sweep = _settings.MaxSweep,
                azimuth = _azimuth,
                lat = _settings.Latitude,
                lng = _settings.Longitude,
                ip = _settings.IP,
                sn = _settings.SerialNumber,
                version = _settings.Version,
                reverse = _settings.Reverse,
                c_n_180 = _settings.CalibrationNeg180,
                c_n_90 = _settings.CalibrationNeg90,
                c_0 = _settings.Calibration0,
                c_90 = _settings.Calibration90,
                c_180 = _settings.Calibration180
            };

            string json = JsonConvert.SerializeObject(payload, Formatting.Indented);
            SendJsonResponse(response, json);
            LogConsole("[HTTP] Response /settings: " + json, ConsoleColor.Green);
        }

        private void HandlePostSettingsAngle(HttpListenerRequest request, HttpListenerResponse response)
        {
            string body = ReadBody(request);
            LogConsole("[HTTP] Request /settings/angle: " + body, ConsoleColor.Yellow);

            dynamic json = JsonConvert.DeserializeObject(body);
            _settings.Sweep = (int)json.sweep;
            _settings.MaxSweep = _settings.Sweep;
            _azimuth = (int)json.azimuth;
            _settings.Azimuth = _azimuth;

            SaveSettings();
            SendJsonResponse(response, "{\"ok\":true}");
            LogConsole("[HTTP] Response /settings/angle: {\"ok\":true}", ConsoleColor.Green);
        }

        private void HandlePostSettingsGeo(HttpListenerRequest request, HttpListenerResponse response)
        {
            string body = ReadBody(request);
            LogConsole("[HTTP] Request /settings/geo: " + body, ConsoleColor.Yellow);

            dynamic json = JsonConvert.DeserializeObject(body);
            _settings.Latitude = (double)json.lat;
            _settings.Longitude = (double)json.lng;

            SaveSettings();
            SendJsonResponse(response, "{\"ok\":true}");
            LogConsole("[HTTP] Response /settings/geo: {\"ok\":true}", ConsoleColor.Green);
        }

        private void HandlePostSettingsIP(HttpListenerRequest request, HttpListenerResponse response)
        {
            string body = ReadBody(request);
            LogConsole("[HTTP] Request /settings/ip: " + body, ConsoleColor.Yellow);

            dynamic json = JsonConvert.DeserializeObject(body);
            _settings.IP = (string)json.ip;

            SaveSettings();
            SendJsonResponse(response, "{\"ok\":true}");
            LogConsole("[HTTP] Response /settings/ip: {\"ok\":true}", ConsoleColor.Green);
        }

        private void HandlePostSettingsCal(HttpListenerRequest request, HttpListenerResponse response)
        {
            string body = ReadBody(request);
            LogConsole("[HTTP] Request /settings/cal: " + body, ConsoleColor.Yellow);

            dynamic json = JsonConvert.DeserializeObject(body);
            _settings.CalibrationNeg180 = (int)json.c_n_180;
            _settings.CalibrationNeg90 = (int)json.c_n_90;
            _settings.Calibration0 = (int)json.c_0;
            _settings.Calibration90 = (int)json.c_90;
            _settings.Calibration180 = (int)json.c_180;

            SaveSettings();
            SendJsonResponse(response, "{\"ok\":true}");
            LogConsole("[HTTP] Response /settings/cal: {\"ok\":true}", ConsoleColor.Green);
        }

        private static string ReadBody(HttpListenerRequest request)
        {
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                return reader.ReadToEnd();
            }
        }

        private static void SendJsonResponse(HttpListenerResponse response, string json)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            response.ContentType = "application/json; charset=utf-8";
            response.ContentLength64 = bytes.Length;
            response.OutputStream.Write(bytes, 0, bytes.Length);
        }

        private static void Send404(HttpListenerResponse response)
        {
            response.StatusCode = 404;
            SendJsonResponse(response, "{\"error\":\"Not found\"}");
        }

        private static void TrySend500(HttpListenerResponse response)
        {
            try
            {
                response.StatusCode = 500;
                SendJsonResponse(response, "{\"error\":\"Internal server error\"}");
            }
            catch
            {
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    _settings = JsonConvert.DeserializeObject<Settings>(json) ?? new Settings();
                }
                else
                {
                    _settings = new Settings();
                    SaveSettings();
                }
            }
            catch
            {
                _settings = new Settings();
            }

            if (_settings.Port <= 0 || _settings.Port > 65535)
            {
                _settings.Port = 8080;
            }

            if (string.IsNullOrWhiteSpace(_settings.IP))
            {
                _settings.IP = "127.0.0.1";
            }
        }

        private void SaveSettings()
        {
            string json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
            File.WriteAllText(_settingsFilePath, json, Encoding.UTF8);
            LogConsole("[CONFIG] Settings saved", ConsoleColor.Green);
        }

        private void PrintStartupInfo()
        {
            Console.Clear();
            LogConsole("============================================", ConsoleColor.Cyan);
            LogConsole(" Spectrozir Rotator Imitation Server", ConsoleColor.Cyan);
            LogConsole("============================================", ConsoleColor.Cyan);
            LogConsole(string.Empty, ConsoleColor.White);
            LogConsole("HTTP: http://" + _settings.IP + ":" + _activePort + "/", ConsoleColor.Green);
            LogConsole("WS:   ws://" + _settings.IP + ":" + _activePort + "/ws", ConsoleColor.Green);
            LogConsole(string.Empty, ConsoleColor.White);
            LogConsole("Press CTRL+C to stop", ConsoleColor.Yellow);
            LogConsole(string.Empty, ConsoleColor.White);
        }

        private static void LogConsole(string message, ConsoleColor color)
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss.fff") + "] " + message);
            Console.ForegroundColor = old;
        }
    }
}
