#!/bin/sh
set -eu

: "${VITE_MARKETFEED_URL:=http://localhost:8081}"

cat > /usr/share/nginx/html/env.js <<EOF
// Generated at container startup (runtime config)
window.__env = window.__env || {};
window.__env.VITE_MARKETFEED_URL = "${VITE_MARKETFEED_URL}";
EOF
