#!/bin/sh

# Generate env.js with runtime configuration
cat <<EOF > /usr/share/nginx/html/env.js
// Runtime configuration injected by Docker
window['__env'] = window['__env'] || {};
window['__env']['apiBaseUrl'] = '${API_BASE_URL:-http://localhost:5000}';
EOF

echo "Generated env.js with API_BASE_URL=${API_BASE_URL:-http://localhost:5000}"

# Start nginx
exec nginx -g 'daemon off;'
