#!/bin/sh
# Write the runtime config from the FRONTEND_GATEWAY_URL env var so the API
# address can be set at deploy time without rebuilding the image.
GATEWAY_URL="${FRONTEND_GATEWAY_URL:-http://localhost:8000}"
echo "{ \"gatewayUrl\": \"${GATEWAY_URL}\" }" > /usr/share/nginx/html/assets/config.json
echo "Frontend configured with gatewayUrl=${GATEWAY_URL}"
exec nginx -g 'daemon off;'
