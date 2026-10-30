using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// PURPOSE: Optional Alpaca WebSocket minute-bar ingestion with reconnects and late-bar correction persistence.
/// WHEN CALLED: Enabled by MarketData:Alpaca:StreamEnabled=true and a configured StreamSymbols list.
/// SAFETY: Disabled by default; no credentials are logged; bars are stored as observations, never treated as orders.
/// </summary>
public sealed class AlpacaMinuteBarStreamWorker(
    IConfiguration configuration,
    IMarketBarObservationStore store,
    ILogger<AlpacaMinuteBarStreamWorker> logger) : BackgroundService
{
    private readonly string? _key = configuration["MarketData:Alpaca:ApiKey"];
    private readonly string? _secret = configuration["MarketData:Alpaca:ApiSecret"];
    private readonly string _feed = (configuration["MarketData:Alpaca:Feed"] ?? "iex").Trim().ToLowerInvariant();
    private readonly string[] _symbols = (configuration["MarketData:Alpaca:StreamSymbols"]
        ?? configuration["MarketData:Collector:Symbols"] ?? string.Empty)
        .Split(new[] { ',', ';', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(x => x.ToUpperInvariant()).Distinct(StringComparer.OrdinalIgnoreCase)
        .Where(x => x.Length is >= 1 and <= 8).ToArray();
    private readonly bool _enabled = configuration.GetValue<bool>("MarketData:Alpaca:StreamEnabled");
    private long _lastMessageUnixMs;
    private long _lastBarUnixMs;
    private long _persistedBars;
    private int _connected;
    private string _state = "DISABLED";
    private string? _lastError;

    public bool IsSubscribed => Volatile.Read(ref _connected) == 1 && string.Equals(_state, "SUBSCRIBED", StringComparison.Ordinal);
    public string Feed => _feed;

    public object Snapshot() => new
    {
        enabled = _enabled,
        configured = !string.IsNullOrWhiteSpace(_key) && !string.IsNullOrWhiteSpace(_secret),
        connected = Volatile.Read(ref _connected) == 1,
        state = _state,
        feed = _feed,
        symbols = _symbols,
        lastMessageUtc = Timestamp(Interlocked.Read(ref _lastMessageUnixMs)),
        lastBarUtc = Timestamp(Interlocked.Read(ref _lastBarUnixMs)),
        persistedBars = Interlocked.Read(ref _persistedBars),
        lastError = _lastError,
        note = "Minute bars and late updatedBars only; this is not a trade/quote tick stream."
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            _state = "DISABLED";
            return;
        }
        if (string.IsNullOrWhiteSpace(_key) || string.IsNullOrWhiteSpace(_secret) || _symbols.Length == 0)
        {
            _state = "MISCONFIGURED";
            _lastError = "Configure Alpaca credentials and MarketData:Alpaca:StreamSymbols (or collector symbols).";
            logger.LogWarning("Alpaca minute-bar stream is enabled but credentials or symbols are missing.");
            return;
        }
        if (_feed is not ("iex" or "sip"))
        {
            _state = "MISCONFIGURED";
            _lastError = "WebSocket feed must be iex or sip.";
            return;
        }

        var delay = TimeSpan.FromSeconds(2);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _state = "CONNECTING";
                using var socket = new ClientWebSocket();
                socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
                await socket.ConnectAsync(new Uri($"wss://stream.data.alpaca.markets/v2/{_feed}"), stoppingToken);
                _state = "AUTHENTICATING";
                await SendAsync(socket, new { action = "auth", key = _key, secret = _secret }, stoppingToken);
                await ReceiveUntilAuthenticatedAsync(socket, stoppingToken);
                await SendAsync(socket, new { action = "subscribe", bars = _symbols }, stoppingToken);
                await ReceiveUntilSubscribedAsync(socket, stoppingToken);
                Volatile.Write(ref _connected, 1);
                _state = "SUBSCRIBED";
                delay = TimeSpan.FromSeconds(2);
                await ReceiveLoopAsync(socket, stoppingToken);
                if (!stoppingToken.IsCancellationRequested)
                    throw new WebSocketException("Alpaca WebSocket closed the connection.");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) when (ex is WebSocketException or HttpRequestException or IOException or JsonException or InvalidOperationException || (ex is OperationCanceledException && !stoppingToken.IsCancellationRequested))
            {
                _lastError = ex.GetType().Name + ": " + ex.Message;
                _state = "RECONNECTING";
                logger.LogWarning(ex, "Alpaca minute-bar stream disconnected; reconnecting.");
            }
            finally { Volatile.Write(ref _connected, 0); }

            try { await Task.Delay(delay, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            delay = TimeSpan.FromSeconds(Math.Min(30, delay.TotalSeconds * 2));
        }
        _state = "STOPPED";
    }

    private async Task ReceiveUntilAuthenticatedAsync(ClientWebSocket socket, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        while (true)
        {
            using var document = await ReceiveDocumentAsync(socket, timeout.Token);
            Interlocked.Exchange(ref _lastMessageUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            foreach (var item in document.RootElement.EnumerateArray())
            {
                var type = GetString(item, "T");
                if (type == "success" && GetString(item, "msg") == "authenticated") return;
                if (type == "error") throw new InvalidOperationException("Alpaca stream authentication error: " + GetString(item, "msg"));
            }
        }
    }

    private async Task ReceiveUntilSubscribedAsync(ClientWebSocket socket, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        while (true)
        {
            using var document = await ReceiveDocumentAsync(socket, timeout.Token);
            Interlocked.Exchange(ref _lastMessageUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            foreach (var item in document.RootElement.EnumerateArray())
            {
                var type = GetString(item, "T");
                if (type == "error") throw new InvalidOperationException("Alpaca stream subscription error: " + GetString(item, "msg"));
                if (type == "subscription")
                {
                    var acknowledged = item.TryGetProperty("bars", out var bars) && bars.ValueKind == JsonValueKind.Array
                        ? bars.EnumerateArray().Select(x => x.GetString()).Where(x => x is not null).ToArray()
                        : Array.Empty<string?>();
                    var missing = _symbols.Except(acknowledged!, StringComparer.OrdinalIgnoreCase).ToArray();
                    if (missing.Length > 0)
                        throw new InvalidOperationException("Alpaca did not acknowledge minute-bar subscriptions for: " + string.Join(", ", missing));
                    return;
                }
            }
        }
    }

    private async Task ReceiveLoopAsync(ClientWebSocket socket, CancellationToken ct)
    {
        while (socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            using var document = await ReceiveDocumentAsync(socket, ct);
            Interlocked.Exchange(ref _lastMessageUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            var observations = new List<MarketBar>();
            foreach (var item in document.RootElement.EnumerateArray())
            {
                var type = GetString(item, "T");
                if (type == "error")
                    throw new InvalidOperationException("Alpaca stream error: " + GetString(item, "msg"));
                if (type is not ("b" or "u")) continue;
                var symbol = GetString(item, "S");
                if (string.IsNullOrWhiteSpace(symbol) || !DateTimeOffset.TryParse(GetString(item, "t"), out var timestamp))
                    continue;
                observations.Add(new MarketBar(
                    symbol, timestamp.ToUniversalTime(),
                    GetDouble(item, "o"), GetDouble(item, "h"), GetDouble(item, "l"), GetDouble(item, "c"),
                    GetLong(item, "v"), "Alpaca", _feed, "1m", "WEBSOCKET"));
            }
            if (observations.Count == 0) continue;
            await store.AppendAsync(observations, ct);
            Interlocked.Add(ref _persistedBars, observations.Count);
            Interlocked.Exchange(ref _lastBarUnixMs, observations.Max(x => x.Timestamp).ToUnixTimeMilliseconds());
        }
    }

    private static async Task SendAsync(ClientWebSocket socket, object message, CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
    }

    private static async Task<JsonDocument> ReceiveDocumentAsync(ClientWebSocket socket, CancellationToken ct)
    {
        var buffer = new byte[16 * 1024];
        using var output = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
            if (result.MessageType == WebSocketMessageType.Close)
                throw new WebSocketException("Alpaca stream sent a close frame.");
            output.Write(buffer, 0, result.Count);
            if (output.Length > 1024 * 1024)
                throw new InvalidOperationException("Alpaca stream message exceeded the 1 MiB safety limit.");
        } while (!result.EndOfMessage);
        return JsonDocument.Parse(output.ToArray());
    }

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static double GetDouble(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.TryGetDouble(out var result) ? result : 0d;
    private static long GetLong(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.TryGetInt64(out var result) ? result : 0L;
    private static DateTimeOffset? Timestamp(long unixMs) =>
        unixMs == 0 ? null : DateTimeOffset.FromUnixTimeMilliseconds(unixMs);
}
