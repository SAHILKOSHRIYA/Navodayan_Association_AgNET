# syntax=docker/dockerfile:1
# ── Single-image build: Angular SPA served by the ASP.NET Core API (one origin) ──

# 1. Build the Angular client
FROM node:22-alpine AS client
WORKDIR /client
COPY client/package*.json ./
RUN npm ci
COPY client/ ./
RUN npm run build

# 2. Build & publish the .NET API
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS server
WORKDIR /src
COPY server/NAU.sln ./
COPY server/src/ ./src/
COPY server/tests/ ./tests/
RUN dotnet restore src/NAU.Api/NAU.Api.csproj
RUN dotnet publish src/NAU.Api/NAU.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# 3. Runtime image: API + Angular static files in wwwroot
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=server /app/publish ./
COPY --from=client /client/dist/client/browser ./wwwroot
# Render (and most PaaS) inject PORT at runtime; bind Kestrel to it, default 8080.
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet NAU.Api.dll"]
