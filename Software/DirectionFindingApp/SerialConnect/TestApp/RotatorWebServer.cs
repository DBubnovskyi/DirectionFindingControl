using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TestApp;

public sealed class RotatorWebServer : IDisposable
{
    private readonly object _sync = new();
    private readonly List<WebSocket> _clients = new();
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly Func<WebSnapshot> _snapshotProvider;
    private readonly Action<int> _onSweepChanged;
    private readonly Action<int> _onCalibrationRequested;
    private readonly Action<double, double> _onGeoChanged;
    private readonly Action<string> _onSerialCommandRequested;
    private readonly Action<string>? _logger;

    private WebApplication? _app;
    private bool _isRunning;

    private double _currentRelativeAngle;
    private double _requestedRelativeAngle;
    private int _currentAzimuth;
    private int _dac;

    public RotatorWebServer(
        Func<WebSnapshot> snapshotProvider,
        Action<int> onSweepChanged,
        Action<int> onCalibrationRequested,
        Action<double, double> onGeoChanged,
        Action<string> onSerialCommandRequested,
        Action<string>? logger = null)
    {
        _snapshotProvider = snapshotProvider;
        _onSweepChanged = onSweepChanged;
        _onCalibrationRequested = onCalibrationRequested;
        _onGeoChanged = onGeoChanged;
        _onSerialCommandRequested = onSerialCommandRequested;
        _logger = logger;
    }

    public bool IsRunning => _isRunning;

