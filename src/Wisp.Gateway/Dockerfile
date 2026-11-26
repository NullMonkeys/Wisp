# Build context: the repository root.
#   docker build -f src/Wisp.Gateway/Dockerfile .

# The SDK runs on the build machine's own architecture and cross-compiles for the target, so building an arm64 image
# on an x64 runner (or the other way round) needs no emulation.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0@sha256:8c6edfb26c85153f236d26efebd9ad349aec403583f343f201ee67838b23658d AS build
ARG TARGETARCH
WORKDIR /src

# Restore first, from the project files alone, so the package layer is cached until a project or package changes.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Wisp.Gateway/Wisp.Gateway.csproj src/Wisp.Gateway/
COPY src/libs/Wisp.Discord/Wisp.Discord.csproj src/libs/Wisp.Discord/
COPY src/libs/Wisp.Persistence/Wisp.Persistence.csproj src/libs/Wisp.Persistence/
COPY aspire/Wisp.ServiceDefaults/Wisp.ServiceDefaults.csproj aspire/Wisp.ServiceDefaults/
RUN dotnet restore src/Wisp.Gateway/Wisp.Gateway.csproj --arch $TARGETARCH

COPY src/Wisp.Gateway/ src/Wisp.Gateway/
COPY src/libs/ src/libs/
COPY aspire/Wisp.ServiceDefaults/ aspire/Wisp.ServiceDefaults/
RUN dotnet publish src/Wisp.Gateway/Wisp.Gateway.csproj \
    --configuration Release \
    --arch $TARGETARCH \
    --no-restore \
    --output /app \
    -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:aac50ceef10391e78a899e3d2ccd00bbafa2dcb56ccf4586cbccf00edb84fc21 AS final
WORKDIR /app
USER $APP_UID
# Health checks: /healthz (liveness) and /readyz (readiness: database + every Discord shard).
EXPOSE 8080
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Wisp.Gateway.dll"]
