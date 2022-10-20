#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0-bullseye-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim AS build
WORKDIR /src
COPY ["api/Fusion.Resources.Api/Fusion.Resources.Api.csproj", "api/Fusion.Resources.Api/"]
COPY ["api/Fusion.Resources.Database/Fusion.Resources.Database.csproj", "api/Fusion.Resources.Database/"]
COPY ["api/Fusion.Resources.Domain/Fusion.Resources.Domain.csproj", "api/Fusion.Resources.Domain/"]
COPY ["api/Fusion.Resources.Logic/Fusion.Resources.Logic.csproj", "api/Fusion.Resources.Logic/"]
COPY ["integration/Fusion.Resources.Integration.Models/Fusion.Resources.Integration.Models.csproj", "integration/Fusion.Resources.Integration.Models/"]
COPY ["nuget.config", "./"]
RUN dotnet restore "api/Fusion.Resources.Api/Fusion.Resources.Api.csproj"
COPY . .
WORKDIR "/src/api/Fusion.Resources.Api"
RUN dotnet build "Fusion.Resources.Api.csproj" -c Release -o /app/build

FROM build AS publish

## Set an variable to indicate build 
ENV IS_DOCKER_BUILD=true

RUN dotnet tool restore
RUN dotnet publish "Fusion.Resources.Api.csproj" -c Release -o /app/publish

WORKDIR "/src/api/Fusion.Resources.Database"
RUN dotnet ef migrations script --idempotent --output "/migrations/migration.sql" --configuration Release --no-build

FROM scratch as export
COPY --from=publish /migrations/migration.sql db-migration.sql

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=publish /migrations .
ENTRYPOINT ["dotnet", "Fusion.Resources.Api.dll"]