    public void Start(string ip, int port)
    {
        if (IsRunning)
        {
            return;
        }

        IPAddress listenAddress = ResolveListenAddress(ip);
        bool listenAllInterfaces = IPAddress.Any.Equals(listenAddress);

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = Array.Empty<string>()
        });

        builder.WebHost.UseKestrel(options =>
        {
            options.Listen(listenAddress, port);
        });

        var app = builder.Build();
        app.UseWebSockets();
        app.Use(async (context, next) =>
        {
            string path = context.Request.Path.HasValue
                ? context.Request.Path.Value!.ToLowerInvariant()
                : "/";
            Log($"[HTTP] {context.Request.Method} {path}");
            await next();
        });

        app.MapGet("/", async context => await ServeRootHtmlAsync(context.Response));
        app.MapGet("/settings", async context => await HandleGetSettingsAsync(context.Response));
        app.MapPost("/settings/angle", async context => await HandlePostSettingsAngleAsync(context.Request, context.Response));
        app.MapPost("/settings/geo", async context => await HandlePostSettingsGeoAsync(context.Request, context.Response));
        app.MapPost("/settings/ip", async context => await SendJsonAsync(context.Response, "{\"ok\":true}"));
        app.MapPost("/settings/cal", async context => await SendJsonAsync(context.Response, "{\"ok\":true}"));
        app.MapGet("/favicon.png", context =>
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
        app.Map("/ws", async context => await HandleWebSocketAsync(context, context.RequestAborted));
        app.MapFallback(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await SendJsonAsync(context.Response, "{\"error\":\"Not found\"}");
        });

        app.StartAsync().GetAwaiter().GetResult();

        _app = app;
        _isRunning = true;

        string configuredIp = listenAllInterfaces ? "0.0.0.0" : listenAddress.ToString();
        if (listenAllInterfaces)
        {
            Log($"[HTTP] Listener started on all interfaces (0.0.0.0):{port}");
            Log($"[WS] Endpoint ws://<this-host-ip>:{port}/ws");
        }
        else
        {
            Log($"[HTTP] Listener started on http://{configuredIp}:{port}/");
            Log($"[WS] Endpoint ws://{configuredIp}:{port}/ws");
        }
    }

    public void Stop()
    {
        try
        {
            _app?.StopAsync().GetAwaiter().GetResult();
        }
        catch
        {
        }

        try
        {
            CloseAllSocketsAsync().GetAwaiter().GetResult();
        }
        catch
        {
        }

        try
        {
            _app?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        catch
        {
        }

        _app = null;
        _isRunning = false;
        Log("[WEB] Server stopped");
    }

    public void NotifySettingsChanged()
    {
        _ = BroadcastCommandAsync("settings");
    }

    public void NotifySerialRx(string message)
    {
        UpdateAnglesFromSerialMessage(message);
        _ = BroadcastStateAsync();
        _ = BroadcastSerialAsync("rx", message);
    }

    public void NotifySerialTx(string message)
    {
        _ = BroadcastSerialAsync("tx", message);
    }

    private async Task HandleWebSocketAsync(HttpContext context, CancellationToken token)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await SendJsonAsync(context.Response, "{\"error\":\"Expected WebSocket upgrade\"}");
            return;
        }

        WebSocket socket;
        try
        {
            socket = await context.WebSockets.AcceptWebSocketAsync();
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            Log($"[WS] Upgrade failed: {ex.Message}");
            return;
        }

        lock (_sync)
        {
            _clients.Add(socket);
        }
        Log("[WS] Client connected");

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

            try
            {
                if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
            }
            catch
            {
            }

            socket.Dispose();
            Log("[WS] Client disconnected");
        }
    }

    private async Task ReceiveLoopAsync(WebSocket socket, CancellationToken token)
    {
        var buffer = new byte[8192];

        while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            using var ms = new MemoryStream();
            WebSocketReceiveResult result;

            do
            {
                result = await socket.ReceiveAsync(buffer, token);
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

            var message = Encoding.UTF8.GetString(ms.ToArray());
            Log($"[WS][RX] {message}");
            await ProcessWebSocketCommandAsync(message);
        }
    }

    private async Task ProcessWebSocketCommandAsync(string message)
    {
        try
        {
            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;
            if (!root.TryGetProperty("command", out var commandEl))
            {
                return;
            }

            var command = commandEl.GetString() ?? string.Empty;
            if (command.Equals("setAngle", StringComparison.OrdinalIgnoreCase) &&
                root.TryGetProperty("value", out var valueEl) &&
                valueEl.TryGetDouble(out var relativeAngle))
            {
                var snapshot = _snapshotProvider();
                double sweepHalf = Math.Max(0.0, snapshot.Sweep / 2.0);
                double clampedRelative = Math.Clamp(relativeAngle, -sweepHalf, sweepHalf);
                int absoluteAzimuth = Normalize360((int)Math.Round(snapshot.Azimuth + clampedRelative));
                _requestedRelativeAngle = clampedRelative;
                Log($"[WS] setAngle={relativeAngle:0.##} clamped={clampedRelative:0.##} => AZ={absoluteAzimuth}");
                _onSerialCommandRequested($"$AZ,{absoluteAzimuth};");
            }
            else if (command.Equals("serial", StringComparison.OrdinalIgnoreCase) &&
                     root.TryGetProperty("value", out var serialEl))
            {
                var serial = serialEl.GetString();
                if (!string.IsNullOrWhiteSpace(serial))
                {
                    Log($"[WS] serial command => {serial}");
                    _onSerialCommandRequested(serial);
                }
            }

            await BroadcastStateAsync();
        }
        catch (Exception ex)
        {
            Log($"[WS] Parse error: {ex.Message}");
        }
    }

    private async Task HandleGetSettingsAsync(HttpResponse response)
    {
        var snapshot = _snapshotProvider();
        string json = JsonSerializer.Serialize(snapshot, _jsonOptions);
        await SendJsonAsync(response, json);
        Log($"[HTTP] /settings => {json}");
    }

    private async Task HandlePostSettingsAngleAsync(HttpRequest request, HttpResponse response)
    {
        string body = await ReadBodyAsync(request);
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        int sweep = root.TryGetProperty("sweep", out var sweepEl) && sweepEl.TryGetInt32(out var sw)
            ? Math.Clamp(sw, 90, 360)
            : _snapshotProvider().Sweep;

        _onSweepChanged(sweep);

        if (root.TryGetProperty("azimuth", out var azEl) && azEl.TryGetInt32(out var az))
        {
            _onCalibrationRequested(Normalize360(az));
        }

        await SendJsonAsync(response, "{\"ok\":true}");
        Log($"[HTTP] /settings/angle payload => {body}");
        NotifySettingsChanged();
    }

    private async Task HandlePostSettingsGeoAsync(HttpRequest request, HttpResponse response)
    {
        string body = await ReadBodyAsync(request);
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        if (root.TryGetProperty("lat", out var latEl) && latEl.TryGetDouble(out var lat) &&
            root.TryGetProperty("lng", out var lngEl) && lngEl.TryGetDouble(out var lng))
        {
            _onGeoChanged(lat, lng);
            await SendJsonAsync(response, "{\"ok\":true}");
            Log($"[HTTP] /settings/geo payload => {body}");
            NotifySettingsChanged();
            return;
        }

        response.StatusCode = StatusCodes.Status400BadRequest;
        await SendJsonAsync(response, "{\"error\":\"Invalid geo payload\"}");
    }

    private async Task ServeRootHtmlAsync(HttpResponse response)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("Antenna Rotator.html", StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            response.StatusCode = StatusCodes.Status404NotFound;
            await SendJsonAsync(response, "{\"error\":\"Embedded html not found\"}");
            return;
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            response.StatusCode = StatusCodes.Status404NotFound;
            await SendJsonAsync(response, "{\"error\":\"Embedded html stream missing\"}");
            return;
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        byte[] bytes = ms.ToArray();

        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength = bytes.Length;
        await response.Body.WriteAsync(bytes);
    }

    private void UpdateAnglesFromSerialMessage(string message)
    {
        var snapshot = _snapshotProvider();
        foreach (string part in message.Trim().TrimEnd(';').Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var cmd = part.Trim();
            if (cmd.StartsWith("AZ,", StringComparison.OrdinalIgnoreCase))
            {
                string value = cmd[3..].Trim();
                if (double.TryParse(value, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var az))
                {
                    _currentAzimuth = Normalize360((int)Math.Round(az));
                    _currentRelativeAngle = Normalize180(az - snapshot.Azimuth);
                }
            }
            else if (cmd.StartsWith("DAC,", StringComparison.OrdinalIgnoreCase))
            {
                string value = cmd[4..].Trim();
                if (int.TryParse(value, out var dac))
                {
                    _dac = dac;
                }
            }
        }
    }

    private async Task BroadcastSerialAsync(string direction, string message)
    {
        var payload = JsonSerializer.Serialize(new { command = "serial", direction, data = message }, _jsonOptions);
        Log($"[WS][TX] serial/{direction}: {message}");
        await BroadcastTextAsync(payload);
    }

    private async Task BroadcastCommandAsync(string command)
    {
        var payload = JsonSerializer.Serialize(new { command }, _jsonOptions);
        Log($"[WS][TX] command={command}");
        await BroadcastTextAsync(payload);
    }

    private async Task BroadcastStateAsync()
    {
        var payload = JsonSerializer.Serialize(new WebState
        {
            Command = "angle",
            CurrentAngle = Math.Round(_currentRelativeAngle, 2),
            RequestedAngle = Math.Round(_requestedRelativeAngle, 2),
            AzimuthAngle = _currentAzimuth,
            DAC = _dac
        }, _jsonOptions);

        await BroadcastTextAsync(payload);
    }

    private async Task SendStateToSocketAsync(WebSocket socket, CancellationToken token)
    {
        var payload = JsonSerializer.Serialize(new WebState
        {
            Command = "angle",
            CurrentAngle = Math.Round(_currentRelativeAngle, 2),
            RequestedAngle = Math.Round(_requestedRelativeAngle, 2),
            AzimuthAngle = _currentAzimuth,
            DAC = _dac
        }, _jsonOptions);

        byte[] bytes = Encoding.UTF8.GetBytes(payload);
        Log($"[WS][TX] initial state => {payload}");
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, token);
    }

    private void Log(string message)
    {
        _logger?.Invoke(message);
    }

    private async Task BroadcastTextAsync(string text)
    {
        WebSocket[] sockets;
        lock (_sync)
        {
            sockets = _clients.Where(c => c.State == WebSocketState.Open).ToArray();
        }

        byte[] bytes = Encoding.UTF8.GetBytes(text);
        foreach (var socket in sockets)
        {
            try
            {
                await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch
            {
            }
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
                if (socket.State == WebSocketState.Open)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Stopping", CancellationToken.None);
                }
            }
            catch
            {
            }
            finally
            {
                socket.Dispose();
            }
        }
    }

    private static IPAddress ResolveListenAddress(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            return IPAddress.Any;
        }

        ip = ip.Trim();
        if (ip is "0.0.0.0" or "127.0.0.1" or "localhost" or "::1")
        {
            return IPAddress.Any;
        }

        if (IPAddress.TryParse(ip, out IPAddress? parsedAddress))
        {
            return parsedAddress;
        }

        IPAddress? resolved = Dns.GetHostAddresses(ip)
            .FirstOrDefault(address => address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

        return resolved ?? IPAddress.Any;
    }

    private static int Normalize360(int value)
    {
        int normalized = value % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }

    private static double Normalize180(double value)
    {
        double normalized = value % 360.0;
        if (normalized > 180.0)
        {
            normalized -= 360.0;
        }

        if (normalized < -180.0)
        {
            normalized += 360.0;
        }

        return normalized;
    }

    private static async Task<string> ReadBodyAsync(HttpRequest request)
    {
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private static async Task SendJsonAsync(HttpResponse response, string json)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        response.ContentType = "application/json; charset=utf-8";
        response.ContentLength = bytes.Length;
        await response.Body.WriteAsync(bytes);
    }

    public void Dispose()
    {
        Stop();
    }
}

