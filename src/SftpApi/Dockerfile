FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Heracles.sln .
COPY src/Shared/Shared.csproj src/Shared/
COPY src/SftpApi/SftpApi.csproj src/SftpApi/
RUN dotnet restore src/SftpApi/SftpApi.csproj
COPY src/Shared/ src/Shared/
COPY src/SftpApi/ src/SftpApi/
RUN dotnet publish src/SftpApi/SftpApi.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "SftpApi.dll"]
