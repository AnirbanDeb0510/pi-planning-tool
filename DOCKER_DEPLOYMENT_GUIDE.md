# Docker Deployment Guide

**Version:** 1.0  
**Last Updated:** March 7, 2026  
**Target Environment:** Linux, macOS, Windows (with Docker Desktop or Colima)

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [Quick Start](#quick-start)
4. [Architecture](#architecture)
5. [Configuration](#configuration)
6. [Production Deployment](#production-deployment)
7. [Database Management](#database-management)
8. [SSL/HTTPS Setup](#sslhttps-setup)
9. [Backup & Restore](#backup--restore)
10. [Monitoring & Logs](#monitoring--logs)
11. [Troubleshooting](#troubleshooting)
12. [Scaling & Performance](#scaling--performance)

---

## Overview

The PI Planning Tool is deployed as a **3-tier containerized application** using Docker Compose:

| Service      | Technology         | Port | Purpose                       |
| ------------ | ------------------ | ---- | ----------------------------- |
| **Frontend** | Angular 20 + Nginx | 4200 | Web UI served as static files |
| **Backend**  | ASP.NET Core 8     | 8080 | REST API + SignalR WebSocket  |
| **Database** | PostgreSQL 16      | 5432 | Persistent data storage       |

**Key Benefits:**

- ✅ **Portable**: Run on any OS with Docker support
- ✅ **Isolated**: Each service runs in its own container
- ✅ **Reproducible**: Same configuration across dev/staging/production
- ✅ **Scalable**: Easy to scale backend/frontend independently

---

## Prerequisites

### System Requirements

**Hardware (Minimum):**

- CPU: 2 cores
- RAM: 4 GB
- Disk: 10 GB free space

**Hardware (Recommended for Production):**

- CPU: 4 cores
- RAM: 8 GB
- Disk: 50 GB SSD

### Software

**Install Docker:**

**Linux (Ubuntu/Debian):**

```bash
sudo apt update
sudo apt install docker.io docker-compose-plugin -y
sudo systemctl start docker
sudo systemctl enable docker
sudo usermod -aG docker $USER
```

**macOS:**

```bash
# Option 1: Docker Desktop
# Download from https://www.docker.com/products/docker-desktop/

# Option 2: Colima (lightweight alternative)
brew install colima docker docker-compose
colima start --cpu 4 --memory 8 --disk 50
```

**Windows:**

```powershell
# Install Docker Desktop for Windows
# Download from https://www.docker.com/products/docker-desktop/
```

**Verify Installation:**

```bash
docker --version        # Should show Docker 20.10+
docker compose version  # Should show Docker Compose v2.x
```

### Network Requirements

**Firewall Rules:**

- Port 4200: Frontend (HTTP)
- Port 8080: Backend API (HTTP)
- Port 5432: PostgreSQL (internal only, not exposed in production)

**Outbound Access:**

- `dev.azure.com`: Required for Azure DevOps API integration
- Docker Hub (optional): For pulling base images during build

---

## Quick Start

### Step 1: Clone Repository

```bash
git clone https://github.com/yourusername/pi-planning-tool.git
cd pi-planning-tool
```

### Step 2: Configure Environment Variables

Edit `docker-compose.yml` or create a `.env` file:

```bash
# Database credentials
POSTGRES_PASSWORD=YourStrong!Passw0rd
POSTGRES_DB=PIPlanningDB
POSTGRES_USER=postgres

# Backend configuration
ASPNETCORE_ENVIRONMENT=Development
DATABASE_PROVIDER=PostgreSQL

# Frontend configuration
API_BASE_URL=http://localhost:8080
PAT_TTL_MINUTES=10
```

### Step 3: Start All Services

```bash
# Build and start in detached mode
docker compose up -d --build

# Check container status
docker compose ps
```

**Expected Output:**

```
NAME           IMAGE                       STATUS         PORTS
pi-postgres    pi-planning-tool-db         Up 10 seconds  0.0.0.0:5432->5432/tcp
pi-backend     pi-planning-tool-backend    Up 9 seconds   0.0.0.0:8080->8080/tcp
pi-frontend    pi-planning-tool-frontend   Up 8 seconds   0.0.0.0:4200->80/tcp
```

### Step 4: Verify Deployment

**Frontend:**

```bash
curl http://localhost:4200
# Should return Angular index.html
```

**Backend API:**

```bash
curl http://localhost:8080/api/boards/search?searchTerm=test
# Should return JSON: {"boards": [], "totalCount": 0}
```

**Database:**

```bash
docker exec -it pi-postgres psql -U postgres -d PIPlanningDB -c "\dt"
# Should list tables: Boards, Features, UserStories, TeamMembers, etc.
```

### Step 5: Access the Application

Open browser: **http://localhost:4200**

**First-Time Setup:**

1. Click "Create New Board"
2. Enter board details (name, org, project)
3. Configure sprints (5 sprints, 2 weeks each)
4. Import features from Azure DevOps (requires PAT)

---

## Architecture

### Container Network

```
┌─────────────────────────────────────────────────────────────┐
│                        Host Machine                         │
│                                                             │
│  ┌───────────────────────────────────────────────────────┐ │
│  │              Docker Network: pi-net                   │ │
│  │                                                       │ │
│  │  ┌──────────────┐    ┌──────────────┐    ┌────────┐ │ │
│  │  │  Frontend    │───▶│   Backend    │───▶│   DB   │ │ │
│  │  │  (Nginx)     │    │  (ASP.NET)   │    │  (PG)  │ │ │
│  │  │  Port: 4200  │    │  Port: 8080  │    │  5432  │ │ │
│  │  └──────────────┘    └──────────────┘    └────────┘ │ │
│  │         ▲                    │                       │ │
│  │         │                    │                       │ │
│  │         │                    ▼                       │ │
│  │         │            SignalR WebSocket              │ │
│  │         │            (Real-time sync)               │ │
│  │         │                                           │ │
│  └─────────┼───────────────────────────────────────────┘ │
│            │                                             │
│            │  HTTP Requests                              │
│            │  (User Browser)                             │
└────────────┼─────────────────────────────────────────────┘
             │
        [End Users]
```

### Volume Mounts

| Volume  | Container Path             | Host Path      | Purpose                |
| ------- | -------------------------- | -------------- | ---------------------- |
| DB Data | `/var/lib/postgresql/data` | `./db/pg-data` | Persist database files |

**Important:** Database volumes ensure data survives container restarts and rebuilds.

---

## Configuration

### docker-compose.yml Explained

```yaml
services:
  # PostgreSQL Database
  db:
    build: ./db
    container_name: pi-postgres
    environment:
      POSTGRES_PASSWORD: "YourStrong!Passw0rd" # Change in production
      POSTGRES_DB: PIPlanningDB
      POSTGRES_USER: postgres
    ports:
      - "5432:5432" # Remove in production (use internal only)
    volumes:
      - ./db/pg-data:/var/lib/postgresql/data # Persist data
    restart: always
    networks:
      - pi-net

  # ASP.NET Core Backend
  backend:
    build:
      context: ./backend
      dockerfile: pi-planning-backend/Dockerfile
    container_name: pi-backend
    depends_on:
      - db
    environment:
      ASPNETCORE_ENVIRONMENT: "Development" # Change to Production
      ASPNETCORE_URLS: "http://+:8080"
      DatabaseProvider: "PostgreSQL"
      ConnectionStrings__DefaultConnection: "Host=pi-postgres;Database=PIPlanningDB;Username=postgres;Password=YourStrong!Passw0rd"
    ports:
      - "8080:8080"
    restart: always
    networks:
      - pi-net

  # Angular Frontend (Nginx)
  frontend:
    build: ./frontend/pi-planning-ui
    container_name: pi-frontend
    depends_on:
      - backend
    environment:
      API_BASE_URL: "http://localhost:8080" # Change to production domain
      PAT_TTL_MINUTES: "10"
    ports:
      - "4200:80"
    restart: always
    networks:
      - pi-net

networks:
  pi-net:
    driver: bridge
```

### Environment Variables Reference

**Database (db service):**

| Variable            | Default               | Description                   |
| ------------------- | --------------------- | ----------------------------- |
| `POSTGRES_PASSWORD` | `YourStrong!Passw0rd` | PostgreSQL superuser password |
| `POSTGRES_DB`       | `PIPlanningDB`        | Database name                 |
| `POSTGRES_USER`     | `postgres`            | Database username             |

**Backend (backend service):**

| Variable                               | Default         | Description                                  |
| -------------------------------------- | --------------- | -------------------------------------------- |
| `ASPNETCORE_ENVIRONMENT`               | `Development`   | ASP.NET environment (Development/Production) |
| `ASPNETCORE_URLS`                      | `http://+:8080` | Backend listening address                    |
| `DatabaseProvider`                     | `PostgreSQL`    | Database provider (PostgreSQL/SqlServer)     |
| `ConnectionStrings__DefaultConnection` | See above       | Connection string (host=pi-postgres)         |

**Frontend (frontend service):**

| Variable          | Default                 | Description                            |
| ----------------- | ----------------------- | -------------------------------------- |
| `API_BASE_URL`    | `http://localhost:8080` | Backend API endpoint (user-facing URL) |
| `PAT_TTL_MINUTES` | `10`                    | Azure PAT cache TTL in minutes         |

---

## Production Deployment

### Production Checklist

- [ ] Change default database password
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Remove database port exposure (5432)
- [ ] Configure reverse proxy (Nginx/Caddy)
- [ ] Enable HTTPS/TLS with valid certificates
- [ ] Set resource limits (CPU/memory)
- [ ] Configure CORS for production domain
- [ ] Set up log aggregation (ELK, Loki, etc.)
- [ ] Enable automated backups
- [ ] Configure health checks and monitoring

### Step 1: Create Production docker-compose.yml

**File: `docker-compose.prod.yml`**

```yaml
services:
  db:
    build: ./db
    container_name: pi-postgres
    environment:
      POSTGRES_PASSWORD_FILE: /run/secrets/db_password # Use Docker secrets
      POSTGRES_DB: PIPlanningDB
      POSTGRES_USER: postgres
    volumes:
      - pi-db-data:/var/lib/postgresql/data # Named volume
    restart: always
    networks:
      - pi-net
    # No ports exposed (internal only)
    deploy:
      resources:
        limits:
          cpus: "2"
          memory: 2G
        reservations:
          cpus: "1"
          memory: 1G

  backend:
    build:
      context: ./backend
      dockerfile: pi-planning-backend/Dockerfile
    container_name: pi-backend
    depends_on:
      - db
    environment:
      ASPNETCORE_ENVIRONMENT: "Production"
      ASPNETCORE_URLS: "http://+:8080"
      DatabaseProvider: "PostgreSQL"
      ConnectionStrings__DefaultConnection: "Host=pi-postgres;Database=PIPlanningDB;Username=postgres;Password=${DB_PASSWORD}"
    restart: always
    networks:
      - pi-net
    # No ports exposed (reverse proxy only)
    deploy:
      resources:
        limits:
          cpus: "2"
          memory: 2G
    healthcheck:
      test:
        [
          "CMD",
          "curl",
          "-f",
          "http://localhost:8080/api/boards/search?searchTerm=",
        ]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

  frontend:
    build: ./frontend/pi-planning-ui
    container_name: pi-frontend
    depends_on:
      - backend
    environment:
      API_BASE_URL: "https://api.yourdomain.com" # Production API URL
      PAT_TTL_MINUTES: "10"
    restart: always
    networks:
      - pi-net
    # No ports exposed (reverse proxy only)
    deploy:
      resources:
        limits:
          cpus: "1"
          memory: 512M

  # Nginx Reverse Proxy (optional)
  nginx:
    image: nginx:alpine
    container_name: pi-nginx
    depends_on:
      - frontend
      - backend
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf:ro
      - ./nginx/ssl:/etc/nginx/ssl:ro
    ports:
      - "80:80"
      - "443:443"
    restart: always
    networks:
      - pi-net

networks:
  pi-net:
    driver: bridge

volumes:
  pi-db-data:
    driver: local

secrets:
  db_password:
    file: ./secrets/db_password.txt
```

### Step 2: Create .env File

**File: `.env`**

```bash
# Database
DB_PASSWORD=SuperSecureRandomPassword123!

# Backend
ASPNETCORE_ENVIRONMENT=Production

# Frontend
API_BASE_URL=https://api.yourdomain.com
```

**Secure the file:**

```bash
chmod 600 .env
```

### Step 3: Update CORS Configuration

Edit `backend/pi-planning-backend/appsettings.json`:

```json
{
  "Cors": {
    "AllowedOrigins": ["https://yourdomain.com"]
  }
}
```

### Step 4: Deploy

```bash
# Build and start production stack
docker compose -f docker-compose.prod.yml --env-file .env up -d --build

# Check logs
docker compose -f docker-compose.prod.yml logs -f
```

---

## Database Management

### Initial Migration

Migrations are automatically applied on backend startup via:

```csharp
// Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();  // Applies pending migrations
}
```

**Manual Migration (if needed):**

```bash
# Enter backend container
docker exec -it pi-backend bash

# Run migrations
cd /app
dotnet ef database update
```

### Database Connection

**From Host Machine:**

```bash
# Development (port 5432 exposed)
psql -h localhost -U postgres -d PIPlanningDB

# Enter password when prompted
```

**From Backend Container:**

```bash
docker exec -it pi-backend bash
apt-get update && apt-get install -y postgresql-client
psql -h pi-postgres -U postgres -d PIPlanningDB
```

### Common SQL Queries

**Check Tables:**

```sql
\dt
```

**Count Boards:**

```sql
SELECT COUNT(*) FROM "Boards";
```

**List Recent Boards:**

```sql
SELECT "Id", "Name", "Organization", "Project", "CreatedDate"
FROM "Boards"
ORDER BY "CreatedDate" DESC
LIMIT 10;
```

**Check Database Size:**

```sql
SELECT pg_size_pretty(pg_database_size('PIPlanningDB'));
```

---

## SSL/HTTPS Setup

### Option 1: Nginx Reverse Proxy (Recommended)

**File: `nginx/nginx.conf`**

```nginx
events {
    worker_connections 1024;
}

http {
    upstream frontend {
        server pi-frontend:80;
    }

    upstream backend {
        server pi-backend:8080;
    }

    # Redirect HTTP to HTTPS
    server {
        listen 80;
        server_name yourdomain.com;
        return 301 https://$server_name$request_uri;
    }

    # HTTPS Server
    server {
        listen 443 ssl http2;
        server_name yourdomain.com;

        ssl_certificate /etc/nginx/ssl/cert.pem;
        ssl_certificate_key /etc/nginx/ssl/key.pem;
        ssl_protocols TLSv1.2 TLSv1.3;
        ssl_ciphers HIGH:!aNULL:!MD5;

        # Frontend
        location / {
            proxy_pass http://frontend;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }

        # Backend API
        location /api {
            proxy_pass http://backend;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }

        # SignalR WebSocket
        location /planningHub {
            proxy_pass http://backend;
            proxy_http_version 1.1;
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection "upgrade";
            proxy_set_header Host $host;
            proxy_cache_bypass $http_upgrade;
            proxy_read_timeout 86400;
        }
    }
}
```

**Generate Self-Signed Certificate (Development):**

```bash
mkdir -p nginx/ssl
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout nginx/ssl/key.pem \
  -out nginx/ssl/cert.pem \
  -subj "/CN=localhost"
```

**Use Let's Encrypt (Production):**

```bash
# Install Certbot
sudo apt install certbot

# Generate certificate
sudo certbot certonly --standalone -d yourdomain.com

# Copy certificates
sudo cp /etc/letsencrypt/live/yourdomain.com/fullchain.pem nginx/ssl/cert.pem
sudo cp /etc/letsencrypt/live/yourdomain.com/privkey.pem nginx/ssl/key.pem
```

### Option 2: Caddy Reverse Proxy (Auto-SSL)

**File: `Caddyfile`**

```
yourdomain.com {
    reverse_proxy /api/* pi-backend:8080
    reverse_proxy /planningHub/* pi-backend:8080 {
        header_up Connection {http.request.header.Connection}
        header_up Upgrade {http.request.header.Upgrade}
    }
    reverse_proxy pi-frontend:80
}
```

**docker-compose.yml addition:**

```yaml
  caddy:
    image: caddy:alpine
    container_name: pi-caddy
    volumes:
      - ./Caddyfile:/etc/caddy/Caddyfile
      - caddy-data:/data
      - caddy-config:/config
    ports:
      - "80:80"
      - "443:443"
    networks:
      - pi-net

volumes:
  caddy-data:
  caddy-config:
```

---

## Backup & Restore

### Backup Database

**Automated Daily Backup:**

```bash
#!/bin/bash
# File: scripts/backup-db.sh

BACKUP_DIR="/backups/pi-planning"
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="$BACKUP_DIR/pi-planning-backup-$DATE.sql"

# Create backup directory
mkdir -p $BACKUP_DIR

# Backup database
docker exec pi-postgres pg_dump -U postgres PIPlanningDB > $BACKUP_FILE

# Compress backup
gzip $BACKUP_FILE

# Delete backups older than 30 days
find $BACKUP_DIR -name "*.sql.gz" -mtime +30 -delete

echo "Backup completed: $BACKUP_FILE.gz"
```

**Schedule with Cron:**

```bash
# Edit crontab
crontab -e

# Add daily backup at 2 AM
0 2 * * * /path/to/scripts/backup-db.sh
```

### Restore Database

**From Backup File:**

```bash
# Stop backend to prevent connections
docker compose stop backend

# Restore database
gunzip -c /backups/pi-planning/pi-planning-backup-20260307_020000.sql.gz \
  | docker exec -i pi-postgres psql -U postgres -d PIPlanningDB

# Restart backend
docker compose start backend
```

### Volume Backup (Full System)

**Backup Volumes:**

```bash
# Stop all services
docker compose down

# Backup database volume
docker run --rm \
  -v pi-planning-tool_pi-db-data:/data \
  -v $(pwd)/backups:/backup \
  alpine tar czf /backup/db-volume-$(date +%Y%m%d).tar.gz /data

# Restart services
docker compose up -d
```

**Restore Volumes:**

```bash
# Stop services
docker compose down

# Restore volume
docker run --rm \
  -v pi-planning-tool_pi-db-data:/data \
  -v $(pwd)/backups:/backup \
  alpine tar xzf /backup/db-volume-20260307.tar.gz -C /

# Restart services
docker compose up -d
```

---

## Monitoring & Logs

### View Logs

**All Services:**

```bash
docker compose logs -f
```

**Specific Service:**

```bash
docker compose logs -f backend
docker compose logs -f frontend
docker compose logs -f db
```

**Last 100 Lines:**

```bash
docker compose logs --tail=100 backend
```

**Since Timestamp:**

```bash
docker compose logs --since 2026-03-07T10:00:00 backend
```

### Log Rotation

**Configure Docker Log Driver:**

Edit `/etc/docker/daemon.json`:

```json
{
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "10m",
    "max-file": "3"
  }
}
```

Restart Docker:

```bash
sudo systemctl restart docker
```

### Health Checks

**Check Container Health:**

```bash
docker compose ps
```

**Backend Health Endpoint:**

```bash
curl http://localhost:8080/api/boards/search?searchTerm=
# Should return: {"boards": [], "totalCount": 0}
```

**Database Health:**

```bash
docker exec pi-postgres pg_isready -U postgres
# Should return: /var/run/postgresql:5432 - accepting connections
```

### Monitoring Stack (Optional)

**Add Prometheus + Grafana:**

```yaml
prometheus:
  image: prom/prometheus
  volumes:
    - ./monitoring/prometheus.yml:/etc/prometheus/prometheus.yml
  ports:
    - "9090:9090"
  networks:
    - pi-net

grafana:
  image: grafana/grafana
  ports:
    - "3000:3000"
  networks:
    - pi-net
```

---

## Troubleshooting

### Issue: Backend Cannot Connect to Database

**Symptom:**

```
Npgsql.NpgsqlException: Connection refused
```

**Diagnosis:**

```bash
# Check if DB is running
docker compose ps db

# Check DB logs
docker compose logs db

# Test DB connectivity from backend
docker exec -it pi-backend bash
nc -zv pi-postgres 5432
```

**Solutions:**

1. Ensure `depends_on: - db` is set for backend
2. Wait 10-15 seconds for DB to fully start
3. Check connection string (Host=pi-postgres, not localhost)
4. Verify network connectivity: `docker network inspect pi-planning-tool_pi-net`

---

### Issue: Frontend Cannot Reach Backend

**Symptom:**

- Frontend loads but API calls fail
- Browser console shows CORS errors

**Diagnosis:**

```bash
# Check backend logs for CORS errors
docker compose logs backend | grep CORS

# Test backend from host
curl http://localhost:8080/api/boards/search?searchTerm=
```

**Solutions:**

1. Update `API_BASE_URL` in frontend environment to match backend URL
2. Configure CORS in `appsettings.json`:
   ```json
   "Cors": {
     "AllowedOrigins": ["http://localhost:4200"]
   }
   ```
3. Ensure backend port is exposed (8080:8080)
4. Check browser network tab for exact error

---

### Issue: SignalR WebSocket Fails

**Symptom:**

```
WebSocket connection failed: Error during WebSocket handshake
```

**Diagnosis:**

```bash
# Check SignalR endpoint
curl -i http://localhost:8080/planningHub/negotiate

# Should return 200 with JSON response
```

**Solutions:**

1. Ensure reverse proxy forwards WebSocket upgrade headers:
   ```nginx
   proxy_http_version 1.1;
   proxy_set_header Upgrade $http_upgrade;
   proxy_set_header Connection "upgrade";
   ```
2. Check CORS allows credentials:
   ```csharp
   app.UseCors(policy => policy
       .WithOrigins("http://localhost:4200")
       .AllowCredentials());
   ```
3. Verify SignalR endpoint in frontend service:
   ```typescript
   const connection = new signalR.HubConnectionBuilder()
     .withUrl("http://localhost:8080/planningHub")
     .build();
   ```

---

### Issue: Database Volume Permissions

**Symptom:**

```
FATAL: data directory "/var/lib/postgresql/data" has invalid permissions
```

**Solution:**

```bash
# Fix permissions
sudo chown -R 999:999 db/pg-data
sudo chmod 700 db/pg-data

# Restart database
docker compose restart db
```

---

### Issue: Out of Memory

**Symptom:**

```
OOMKilled
```

**Diagnosis:**

```bash
# Check container stats
docker stats

# Check host memory
free -h
```

**Solutions:**

1. Increase Docker memory limit (Docker Desktop settings)
2. Set container resource limits in docker-compose.yml
3. Optimize backend queries (add indexes, pagination)
4. Scale horizontally (multiple backend replicas)

---

### Issue: Slow Performance

**Diagnosis:**

```bash
# Check CPU/memory usage
docker stats

# Check database connections
docker exec pi-postgres psql -U postgres -d PIPlanningDB -c "SELECT count(*) FROM pg_stat_activity;"

# Check query performance
docker compose logs backend | grep "Executed DbCommand"
```

**Solutions:**

1. Add database indexes on frequently queried columns:
   ```sql
   CREATE INDEX idx_boards_org ON "Boards"("Organization");
   CREATE INDEX idx_features_boardid ON "Features"("BoardId");
   ```
2. Enable query caching in backend
3. Use connection pooling (EF Core default: max 100 connections)
4. Scale backend replicas with load balancer

---

## Scaling & Performance

### Horizontal Scaling (Multiple Backend Replicas)

**Update docker-compose.yml:**

```yaml
backend:
  build: ./backend/pi-planning-backend
  deploy:
    replicas: 3 # 3 backend instances
  environment:
    ASPNETCORE_ENVIRONMENT: "Production"
    # ... other config
  networks:
    - pi-net

nginx:
  image: nginx:alpine
  volumes:
    - ./nginx/nginx-lb.conf:/etc/nginx/nginx.conf:ro
  ports:
    - "80:80"
  networks:
    - pi-net
```

**Load Balancer Config (nginx-lb.conf):**

```nginx
http {
    upstream backend_pool {
        least_conn;  # Load balancing algorithm
        server pi-backend-1:8080;
        server pi-backend-2:8080;
        server pi-backend-3:8080;
    }

    server {
        listen 80;
        location /api {
            proxy_pass http://backend_pool;
        }
        location /planningHub {
            proxy_pass http://backend_pool;
            # WebSocket sticky sessions required
            ip_hash;
        }
    }
}
```

### Database Optimization

**Connection Pooling:**

Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=pi-postgres;Database=PIPlanningDB;Username=postgres;Password=..;Maximum Pool Size=100;Connection Idle Lifetime=60;"
  }
}
```

**Indexes:**

```sql
-- Speed up board search
CREATE INDEX idx_boards_name ON "Boards"("Name");
CREATE INDEX idx_boards_org_project ON "Boards"("Organization", "Project");

-- Speed up feature queries
CREATE INDEX idx_features_boardid ON "Features"("BoardId");

-- Speed up story queries
CREATE INDEX idx_userstories_featureid ON "UserStories"("FeatureId");
CREATE INDEX idx_userstories_sprintid ON "UserStories"("TargetSprintId");
```

### Caching Strategy

**Add Redis (Optional):**

```yaml
redis:
  image: redis:alpine
  container_name: pi-redis
  ports:
    - "6379:6379"
  networks:
    - pi-net
```

**Backend Caching:**

```csharp
// Cache board previews for 5 minutes
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "pi-redis:6379";
});
```

---

## Additional Resources

### Related Documentation

- [User Guide](USER_GUIDE.md): End-user documentation
- [API Reference](API_REFERENCE.md): REST API and SignalR documentation
- [Architecture Guide](ARCHITECTURE.md): System design and patterns
- [Configuration Guide](CONFIGURATION.md): Environment variables and settings
- [IIS Deployment Guide](IIS_DEPLOYMENT_GUIDE.md): Windows deployment with SQL Server
- [Security Guide](SECURITY.md): Security best practices

### Useful Commands

**Start Services:**

```bash
docker compose up -d
```

**Stop Services:**

```bash
docker compose down
```

**Rebuild All:**

```bash
docker compose up -d --build --force-recreate
```

**View Resource Usage:**

```bash
docker stats
```

**Clean Up:**

```bash
# Remove stopped containers
docker compose down --remove-orphans

# Remove unused images
docker image prune -a

# Remove unused volumes
docker volume prune
```

---

## Changelog

### Version 1.0 (March 2026)

- Initial Docker deployment guide
- PostgreSQL configuration
- Production deployment best practices
- SSL/HTTPS setup with Nginx and Caddy
- Backup and restore procedures
- Monitoring and logging guidance
- Comprehensive troubleshooting section
- Scaling and performance optimization

---

**Questions or issues?** Report on GitHub or contact the DevOps team.

**Happy Deploying! 🐳**
