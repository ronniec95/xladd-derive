FROM mcr.microsoft.com/dotnet/core/runtime:2.2-stretch-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ../MeshHostedService/MeshHostedService.csproj ../MeshHostedService/
COPY ../AARC.Mesh/AARC.Mesh.csproj ../AARC.Mesh/
COPY AARC.Graph.Test/AARC.Graph.Test.csproj AARC.Graph.Test/
RUN dotnet restore "../MeshHostedService/MeshHostedService.csproj"
COPY . .
WORKDIR "/src/../MeshHostedService"
RUN dotnet build "MeshHostedService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MeshHostedService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MeshHostedService.dll"]
