using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

string leaderboardFilePath = Path.Combine(AppContext.BaseDirectory, "leaderboard.json");

// Simple in-Memory leaderboard
List<PlayerScore> leaderboard = LeaderboardStorage.LoadLeaderboard(leaderboardFilePath);

// Get Leaderboard
app.MapGet("/leaderboard", () =>
{
    var sorted = leaderboard.OrderByDescending(s => s.Score).ToList();
    return Results.Json(sorted);
});

//Submit Score
app.MapPost("/leaderboard", async (HttpRequest request) =>
{
    var body = await JsonSerializer.DeserializeAsync<PlayerScore>(request.Body);

    if (body is null || string.IsNullOrEmpty(body.PlayerName))
    {
        return Results.BadRequest("PlayerName and Score are required");
    }

    // Check if player exists
    var existing = leaderboard.FirstOrDefault(p => p.PlayerName == body.PlayerName);
    if (existing != null)
    {
        if (body.Score > existing.Score)
        {
            existing.Score = body.Score;
        }
    }
    else
    {
        leaderboard.Add(body);
    }

    LeaderboardStorage.SaveLeaderboard(leaderboard, leaderboardFilePath);

    return Results.Ok(new { success = true, message = "Score submitted" });
});

app.Run();

public class PlayerScore
{
    public string PlayerName { get; set; }
    public float Score { get; set; }

    public PlayerScore()
    {
        PlayerName = string.Empty;
    }
}
public static class LeaderboardStorage
{
    public static List<PlayerScore> LoadLeaderboard(string filePath)
    {
        if (File.Exists(filePath))
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var loaded = JsonSerializer.Deserialize<List<PlayerScore>>(json);
                if (loaded != null)
                    return loaded;
            }
            catch
            {

            }
        }
        return new List<PlayerScore>();
    }

    public static void SaveLeaderboard(List<PlayerScore> leaderboard, string filePath)
    {
        var json = JsonSerializer.Serialize(leaderboard, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(filePath, json);
    }
}
