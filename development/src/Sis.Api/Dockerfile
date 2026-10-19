# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY nuget.config Directory.Build.props Directory.Packages.props ./
COPY src/Sis.Contracts/Sis.Contracts.csproj src/Sis.Contracts/
COPY src/Sis.Api/Sis.Api.csproj src/Sis.Api/
RUN dotnet restore src/Sis.Api/Sis.Api.csproj

COPY src/Sis.Contracts/ src/Sis.Contracts/
COPY src/Sis.Api/ src/Sis.Api/
RUN dotnet publish src/Sis.Api/Sis.Api.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Sis.Api.dll"]
