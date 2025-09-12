# Use the official .NET 8 runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the official .NET 8 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj from the nested subfolder and restore
COPY LeaderboardAPI/LeaderboardAPI/*.csproj ./LeaderboardAPI/LeaderboardAPI/
RUN dotnet restore LeaderboardAPI/LeaderboardAPI/LeaderboardAPI.csproj

# Copy everything else and build
COPY LeaderboardAPI/LeaderboardAPI/ ./LeaderboardAPI/LeaderboardAPI/
RUN dotnet publish LeaderboardAPI/LeaderboardAPI/LeaderboardAPI.csproj -c Release -o /app/publish

# Final stage/image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "LeaderboardAPI.dll"]