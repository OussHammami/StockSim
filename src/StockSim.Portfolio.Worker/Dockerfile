# build
ARG PROJECT
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
ARG PROJECT=src/StockSim.Portfolio.Worker/StockSim.Portfolio.Worker.csproj
RUN test -f "$PROJECT" || (echo "Project not found at: $PROJECT" && ls -R src | head -n 200 && exit 1)
RUN dotnet publish "$PROJECT" -c Release -o /app/publish

# run
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "StockSim.Portfolio.Worker.dll"]
