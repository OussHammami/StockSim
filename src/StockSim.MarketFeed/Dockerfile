FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Copy central files first so CPM is in effect during restore
COPY Directory.Packages.props ./
# restore with only csproj files
COPY src/StockSim.Domain/*.csproj src/StockSim.Domain/
COPY src/StockSim.MarketFeed/*.csproj src/StockSim.MarketFeed/
RUN dotnet restore src/StockSim.MarketFeed/StockSim.MarketFeed.csproj

# now copy source folders explicitly (not "src/ .")
COPY src/StockSim.Domain/       src/StockSim.Domain/
COPY src/StockSim.MarketFeed/   src/StockSim.MarketFeed/

# sanity check
RUN ls -la src/StockSim.MarketFeed && test -f src/StockSim.MarketFeed/Program.cs
RUN dotnet publish src/StockSim.MarketFeed/StockSim.MarketFeed.csproj -c Release -o /out

FROM base
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet","StockSim.MarketFeed.dll"]
