# ADR-0017. Build from the repository root: docker build -f src/SupportFlow.Worker/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0.302 AS build
WORKDIR /source
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/ src/
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet publish src/SupportFlow.Worker/SupportFlow.Worker.csproj --configuration Release --output /app -p:UseAppHost=false

# Chiseled: no shell or package manager, runs as the non-root "app" user, serves the health probes on 8080.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "SupportFlow.Worker.dll"]
