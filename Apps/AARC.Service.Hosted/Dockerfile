FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS build
WORKDIR /Apps

# copy csproj and restore as distinct layers
COPY ./Apps/AARC.Service.Hosted/*.csproj ./AARC.Service.Hosted/

WORKDIR /Libs
COPY ./Libs/AARC.Compression/*.csproj ./AARC.Compression/
COPY ./Libs/AARC.Mesh/*.csproj ./AARC.Mesh/
COPY ./Libs/AARC.Mesh.Dataflow/*.csproj ./AARC.Mesh.Dataflow/
COPY ./Libs/AARC.Mesh.TCP/*.csproj ./AARC.Mesh.TCP/
COPY ./Libs/AARC.Model/*.csproj ./AARC.Model/
COPY ./Libs/AARC.RDS/*.csproj ./AARC.RDS/
COPY ./Libs/AARC.Repository.EF/*.csproj ./AARC.Repository.EF/
COPY ./Libs/AARC.Repository.Interfaces/*.csproj ./AARC.Repository.Interfaces/
COPY ./Libs/AARC.Utilities/*.csproj ./AARC.Utilities/

WORKDIR /Apps/AARC.Service.Hosted
RUN dotnet restore

# copy and publish app and libraries
WORKDIR /Apps
COPY ./AARC.Service.Hosted/. ./AARC.Service.Hosted/

WORKDIR /Libs
COPY ./Libs/AARC.Compression/. ./Libs/AARC.Compression/
COPY ./Libs/AARC.Mesh/. ./Libs/AARC.Mes/
COPY ./Libs/AARC.Mesh.Dataflow/. ./Libs/AARC.Mesh.Dataflow/
COPY ./Libs/AARC.Mesh.TCP/. ./Libs/AARC.Mesh.TCP/
COPY ./Libs/AARC.Model/. ./Libs/AARC.Mode/
COPY ./Libs/AARC.RDS/. ./Libs/AARC.RDS/
COPY ./Libs/AARC.Repository.EF/. ./Libs/AARC.Repository.EF/
COPY ./Libs/AARC.Repository.Interfaces/. ./Libs/AARC.Repository.Interface/
COPY ./Libs/AARC.Utilities/. ./Libs/AARC.Utilitie/

WORKDIR /Apps
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/core/runtime:3.0 AS runtime
WORKDIR /Apps
COPY --from=build ./AARC.Service.Hosted/out ./
ENTRYPOINT ["dotnet", "AARC.Service.Hosted.dll"]