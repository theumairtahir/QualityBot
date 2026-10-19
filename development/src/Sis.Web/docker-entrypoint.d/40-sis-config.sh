#!/bin/sh
# Writes the Blazor client's runtime config from the container environment, so one image
# can point at any API without a rebuild. nginx's entrypoint runs every *.sh in
# /docker-entrypoint.d/ before starting the server.
set -eu

: "${API_BASE_URL:?API_BASE_URL must be set, e.g. http://localhost:8081/}"

cat > /usr/share/nginx/html/appsettings.json <<JSON
{
  "Api": {
    "BaseUrl": "${API_BASE_URL}"
  }
}
JSON
