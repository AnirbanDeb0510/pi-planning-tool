# PI Planning Tool - Configuration Guide

## Runtime Configuration (Docker)

The frontend application supports runtime configuration for the API base URL, allowing the same Docker image to be used across different environments without rebuilding.

### How It Works

1. **env.js File**: A JavaScript file (`/env.js`) is loaded before the Angular app starts
2. **Docker Entrypoint**: The `docker-entrypoint.sh` script generates `env.js` with environment variables
3. **RuntimeConfig Service**: Angular reads configuration from `window.__env` object
4. **Environment Files**: Use getter methods to access runtime configuration dynamically

### Local Development

For local development, the default API URL is `http://localhost:5000`. This is configured in `/public/env.js`:

```javascript
window['__env'] = window['__env'] || {};
window['__env']['apiBaseUrl'] = 'http://localhost:5000';
window['__env']['patTtlMinutes'] = '10';
```

To change the API URL locally, edit this file before running `npm start`.

### Docker Deployment

#### Environment Variable

Set the `API_BASE_URL` environment variable when running the container:

```bash
docker run -e API_BASE_URL=http://backend:8080 -e PAT_TTL_MINUTES=10 -p 4200:80 pi-frontend
```

#### Docker Compose

The `docker-compose.yml` is already configured:

```yaml
frontend:
  build: ./frontend/pi-planning-ui
  environment:
    API_BASE_URL: "http://pi-backend:8080"
      PAT_TTL_MINUTES: "10"
  ports:
    - "4200:80"
```

#### Custom Deployments

For production or other environments, override the environment variable:

```yaml
# Production docker-compose.override.yml
services:
  frontend:
    environment:
      API_BASE_URL: "https://api.yourdomain.com"
         PAT_TTL_MINUTES: "10"
```

### Architecture

#### Files Changed

1. **Runtime Configuration**
   - `/src/app/core/config/runtime-config.ts` - Service to read window.__env
   - `/public/env.js` - Default configuration file
   - `/src/index.html` - Loads env.js before app

2. **Environment Files**
   - `/src/environments/environment.ts` - Uses RuntimeConfig getter
   - `/src/environments/environment.prod.ts` - Uses RuntimeConfig getter

3. **Docker Setup**
   - `/Dockerfile` - Multi-stage build with nginx
   - `/nginx.conf` - Custom nginx configuration
   - `/docker-entrypoint.sh` - Generates env.js at container startup
   - `/docker-compose.yml` - Sets API_BASE_URL environment variable

### PAT Time-to-Live (TTL)

The Personal Access Token (PAT) is stored in memory for a limited time to avoid repeated prompts.
You can configure the TTL in minutes using `patTtlMinutes` in env.js or via the Docker environment
variable `PAT_TTL_MINUTES`.

**Defaults:** 10 minutes (if not configured)

4. **Removed Files**
   - Mock service files (no longer using mock data)
   - API injection tokens (direct service injection)
   - `board.service.old.ts` (legacy backup)

### Benefits

✅ **Same Docker image for all environments** - No rebuild needed  
✅ **Runtime configuration** - Change API URL without recompilation  
✅ **No mock data fallback** - Always uses real API  
✅ **Clean architecture** - Direct service injection  
✅ **Docker-first approach** - Follows backend pattern

### Testing

```bash
# Build and run with docker-compose
docker-compose up --build

# Frontend will be available at: http://localhost:4200
# Backend API at: http://localhost:8080
# Frontend will call: http://pi-backend:8080 (internal Docker network)
```

### Troubleshooting

**Issue**: Frontend can't connect to backend  
**Solution**: Check that `API_BASE_URL` environment variable is set correctly and uses the Docker service name (e.g., `http://pi-backend:8080`)

**Issue**: Getting CORS errors  
**Solution**: Ensure backend CORS settings allow requests from the frontend origin

**Issue**: env.js not loaded  
**Solution**: Check that nginx is serving the file correctly and it's not cached by the browser
