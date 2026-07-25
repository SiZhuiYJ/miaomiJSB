using api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<LiveSignalingHub>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("Frontend");
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    service = "live-video-api"
}));

app.Map("/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("WebSocket requests only.");
        return;
    }

    var hub = context.RequestServices.GetRequiredService<LiveSignalingHub>();
    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    await hub.HandleAsync(socket, context.RequestAborted);
});

app.Run();
