FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ../../../../Momentum.sln ./
COPY ../../streamer/Streamer.Domain/ Streamer.Domain/
COPY ../../streamer/Streamer.Application/ Streamer.Application/
COPY ../../streamer/Streamer.Infrastructure/ Streamer.Infrastructure/
COPY ../../streamer/Streamer.Api/ Streamer.Api/
RUN dotnet restore Streamer.Api/Streamer.Api.csproj || true
RUN dotnet publish Streamer.Api/Streamer.Api.csproj -c Release -o /app/publish || true

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Streamer.Api.dll"]
