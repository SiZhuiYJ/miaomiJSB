using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace api.Services;

public sealed class LiveSignalingHub
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ConcurrentDictionary<string, LiveClient> _viewers = new();
    private readonly object _publisherGate = new();
    private LiveClient? _publisher;

    public async Task HandleAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var client = new LiveClient(socket);

        try
        {
            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var text = await ReceiveTextAsync(socket, cancellationToken);
                if (text is null)
                {
                    break;
                }

                using var message = ParseJson(text);
                if (message is null)
                {
                    continue;
                }

                await HandleMessageAsync(client, message.RootElement, cancellationToken);
            }
        }
        finally
        {
            await RemoveClientAsync(client);
            client.Dispose();
        }
    }

    private async Task HandleMessageAsync(LiveClient client, JsonElement message, CancellationToken cancellationToken)
    {
        var type = GetString(message, "type");
        if (string.IsNullOrWhiteSpace(type))
        {
            return;
        }

        switch (type)
        {
            case "join":
                await JoinAsync(client, message, cancellationToken);
                break;

            case "offer":
                await SendToViewerAsync(message, cancellationToken);
                break;

            case "answer":
                await SendToPublisherAsync(message, cancellationToken);
                break;

            case "candidate":
                await RelayCandidateAsync(message, cancellationToken);
                break;

            case "danmaku":
                await BroadcastDanmakuAsync(message, cancellationToken);
                break;
        }
    }

    private async Task JoinAsync(LiveClient client, JsonElement message, CancellationToken cancellationToken)
    {
        var role = GetString(message, "role");

        if (role == "publisher")
        {
            await RegisterPublisherAsync(client, cancellationToken);
            return;
        }

        var viewerId = GetString(message, "id");
        if (string.IsNullOrWhiteSpace(viewerId))
        {
            viewerId = $"v_{Guid.NewGuid():N}"[..10];
        }

        await RegisterViewerAsync(client, viewerId, cancellationToken);
    }

    private async Task RegisterPublisherAsync(LiveClient client, CancellationToken cancellationToken)
    {
        LiveClient? oldPublisher;
        lock (_publisherGate)
        {
            oldPublisher = _publisher;
            _publisher = client;
            client.Role = LiveRole.Publisher;
        }

        if (oldPublisher is not null && !ReferenceEquals(oldPublisher, client))
        {
            await oldPublisher.CloseAsync();
        }

        foreach (var viewer in _viewers)
        {
            await viewer.Value.SendAsync(new { type = "publisher-ready" }, cancellationToken);
            await client.SendAsync(new { type = "viewer-joined", viewerId = viewer.Key }, cancellationToken);
        }
    }

    private async Task RegisterViewerAsync(LiveClient client, string viewerId, CancellationToken cancellationToken)
    {
        client.Role = LiveRole.Viewer;
        client.ViewerId = viewerId;

        if (_viewers.TryGetValue(viewerId, out var oldViewer) && !ReferenceEquals(oldViewer, client))
        {
            await oldViewer.CloseAsync();
        }

        _viewers[viewerId] = client;

        var publisher = GetPublisher();
        if (publisher is null)
        {
            await client.SendAsync(new { type = "waiting" }, cancellationToken);
            return;
        }

        await client.SendAsync(new { type = "publisher-ready" }, cancellationToken);
        await publisher.SendAsync(new { type = "viewer-joined", viewerId }, cancellationToken);
    }

    private async Task SendToViewerAsync(JsonElement message, CancellationToken cancellationToken)
    {
        var viewerId = GetString(message, "viewerId");
        if (!string.IsNullOrWhiteSpace(viewerId) && _viewers.TryGetValue(viewerId, out var viewer))
        {
            await viewer.SendJsonAsync(message.GetRawText(), cancellationToken);
        }
    }

    private async Task SendToPublisherAsync(JsonElement message, CancellationToken cancellationToken)
    {
        var publisher = GetPublisher();
        if (publisher is not null)
        {
            await publisher.SendJsonAsync(message.GetRawText(), cancellationToken);
        }
    }

    private async Task RelayCandidateAsync(JsonElement message, CancellationToken cancellationToken)
    {
        var target = GetString(message, "target");
        if (target == "publisher")
        {
            await SendToPublisherAsync(message, cancellationToken);
            return;
        }

        if (target == "viewer")
        {
            await SendToViewerAsync(message, cancellationToken);
        }
    }

    private async Task BroadcastDanmakuAsync(JsonElement message, CancellationToken cancellationToken)
    {
        var text = GetString(message, "text");
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var data = new
        {
            type = "danmaku",
            text = text.Trim()[..Math.Min(text.Trim().Length, 50)],
            color = GetString(message, "color") ?? "#ffffff"
        };

        foreach (var viewer in _viewers.Values)
        {
            await viewer.SendAsync(data, cancellationToken);
        }
    }

    private async Task RemoveClientAsync(LiveClient client)
    {
        if (client.Role == LiveRole.Publisher)
        {
            var removed = false;
            lock (_publisherGate)
            {
                if (ReferenceEquals(_publisher, client))
                {
                    _publisher = null;
                    removed = true;
                }
            }

            if (removed)
            {
                foreach (var viewer in _viewers.Values)
                {
                    await viewer.SendAsync(new { type = "publisher-left" }, CancellationToken.None);
                }
            }
        }

        if (client.Role == LiveRole.Viewer && client.ViewerId is not null)
        {
            if (_viewers.TryGetValue(client.ViewerId, out var viewer) && ReferenceEquals(viewer, client))
            {
                _viewers.TryRemove(client.ViewerId, out _);

                var publisher = GetPublisher();
                if (publisher is not null)
                {
                    await publisher.SendAsync(new { type = "viewer-left", viewerId = client.ViewerId }, CancellationToken.None);
                }
            }
        }
    }

    private LiveClient? GetPublisher()
    {
        lock (_publisherGate)
        {
            return _publisher;
        }
    }

    private static JsonDocument? ParseJson(string text)
    {
        try
        {
            return JsonDocument.Parse(text);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? GetString(JsonElement message, string propertyName)
    {
        if (!message.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return property.GetString();
    }

    private static async Task<string?> ReceiveTextAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        using var stream = new MemoryStream();

        while (true)
        {
            var result = await socket.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            stream.Write(buffer, 0, result.Count);

            if (result.EndOfMessage)
            {
                break;
            }
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private sealed class LiveClient : IDisposable
    {
        private readonly SemaphoreSlim _sendLock = new(1, 1);

        public LiveClient(WebSocket socket)
        {
            Socket = socket;
        }

        public WebSocket Socket { get; }
        public LiveRole Role { get; set; } = LiveRole.Unknown;
        public string? ViewerId { get; set; }

        public Task SendAsync(object payload, CancellationToken cancellationToken)
        {
            return SendJsonAsync(JsonSerializer.Serialize(payload, JsonOptions), cancellationToken);
        }

        public async Task SendJsonAsync(string json, CancellationToken cancellationToken)
        {
            if (Socket.State != WebSocketState.Open)
            {
                return;
            }

            await _sendLock.WaitAsync(cancellationToken);
            try
            {
                if (Socket.State == WebSocketState.Open)
                {
                    var bytes = Encoding.UTF8.GetBytes(json);
                    await Socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
                }
            }
            catch (WebSocketException)
            {
                // The receive loop will clean up closed clients.
            }
            catch (OperationCanceledException)
            {
                // Shutdown in progress.
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public async Task CloseAsync()
        {
            if (Socket.State is not WebSocketState.Open and not WebSocketState.CloseReceived)
            {
                return;
            }

            try
            {
                await Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", CancellationToken.None);
            }
            catch (WebSocketException)
            {
            }
        }

        public void Dispose()
        {
            _sendLock.Dispose();
        }
    }

    private enum LiveRole
    {
        Unknown,
        Publisher,
        Viewer
    }
}
