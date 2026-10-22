# Build context: repository root (see docker-compose.yml)

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# Restore first so the package layer is cached until a csproj or props file changes
COPY Directory.Build.props Directory.Packages.props ./
COPY src/TeamBoard.Domain/TeamBoard.Domain.csproj src/TeamBoard.Domain/
COPY src/TeamBoard.Application/TeamBoard.Application.csproj src/TeamBoard.Application/
COPY src/TeamBoard.Infrastructure/TeamBoard.Infrastructure.csproj src/TeamBoard.Infrastructure/
COPY src/TeamBoard.Api/TeamBoard.Api.csproj src/TeamBoard.Api/
RUN dotnet restore src/TeamBoard.Api/TeamBoard.Api.csproj

COPY src/ src/
RUN dotnet publish src/TeamBoard.Api/TeamBoard.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# The aspnet image listens on 8080 and runs as the non-root "app" user
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "TeamBoard.Api.dll"]