public sealed class WebState
{
    [JsonPropertyName("command")]
    public string Command { get; set; } = "angle";

    [JsonPropertyName("angle")]
    public double CurrentAngle { get; set; }

    [JsonPropertyName("req_angle")]
    public double RequestedAngle { get; set; }

    [JsonPropertyName("az_angle")]
    public double AzimuthAngle { get; set; }

    [JsonPropertyName("dac")]
    public int DAC { get; set; }
}

public sealed class WebSnapshot
{
    [JsonPropertyName("sweep")]
    public int Sweep { get; set; }

    [JsonPropertyName("max_sweep")]
    public double MaxSweep { get; set; }

    [JsonPropertyName("azimuth")]
    public int Azimuth { get; set; }

    [JsonPropertyName("lat")]
    public double Latitude { get; set; }

    [JsonPropertyName("lng")]
    public double Longitude { get; set; }

    [JsonPropertyName("ip")]
    public string IP { get; set; } = "127.0.0.1";

    [JsonPropertyName("port")]
    public int Port { get; set; } = 80;

    [JsonPropertyName("sn")]
    public string SerialNumber { get; set; } = "SZR-F0F0F0F0F0F0";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "ROTATOR-SZ-1M; HW:1.0; FW:1.11";

    [JsonPropertyName("reverse")]
    public int Reverse { get; set; } = 1;

    [JsonPropertyName("c_n_180")]
    public int CalibrationNeg180 { get; set; } = 4150;

    [JsonPropertyName("c_n_90")]
    public int CalibrationNeg90 { get; set; } = 3135;

    [JsonPropertyName("c_0")]
    public int Calibration0 { get; set; } = 2107;

    [JsonPropertyName("c_90")]
    public int Calibration90 { get; set; } = 1066;

    [JsonPropertyName("c_180")]
    public int Calibration180 { get; set; } = 0;
}
