FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY Directory.Packages.props ./

COPY Momentum.sln ./
COPY src/services/streamer/Streamer.Domain/Streamer.Domain.csproj src/services/streamer/Streamer.Domain/
COPY src/services/streamer/Streamer.Application/Streamer.Application.csproj src/services/streamer/Streamer.Application/
COPY src/services/streamer/Streamer.Infrastructure/Streamer.Infrastructure.csproj src/services/streamer/Streamer.Infrastructure/
COPY src/services/streamer/Streamer.Api/Streamer.Api.csproj src/services/streamer/Streamer.Api/
RUN dotnet restore src/services/streamer/Streamer.Api/Streamer.Api.csproj

COPY . .
RUN dotnet publish src/services/streamer/Streamer.Api/Streamer.Api.csproj -c Release -o /app/out /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "Streamer.Api.dll"]
