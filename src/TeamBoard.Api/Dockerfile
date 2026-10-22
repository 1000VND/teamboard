# Build context: src/TeamBoard.Api (see docker-compose.yml)

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the package layer is cached until the csproj changes
COPY TeamBoard.Api.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# The aspnet image listens on 8080 and runs as the non-root "app" user
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "TeamBoard.Api.dll"]
