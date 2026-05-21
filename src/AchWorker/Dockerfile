FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Heracles.sln .
COPY src/Shared/Shared.csproj src/Shared/
COPY src/AchWorker/AchWorker.csproj src/AchWorker/
RUN dotnet restore src/AchWorker/AchWorker.csproj
COPY src/Shared/ src/Shared/
COPY src/AchWorker/ src/AchWorker/
RUN dotnet publish src/AchWorker/AchWorker.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "AchWorker.dll"]
