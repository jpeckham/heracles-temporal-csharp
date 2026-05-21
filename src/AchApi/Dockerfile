FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Heracles.sln .
COPY src/Shared/Shared.csproj src/Shared/
COPY src/AchApi/AchApi.csproj src/AchApi/
RUN dotnet restore src/AchApi/AchApi.csproj
COPY src/Shared/ src/Shared/
COPY src/AchApi/ src/AchApi/
RUN dotnet publish src/AchApi/AchApi.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "AchApi.dll"]
