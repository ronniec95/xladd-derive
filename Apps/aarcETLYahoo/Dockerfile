FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 5001,4000

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY Apps/aarcYahooFinETL/aarcYahooFinETL.csproj Apps/aarcYahooFinETL/
COPY Libs/AARC.Model/AARC.Model.csproj Libs/AARC.Model/
COPY Libs/AARC.DateLib/AARC.DateLib.csproj Libs/AARC.DateLib/
COPY Libs/AARC.Utilities/AARC.Utilities.csproj Libs/AARC.Utilities/
COPY Libs/AARC.RDS/AARC.RDS.csproj Libs/AARC.RDS/
RUN dotnet restore Apps/aarcYahooFinETL/aarcYahooFinETL.csproj
COPY . .
WORKDIR /src/Apps/aarcYahooFinETL
RUN dotnet build aarcYahooFinETL.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish aarcYahooFinETL.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "aarcYahooFinETL.dll"]
