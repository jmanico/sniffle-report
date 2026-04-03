FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS restore
WORKDIR /src

COPY SniffleReport.Api/SniffleReport.Api.csproj SniffleReport.Api/
RUN dotnet restore SniffleReport.Api/SniffleReport.Api.csproj

FROM restore AS build
COPY . .
RUN dotnet build SniffleReport.Api/SniffleReport.Api.csproj -c Release --no-restore

FROM build AS publish
RUN dotnet publish SniffleReport.Api/SniffleReport.Api.csproj -c Release --no-build -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "SniffleReport.Api.dll"]
