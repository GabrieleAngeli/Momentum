FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ../../../../Momentum.sln ./
COPY ../../identifier/Identifier.Domain/ Identifier.Domain/
COPY ../../identifier/Identifier.Application/ Identifier.Application/
COPY ../../identifier/Identifier.Infrastructure/ Identifier.Infrastructure/
COPY ../../identifier/Identifier.Api/ Identifier.Api/
RUN dotnet restore Identifier.Api/Identifier.Api.csproj || true
RUN dotnet publish Identifier.Api/Identifier.Api.csproj -c Release -o /app/publish || true

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Identifier.Api.dll"]
