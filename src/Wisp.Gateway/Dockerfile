FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:11.0@sha256:2794448a07118a6eaf00f29ab5d2bf44928517ce5ff86505a63a69230374f599 AS build
ARG TARGETARCH
WORKDIR /src

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

FROM mcr.microsoft.com/dotnet/aspnet:11.0@sha256:ca396ece3f2a2e62c7a03f1d7d710b8bc7e8c9dea5597d091178d9f5bfca13f0 AS final

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
USER $APP_UID
EXPOSE 8080
COPY --from=build /app .

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -fsS -o /dev/null http://localhost:8080/readyz || exit 1

ENTRYPOINT ["dotnet", "Wisp.Gateway.dll"]
