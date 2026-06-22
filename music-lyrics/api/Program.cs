using MusicLyrics.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddSingleton<MusicService>();

var app = builder.Build();

app.UseCors();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/search", async (SearchRequest request, MusicService service, CancellationToken ct) =>
{
    return await Handle(async () => await service.SearchAsync(request, ct));
});

app.MapPost("/api/tracks", async (ContainerTracksRequest request, MusicService service, CancellationToken ct) =>
{
    return await Handle(async () => await service.GetContainerTracksAsync(request, ct));
});

app.MapPost("/api/lyrics", async (LyricsRequest request, MusicService service, CancellationToken ct) =>
{
    return await Handle(async () => await service.GetLyricsAsync(request, ct));
});

app.MapPost("/api/song-link", async (SongLinkRequest request, MusicService service, CancellationToken ct) =>
{
    return await Handle(async () => await service.GetSongLinkAsync(request, ct));
});

app.MapPost("/api/convert", (ConvertLyricsRequest request) =>
{
    return Handle(() => Task.FromResult(LyricsFormatter.Convert(request)));
});

app.Run();

static async Task<IResult> Handle<T>(Func<Task<T>> action)
{
    try
    {
        return Results.Ok(await action());
    }
    catch (ApiException ex)
    {
        return Results.BadRequest(new ErrorResponse(ex.Message));
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
}
