# ── Build ─────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore first, from the project files only, so the layer is cached until a dependency changes.
COPY OverLoad.sln Directory.Build.props .editorconfig ./
COPY OverLoad.API/OverLoad.API.csproj OverLoad.API/
COPY OverLoad.Services/OverLoad.Services.csproj OverLoad.Services/
COPY OverLoad.Repositories/OverLoad.Repositories.csproj OverLoad.Repositories/
COPY OverLoad.Domain/OverLoad.Domain.csproj OverLoad.Domain/
RUN dotnet restore OverLoad.API/OverLoad.API.csproj

COPY OverLoad.API/ OverLoad.API/
COPY OverLoad.Services/ OverLoad.Services/
COPY OverLoad.Repositories/ OverLoad.Repositories/
COPY OverLoad.Domain/ OverLoad.Domain/
RUN dotnet publish OverLoad.API/OverLoad.API.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ── Runtime ───────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
# HTTPS is terminated by the hosting platform / reverse proxy in front of the container.
ENV HttpsRedirection__Enabled=false
EXPOSE 8080

COPY --from=build /app/publish .

# The aspnet image ships a non-root "app" user.
USER app

ENTRYPOINT ["dotnet", "OverLoad.API.dll"]
