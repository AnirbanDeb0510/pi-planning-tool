# Windows IIS + SQL Server Deployment Guide

**Last Updated:** February 28, 2026  
**Target:** Windows Server 2019/2022 or Windows 10/11 with IIS  
**Database:** SQL Server 2016+ (local or remote)  
**Deployment Model:** IIS Applications under single website (port 80)

**URLs after deployment:**

- Frontend: `http://localhost/PIPlanningUI`
- Backend API: `http://localhost/PIPlanningBackend`

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Quick Start (6 Steps)](#quick-start)
3. [Detailed Backend Deployment](#detailed-backend-deployment)
4. [Detailed Frontend Deployment](#detailed-frontend-deployment)
5. [IIS Configuration](#iis-configuration)
6. [Troubleshooting](#troubleshooting)
7. [Accessing via IP/Hostname](#accessing-via-iphostname)

---

## Prerequisites

### Required Software

- **Windows Server 2019/2022** (or Windows 10/11 Professional with IIS enabled)
- **IIS (Internet Information Services)** installed and running
- **.NET 8 Hosting Bundle** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- **SQL Server 2016+** (local installation or remote connection)
  - **SQL Server Management Studio (SSMS)** recommended for database setup
  - Or **sqlcmd** command-line tool
- **Git** for cloning repository
- **Node.js 20+** (for building Angular frontend)

### Enable IIS (if not already enabled)

**Windows 10/11:**

1. Control Panel → Programs → Turn Windows features on or off
2. Check: Internet Information Services
3. Expand IIS → World Wide Web Services → Application Development Features
4. Check: ASP.NET 4.8, WebSocket Protocol
5. Click OK and restart

**Windows Server:**

1. Server Manager → Add Roles and Features
2. Select: Web Server (IIS)
3. Include: ASP.NET 4.8, WebSockets
4. Complete installation and restart

---

## Quick Start (6 Steps)

```powershell
# 1. Clone repository
git clone <repository-url>
cd pi-planning-tool

# 2. Create SQL Server database
# Open SQL Server Management Studio (SSMS) or use sqlcmd:
# Run the script: db/init-sqlserver.sql
# This creates the PIPlanningDB database

# 3. Configure backend for SQL Server
cd backend/pi-planning-backend
# Edit appsettings.json:
#   - Set "DatabaseProvider": "SqlServer"
#   - Update "DefaultConnection" with your SQL Server connection string

# 4. Generate SQL Server migrations
dotnet ef migrations add InitialCreate_SqlServer -o Migrations_SqlServer

# 5. Build and publish backend
dotnet publish -c Release -o ./publish

# 6. Build frontend
cd ../../frontend/pi-planning-ui
# Edit public/env.js with your backend URL
npm install
npm run build -- --base-href /PIPlanningUI/ --deploy-url /PIPlanningUI/
```

Then deploy `publish/` folder to IIS (see detailed steps below).

---

## Detailed Backend Deployment

### Step 1: Create SQL Server Database

Before deploying the application, create the database in SQL Server.

**Option A: Using SQL Server Management Studio (SSMS)**

1. Open SSMS and connect to your SQL Server instance
2. Open the file: `db/init-sqlserver.sql`
3. Click **Execute** (or press F5)
4. Verify output: "Database PIPlanningDB created successfully"

**Option B: Using sqlcmd**

```powershell
cd pi-planning-tool
sqlcmd -S localhost\SQLEXPRESS -i db/init-sqlserver.sql
```

**What this does:**

- Creates database `PIPlanningDB` if it doesn't exist
- Creates a test `VersionInfo` table to verify connectivity
- EF Core migrations will create the actual application tables on first run

**Verify:**

```sql
-- In SSMS, run this query:
SELECT name FROM sys.databases WHERE name = 'PIPlanningDB'
-- Should return: PIPlanningDB
```

### Step 2: Configure appsettings.json

Navigate to: `backend/pi-planning-backend/appsettings.json`

Update these values:

```json
{
  "DatabaseProvider": "SqlServer",
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER\\SQLEXPRESS;Database=PIPlanningDB;User Id=sa;Password=YOUR_PASSWORD;Encrypt=false;TrustServerCertificate=true;"
  }
}
```

**Connection String Examples:**

| Scenario         | Connection String                                                  |
| ---------------- | ------------------------------------------------------------------ |
| Local SQL Server | `Server=localhost\\SQLEXPRESS;Database=PIPlanningDB;...`           |
| Named Instance   | `Server=COMPUTER_NAME\\SQLEXPRESS;Database=PIPlanningDB;...`       |
| Default Instance | `Server=localhost;Database=PIPlanningDB;...`                       |
| Remote Server    | `Server=192.168.1.100;Database=PIPlanningDB;...`                   |
| Windows Auth     | `Server=localhost;Database=PIPlanningDB;Integrated Security=true;` |

**Note:** Make sure the database `PIPlanningDB` exists before proceeding (see Step 1).

### Step 3: Generate SQL Server Migrations

```powershell
cd backend/pi-planning-backend
dotnet ef migrations add InitialCreate_SqlServer -o Migrations_SqlServer
```

This creates:

- `Migrations_SqlServer/TIMESTAMP_InitialCreate_SqlServer.cs`
- `Migrations_SqlServer/TIMESTAMP_InitialCreate_SqlServer.Designer.cs`
- `Migrations_SqlServer/AppDbContextModelSnapshot.cs`

**Verify:** Files should contain SQL Server-specific types (e.g., `nvarchar`, `IDENTITY`)

### Step 4: Test Build Locally

```powershell
dotnet build
dotnet run
```

**Expected output:**

```
Active database provider: SqlServer
Now listening on: http://localhost:5000
```

Navigate to: `http://localhost:5000/swagger` (should show API documentation)

Press Ctrl+C to stop.

### Step 5: Publish Release Build

```powershell
dotnet publish -c Release -o ./publish
```

This creates a self-contained deployment in `./publish/` folder containing:

- `pi-planning-backend.dll`
- `appsettings.json`
- `web.config` (auto-generated)
- All dependencies

### Step 6: Deploy to IIS

1. **Copy publish folder** to IIS directory:

   ```powershell
   xcopy /E /I ./publish C:\inetpub\wwwroot\PIPlanningBackend
   ```

2. **Create Application Pool:**
   - Open IIS Manager
   - Right-click "Application Pools" → Add Application Pool
   - Name: `PIPlanningBackend`
   - .NET CLR version: **No Managed Code**
   - Managed pipeline mode: Integrated
   - Click OK

3. **Create IIS Application** (under Default Web Site):
   - Expand "Sites" → "Default Web Site"
   - Right-click "Default Web Site" → Add Application
   - Alias: `PIPlanningBackend` (this becomes the URL path)
   - Application pool: `PIPlanningBackend`
   - Physical path: `C:\inetpub\wwwroot\PIPlanningBackend`
   - Click OK

4. **Set Folder Permissions:**
   - Right-click `C:\inetpub\wwwroot\PIPlanningBackend` → Properties → Security
   - Add: `IIS AppPool\PIPlanningBackend`
   - Permissions: Read & Execute, List folder contents, Read
   - Click OK

5. **Test Backend:**
   - Open browser: `http://localhost/PIPlanningBackend/swagger`
   - Should show API documentation
   - Check logs in: `C:\inetpub\logs\LogFiles\`

**Note:** Default Web Site runs on port 80. If you need a different port or custom site name, create a new website first, then add applications under it.

---

## Detailed Frontend Deployment

### Step 1: Configure API URL

Navigate to: `frontend/pi-planning-ui/public/env.js`

Update with your backend URL:

```javascript
window["__env"] = window["__env"] || {};
window["__env"]["apiBaseUrl"] = "http://localhost/PIPlanningBackend"; // Update this
window["__env"]["patTtlMinutes"] = "10";
```

**Examples:**

| Deployment | apiBaseUrl                                           |
| ---------- | ---------------------------------------------------- |
| Localhost  | `http://localhost/PIPlanningBackend`                 |
| IP Address | `http://192.168.1.100/PIPlanningBackend`             |
| Hostname   | `http://server-name/PIPlanningBackend`               |
| Domain     | `http://planning.your-company.com/PIPlanningBackend` |

### Step 2: Build Angular App

```powershell
cd frontend/pi-planning-ui
npm install
npm run build -- --base-href /PIPlanningUI/ --deploy-url /PIPlanningUI/
```

This creates production build in: `dist/pi-planning-ui/browser/`

**Validation:** Open `dist/pi-planning-ui/browser/index.html` and verify:

```html
<base href="/PIPlanningUI/" />
```

### Step 3: Deploy to IIS

1. **Copy dist folder** to IIS directory:

   ```powershell
   xcopy /E /I dist\pi-planning-ui\browser C:\inetpub\wwwroot\PIPlanningUI
   ```

2. **Create Application Pool:**
   - Open IIS Manager
   - Right-click "Application Pools" → Add Application Pool
   - Name: `PIPlanningUI`
   - .NET CLR version: **No Managed Code**
   - Click OK

3. **Create IIS Application** (under Default Web Site):
   - Expand "Sites" → "Default Web Site"
   - Right-click "Default Web Site" → Add Application
   - Alias: `PIPlanningUI` (this becomes the URL path)
   - Application pool: `PIPlanningUI`
   - Physical path: `C:\inetpub\wwwroot\PIPlanningUI`
   - Click OK
   - Click OK

4. **Add URL Rewrite Rule** (for Angular routing):

   Install URL Rewrite module if not present: [Download here](https://www.iis.net/downloads/microsoft/url-rewrite)

   Create `web.config` in `C:\inetpub\wwwroot\PIPlanningUI\` (if not exists):

   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <configuration>
     <system.webServer>
       <rewrite>
         <rules>
           <rule name="Angular Routes" stopProcessing="true">
             <match url=".*" />
             <conditions logicalGrouping="MatchAll">
               <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
               <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
             </conditions>
             <action type="Rewrite" url="/PIPlanningUI/" />
           </rule>
         </rules>
       </rewrite>
     </system.webServer>
   </configuration>
   ```

5. **Test Frontend:**
   - Open browser: `http://localhost/PIPlanningUI`
   - Should load PI Planning Tool UI
   - Try creating a board to verify backend connection

---

## IIS Configuration

### Application Pool Settings

**Recommended for both API and UI:**

- .NET CLR version: **No Managed Code**
- Managed pipeline mode: **Integrated**
- Start mode: **AlwaysRunning** (optional, for faster cold starts)
- Identity: **ApplicationPoolIdentity** (default, works for most scenarios)

### Firewall Rules

If accessing from other machines, open port 80 (both applications share this port):

```powershell
# PI Planning Tool (Frontend + Backend)
netsh advfirewall firewall add rule name="PI Planning Tool" dir=in action=allow protocol=TCP localport=80
```

**Note:** Since both `/PIPlanningUI` and `/PIPlanningBackend` run under the same website on port 80, you only need to open one port.

---

## Troubleshooting

### Backend Issues

#### 500.19 - Configuration Error

**Symptom:** White page with "HTTP Error 500.19"

**Cause:** Missing .NET 8 Hosting Bundle

**Fix:**

1. Download: [.NET 8 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Install and restart IIS:
   ```powershell
   iisreset
   ```

#### 500.30 - ANCM In-Process Start Failure

**Symptom:** Error 500.30 in browser

**Cause:** Application startup failure

**Fix:**

1. Check Event Viewer:
   - Windows Logs → Application
   - Look for errors from "IIS AspNetCore Module"
2. Common causes:
   - Missing appsettings.json
   - Invalid connection string
   - Database connection failure
   - Missing dependencies

#### Database Connection Failures

**Symptom:** Error 500, logs show "Cannot open database"

**Fix:**

1. Verify SQL Server is running:
   ```powershell
   Get-Service MSSQLSERVER
   ```
2. Test connection string with `sqlcmd`:
   ```powershell
   sqlcmd -S localhost\SQLEXPRESS -U sa -P YourPassword
   ```
3. Check firewall allows SQL Server port (1433)
4. Verify SQL Server login credentials

### Frontend Issues

#### Angular Routes Not Working (404 on refresh)

**Symptom:** Direct URL navigation returns 404

**Fix:** Install URL Rewrite module and add `web.config` (see Frontend Deployment Step 4)

#### API Calls Failing (CORS errors)

**Symptom:** Browser console shows CORS errors

**Fix:**

1. Verify `env.js` has correct backend URL
2. Check backend `appsettings.json` CORS settings
3. Ensure backend is running and accessible

#### Blank Page After Deployment

**Symptom:** White page, no content

**Fix:**

1. Check browser console for JavaScript errors
2. Verify `env.js` is being loaded (check Network tab)
3. Verify `base href="/PIPlanningUI/"` in `index.html` (build with `--base-href /PIPlanningUI/ --deploy-url /PIPlanningUI/`)
4. Clear browser cache

---

## Accessing via IP/Hostname

### Configure Website Binding

Since both `/PIPlanningBackend` and `/PIPlanningUI` are applications under Default Web Site, configure binding on the parent website:

1. Open IIS Manager → Sites → Default Web Site
2. Right-click → Edit Bindings
3. Verify or add binding:
   - Type: http
   - IP address: **Your server IP** (e.g., 192.168.1.100) or **All Unassigned**
   - Port: 80
   - Host name: (leave blank for IP access, or enter hostname like `planning.company.com`)

**Note:** Both applications inherit the binding from Default Web Site. No separate port configuration needed.

### Update Frontend env.js

If using IP or hostname access, update `env.js` in deployed folder:

```javascript
// For IP access:
window["__env"]["apiBaseUrl"] = "http://192.168.1.100/PIPlanningBackend";

// For hostname access:
window["__env"]["apiBaseUrl"] = "http://server-name/PIPlanningBackend";
```

### Test from Remote Machine

```powershell
# From another computer on the network:
# Frontend
http://192.168.1.100/PIPlanningUI

# Backend API
http://192.168.1.100/PIPlanningBackend/swagger
```

---

## Common Deployment Scenarios

### Scenario 1: Single Server (Backend + Frontend + SQL Server)

- Backend: `http://localhost/PIPlanningBackend`
- Frontend: `http://localhost/PIPlanningUI`
- SQL Server: `localhost\SQLEXPRESS`
- env.js: `apiBaseUrl: 'http://localhost/PIPlanningBackend'`
- **Single port 80** - simplest deployment

### Scenario 2: Separate Backend/Frontend Servers

- Backend server: `http://192.168.1.100/PIPlanningBackend`
- Frontend server: `http://192.168.1.101/PIPlanningUI`
- env.js: `apiBaseUrl: 'http://192.168.1.100/PIPlanningBackend'`
- Each server uses port 80

### Scenario 3: Remote SQL Server

- Backend: `http://192.168.1.100/PIPlanningBackend`
- SQL Server: `192.168.1.200`
- Connection string: `Server=192.168.1.200;Database=PIPlanningDB;...`
- Frontend: `http://192.168.1.100/PIPlanningUI`

---

## Next Steps

1. **Test all features:** Create board, add team members, import features
2. **Configure SSL/HTTPS:** For production, add SSL certificates
3. **Set up backups:** Regular SQL Server database backups
4. **Monitor logs:** Check IIS logs and Event Viewer regularly
5. **Performance tuning:** Adjust IIS application pool settings for load

---

## Support

For issues, check:

- IIS Logs: `C:\inetpub\logs\LogFiles\`
- Event Viewer: Windows Logs → Application
- Backend logs: Check console output or configured log files
- Project README: Additional configuration options
