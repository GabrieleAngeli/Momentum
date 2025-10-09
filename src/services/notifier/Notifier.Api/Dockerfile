FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ../../../../Momentum.sln ./
COPY ../../notifier/Notifier.Domain/ Notifier.Domain/
COPY ../../notifier/Notifier.Application/ Notifier.Application/
COPY ../../notifier/Notifier.Infrastructure/ Notifier.Infrastructure/
COPY ../../notifier/Notifier.Api/ Notifier.Api/
RUN dotnet restore Notifier.Api/Notifier.Api.csproj || true
RUN dotnet publish Notifier.Api/Notifier.Api.csproj -c Release -o /app/publish || true

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Notifier.Api.dll"]
