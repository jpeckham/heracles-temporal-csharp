FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Heracles.sln .
COPY src/Shared/Shared.csproj src/Shared/
COPY src/PaymentApi/PaymentApi.csproj src/PaymentApi/
RUN dotnet restore src/PaymentApi/PaymentApi.csproj
COPY src/Shared/ src/Shared/
COPY src/PaymentApi/ src/PaymentApi/
RUN dotnet publish src/PaymentApi/PaymentApi.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "PaymentApi.dll"]
