#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY Apps/AARC.Service.Hosted/AARC.Service.Hosted.csproj Apps/AARC.Service.Hosted/
COPY Libs/AARC.Mesh/AARC.Mesh.csproj Libs/AARC.Mesh/
COPY Libs/AARC.Compression/AARC.Compression.csproj Libs/AARC.Compression/
COPY Libs/AARC.Utilities/AARC.Utilities.csproj Libs/AARC.Utilities/
COPY Libs/AARC.Mesh.TCP/AARC.Mesh.TCP.csproj Libs/AARC.Mesh.TCP/
COPY Libs/AARC.Mesh.Dataflow/AARC.Mesh.Dataflow.csproj Libs/AARC.Mesh.Dataflow/
COPY Libs/AARC.Model/AARC.Model.csproj Libs/AARC.Model/
COPY Libs/AARC.Repository.Interfaces/AARC.Repository.Interfaces.csproj Libs/AARC.Repository.Interfaces/
COPY Libs/AARC.RDS/AARC.RDS.csproj Libs/AARC.RDS/
COPY Libs/AARC.Repository.EF/AARC.Repository.EF.csproj Libs/AARC.Repository.EF/
COPY Libs/AARC.Mesh.AutoWireUp/AARC.Mesh.AutoWireUp.csproj Libs/AARC.Mesh.AutoWireUp/
COPY Libs/AARC.Mesh.Reflection/AARC.Mesh.Reflection.csproj Libs/AARC.Mesh.Reflection/
RUN dotnet restore "Apps/AARC.Service.Hosted/AARC.Service.Hosted.csproj"
COPY . .
WORKDIR "/src/Apps/AARC.Service.Hosted"
RUN dotnet build "AARC.Service.Hosted.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AARC.Service.Hosted.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 1024-65335
ENTRYPOINT ["dotnet", "AARC.Service.Hosted.dll"]
CMD [ds=tcp://192.168.0.202:9999 services=NasdaqTradableTickers,biggeststock]
