FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

# Install a minimal HTTP client so the container healthcheck can curl the local /healthz endpoint.
# Use apt-get because the official aspnet image is Debian-based.
RUN apt-get update && apt-get install -y --no-install-recommends curl ca-certificates && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# restore with only csproj files
COPY src/StockSim.Domain/*.csproj src/StockSim.Domain/
COPY src/StockSim.MarketFeed/*.csproj src/StockSim.MarketFeed/
RUN dotnet restore src/StockSim.MarketFeed/StockSim.MarketFeed.csproj

# now copy source folders explicitly
COPY src/StockSim.Domain/       src/StockSim.Domain/
COPY src/StockSim.MarketFeed/   src/StockSim.MarketFeed/

# sanity check
RUN ls -la src/StockSim.MarketFeed && test -f src/StockSim.MarketFeed/Program.cs

ARG BUILD_CONFIGURATION=Release
RUN dotnet publish src/StockSim.MarketFeed/StockSim.MarketFeed.csproj -c $BUILD_CONFIGURATION -o /out /p:DebugType=portable /p:DebugSymbols=true

FROM base AS final
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet","StockSim.MarketFeed.dll"]