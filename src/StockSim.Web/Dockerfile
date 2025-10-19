FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV LANG=en_US.UTF-8
ENV LC_ALL=en_US.UTF-8

# Ensure a minimal HTTP client exists for container-local healthchecks
RUN apt-get update && apt-get install -y --no-install-recommends curl ca-certificates && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/StockSim.Domain/*.csproj src/StockSim.Domain/
COPY src/StockSim.Web/*.csproj src/StockSim.Web/
RUN dotnet restore src/StockSim.Web/StockSim.Web.csproj

# copy sources explicitly
COPY src/StockSim.Domain/         src/StockSim.Domain/
COPY src/StockSim.Application/    src/StockSim.Application/
COPY src/StockSim.Infrastructure/ src/StockSim.Infrastructure/
COPY src/StockSim.Web/            src/StockSim.Web/
ARG BUILD_CONFIGURATION=Debug
RUN dotnet publish src/StockSim.Web/StockSim.Web.csproj -c $BUILD_CONFIGURATION -o /out /p:DebugType=portable /p:DebugSymbols=true

FROM base AS final
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet","StockSim.Web.dll"]