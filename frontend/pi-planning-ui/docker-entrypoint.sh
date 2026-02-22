#!/bin/sh

# Generate env.js with runtime configuration
cat <<EOF > /usr/share/nginx/html/env.js
// Runtime configuration injected by Docker
window['__env'] = window['__env'] || {};
window['__env']['apiBaseUrl'] = '${API_BASE_URL:-http://localhost:5000}';
window['__env']['patTtlMinutes'] = '${PAT_TTL_MINUTES:-10}';
EOF

echo "Generated env.js with API_BASE_URL=${API_BASE_URL:-http://localhost:5000}"
echo "Generated env.js with PAT_TTL_MINUTES=${PAT_TTL_MINUTES:-10}"

# Start nginx
exec nginx -g 'daemon off;'
