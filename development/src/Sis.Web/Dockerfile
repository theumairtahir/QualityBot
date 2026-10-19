# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY nuget.config Directory.Build.props Directory.Packages.props ./
COPY src/Sis.Contracts/Sis.Contracts.csproj src/Sis.Contracts/
COPY src/Sis.Web/Sis.Web.csproj src/Sis.Web/
# The private-feed credentials arrive as BuildKit secrets, exposed as env vars to this
# step only, so they never land in an image layer.
RUN --mount=type=secret,id=gh_packages_user,env=GH_PACKAGES_USER \
    --mount=type=secret,id=gh_packages_token,env=GH_PACKAGES_TOKEN \
    dotnet restore src/Sis.Web/Sis.Web.csproj

COPY src/Sis.Contracts/ src/Sis.Contracts/
COPY src/Sis.Web/ src/Sis.Web/
RUN dotnet publish src/Sis.Web/Sis.Web.csproj -c Release --no-restore -o /app/publish

FROM nginxinc/nginx-unprivileged:alpine AS final
COPY src/Sis.Web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --chmod=0755 src/Sis.Web/docker-entrypoint.d/40-sis-config.sh /docker-entrypoint.d/40-sis-config.sh
# Owned by nginx's uid so the entrypoint script can rewrite appsettings.json.
COPY --from=build --chown=101:101 /app/publish/wwwroot /usr/share/nginx/html
EXPOSE 8080
