# ── Stage 1: Build ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY AssetManager/AssetManager.csproj AssetManager/
RUN dotnet restore AssetManager/AssetManager.csproj

COPY AssetManager/ AssetManager/
RUN dotnet publish AssetManager/AssetManager.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ── Stage 2: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# curl is required by the HEALTHCHECK below
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
# Overridden by docker-compose.yml to point at the `db` service
ENV ConnectionStrings__DefaultConnection="Server=db;Port=3306;Database=mini_asm;User=root;Password=root;"

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "AssetManager.dll"]
