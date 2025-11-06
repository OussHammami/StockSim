FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV LANG=en_US.UTF-8
ENV LC_ALL=en_US.UTF-8

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Directory.Packages.props ./
COPY src/StockSim.Domain/*.csproj src/StockSim.Domain/
COPY src/StockSim.Application/*.csproj src/StockSim.Application/
COPY src/StockSim.Infrastructure/*.csproj src/StockSim.Infrastructure/
COPY src/StockSim.Contracts/*.csproj      src/StockSim.Contracts/
COPY src/StockSim.Web/*.csproj src/StockSim.Web/
RUN dotnet restore src/StockSim.Web/StockSim.Web.csproj

# copy sources explicitly
COPY src/StockSim.Domain/         src/StockSim.Domain/
COPY src/StockSim.Application/    src/StockSim.Application/
COPY src/StockSim.Infrastructure/ src/StockSim.Infrastructure/
COPY src/StockSim.Contracts/      src/StockSim.Contracts/
COPY src/StockSim.Web/            src/StockSim.Web/
ARG BUILD_CONFIGURATION=Debug
# RUN dotnet publish src/StockSim.Web/StockSim.Web.csproj -c Release -o /out
RUN dotnet publish src/StockSim.Web/StockSim.Web.csproj -c $BUILD_CONFIGURATION -o /out /p:DebugType=portable /p:DebugSymbols=true


FROM base
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet","StockSim.Web.dll"]
