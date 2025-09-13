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
    var sorted = leaderboard.OrderByDescending(s => s.score).ToList();

    /*
    var result = new Dictionary<string, object>
    {
        ["Items"] = sorted.Select(p => new Dictionary<string, object>
        {
            ["playerName"] = p.playerName,
            ["score"] = p.score
        }).ToArray()
    };

    return Results.Json(result);
    */

    
    var unityFormnat = new
    {
        Items = sorted.Select(p => new
        {
            playerName = p.playerName,
            score = p.score
        }).ToArray()
    };

    var jsonResult = JsonSerializer.Serialize(unityFormnat);
    Console.WriteLine($"API sending JSON: {jsonResult}");

    return Results.Json(unityFormnat);
    
});

//Submit Score
app.MapPost("/leaderboard", async (HttpRequest request) =>
{
    var body = await JsonSerializer.DeserializeAsync<PlayerScore>(request.Body);

    if (body is null || string.IsNullOrEmpty(body.playerName))
    {
        return Results.BadRequest("PlayerName and Score are required");
    }

    // Check if player exists
    var existing = leaderboard.FirstOrDefault(p => p.playerName == body.playerName);
    if (existing != null)
    {
        if (body.score > existing.score)
        {
            existing.score = body.score;
        }
    }
    else
    {
        leaderboard.Add(body);
    }

    LeaderboardStorage.SaveLeaderboard(leaderboard, leaderboardFilePath);

    return Results.Ok(new { success = true, message = "Score submitted" });
});

//app.Run();
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");

public class PlayerScore
{
    public string playerName { get; set; }
    public float score { get; set; }

    public PlayerScore()
    {
        playerName = string.Empty;
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
