# syntax=docker/dockerfile:1

# ---- Build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY API.csproj ./
RUN dotnet restore API.csproj

COPY . .
RUN dotnet publish API.csproj -c Release -o /app/publish --no-restore

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# All persistent state lives in Firestore (external), so this container
# itself is stateless — no volume/disk needed.

# The base image ships a built-in non-root "app" user for exactly this.
USER app

# Render injects its own PORT at runtime — 10000 by default, or whatever's
# set in the service's dashboard — and expects the app to bind to it;
# per Render's docs, EXPOSE is informational only and isn't used for
# routing. This ENV/EXPOSE pair is only the fallback for a plain local
# `docker run` (see docker-compose.yml, which maps it to host port 5260).
ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["/bin/sh", "-c", "exec dotnet API.dll --urls http://0.0.0.0:${PORT}"]
