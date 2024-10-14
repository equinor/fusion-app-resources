#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Fusion.Summary.Api/Fusion.Summary.Api.csproj", "Fusion.Summary.Api/"]
RUN dotnet restore "./Fusion.Summary.Api/Fusion.Summary.Api.csproj"
COPY . .
WORKDIR "/src/Fusion.Summary.Api"
RUN dotnet build "./Fusion.Summary.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish

## Set an variable to indicate build 
ENV IS_DOCKER_BUILD=true

ARG BUILD_CONFIGURATION=Release
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet publish "./Fusion.Summary.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish
RUN dotnet ef migrations script --idempotent --output "/migrations/migration.sql" --configuration Release --no-build

FROM scratch as export
COPY --from=publish /migrations/migration.sql db-migration.sql

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=publish /migrations .
ENTRYPOINT ["dotnet", "Fusion.Summary.Api.dll"]