# Windows IIS + SQL Server Deployment Guide

**Last Updated:** March 3, 2026  
**Target:** Windows Server 2019/2022 or Windows 10/11 with IIS  
**Database:** SQL Server 2016+ (local or remote)  
**Deployment Model:** IIS Applications under single website (port 80)

**URLs after deployment:**

- Frontend: `http://localhost/PIPlanningUI`
- Backend API: `http://localhost/PIPlanningBackend`

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Setup Steps](#setup-steps)
3. [Backend Deployment](#backend-deployment)
4. [Database Permissions (Critical)](#database-permissions-critical)
5. [Frontend Deployment](#frontend-deployment)
6. [Troubleshooting & Common Issues](#troubleshooting--common-issues)

---

## Prerequisites

### Required Software

- **Windows Server 2019/2022** (or Windows 10/11 Professional with IIS enabled)
- **IIS (Internet Information Services)** installed and running
- **.NET 8 Hosting Bundle** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **SQL Server 2016+** (local or remote) + SSMS or sqlcmd
- **Node.js 20+** (for building Angular frontend)

### Enable IIS on Windows

**Windows 10/11:**

1. Control Panel → Programs → Turn Windows features on or off
2. Check: Internet Information Services
3. Expand: IIS → Web Services → Application Development Features
4. Check: ASP.NET 4.8, WebSocket Protocol
5. Restart

**Windows Server:**

1. Server Manager → Add Roles and Features
2. Select: Web Server (IIS), ASP.NET 4.8, WebSockets
3. Complete and restart

---

## Setup Steps

### 1. Create SQL Server Database

Run this script via SSMS or sqlcmd:

```powershell
sqlcmd -S localhost\SQLEXPRESS -i db\init-sqlserver.sql
```

Verify:

```sql
SELECT name FROM sys.databases WHERE name = 'PIPlanningDB'
```

**IMPORTANT:** Database name must be **`PIPlanningDB`** (camelcase, not lowercase).

### 2. Create IIS Application Pools

Create two application pools via IIS Manager:

- Name: `PIPlanningBackend`, .NET CLR: **No Managed Code**, Pipeline: Integrated
- Name: `PIPlanningUI`, .NET CLR: **No Managed Code**

These must exist before deploying applications.

---

## Backend Deployment

### Step 1: Configure appsettings.json

Edit: `backend/pi-planning-backend/appsettings.json`

```json
{
  "DatabaseProvider": "SqlServer",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=PIPlanningDB;User Id=sa;Password=YOUR_PASSWORD;Encrypt=false;TrustServerCertificate=true;"
  },
  "Swagger": {
    "Enabled": false
  }
}
```

**Connection String Variants:**
| Scenario | Connection String |
|----------|---|
| Local SQL Server | `Server=localhost\\SQLEXPRESS;Database=PIPlanningDB;...` |
| Named Instance | `Server=COMPUTER_NAME\\SQLEXPRESS;Database=PIPlanningDB;...` |
| Windows Auth | `Server=localhost;Database=PIPlanningDB;Integrated Security=true;` |

### Step 2: Restore Dependencies

```powershell
cd backend\pi-planning-backend
dotnet restore pi-planning-tool.sln
```

### Step 3: Publish Backend

```powershell
dotnet publish -c Release -o .\publish
```

### Step 4: Deploy to IIS

```powershell
# Copy to IIS folder
robocopy .\publish C:\inetpub\wwwroot\PIPlanningBackend /MIR /R:2 /W:2
```

### Step 5: Create IIS Application

In IIS Manager:

1. Sites → Default Web Site → Add Application
2. **Alias:** `PIPlanningBackend`
3. **Application pool:** `PIPlanningBackend`
4. **Physical path:** `C:\inetpub\wwwroot\PIPlanningBackend`
5. Click OK

### Step 6: Verify Backend

```
http://localhost/PIPlanningBackend/api/boards
```

Should return empty array: `[]` (no CORS error) ✅

---

## Database Permissions (Critical)

**This is the most common deployment issue.** The IIS AppPool identity needs DDL (CREATE TABLE) rights for migrations to apply on startup.

### Grant Permissions to AppPool

Run this SQL as Administrator in SSMS:

```sql
-- Create Windows login for AppPool
USE [master]
CREATE LOGIN [IIS APPPOOL\PIPlanningBackend] FROM WINDOWS;

-- Create database user and grant db_owner role
USE [PIPlanningDB]
CREATE USER [IIS APPPOOL\PIPlanningBackend] FOR LOGIN [IIS APPPOOL\PIPlanningBackend];
ALTER ROLE db_owner ADD MEMBER [IIS APPPOOL\PIPlanningBackend];

-- Verify
SELECT * FROM sys.database_role_members
WHERE role_principal_id = (SELECT principal_id FROM sys.database_principals WHERE name = 'IIS APPPOOL\PIPlanningBackend')
```

**If this returns empty, migrations will NOT apply on startup.**

### Backup: Manual Migration Apply

If AppPool permissions aren't set yet, manually apply migrations using one of these approaches:

#### Option A: Run from Backend Project (Recommended)

1. **Update appsettings.Development.json:**

```json
{
  "DatabaseProvider": "SqlServer",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=PIPlanningDB;User Id=sa;Password=YOUR_PASSWORD;Encrypt=false;TrustServerCertificate=true;"
  }
}
```

2. **Run from backend project:**

```powershell
cd backend\pi-planning-backend
dotnet ef database update --context AppDbContext
```

#### Option B: Run from SQL Server Migrations Project

This approach is useful when working directly with the migration project:

1. **Update DesignTimeDbContextFactory.cs temporarily:**

In `backend/pi-planning-backend.migrations.sqlserver/DesignTimeDbContextFactory.cs`, update the hardcoded fallback connection string to target SQL Server:

```csharp
// Around line 30 in the fallback section
string connectionString = "Server=localhost\\SQLEXPRESS;Database=PIPlanningDB;User Id=sa;Password=YOUR_PASSWORD;Encrypt=false;TrustServerCertificate=true;";
```

2. **Run from migrations project:**

```powershell
cd backend\pi-planning-backend.migrations.sqlserver
dotnet ef database update --context AppDbContext
```

#### Verify Migrations Applied

After applying migrations, verify in SQL Server:

```sql
SELECT MigrationId FROM __EFMigrationsHistory;
SELECT name FROM sys.tables WHERE name IN ('Boards', 'Features', 'UserStories');
```

Restart the backend app pool and test the API again.

---

## Frontend Deployment

### Step 1: Configure env.js

Edit: `frontend/pi-planning-ui/public/env.js`

```javascript
window["__env"] = window["__env"] || {};
window["__env"]["apiBaseUrl"] = "http://localhost/PIPlanningBackend";
window["__env"]["patTtlMinutes"] = "10";
```

**Important:** The script tag in `src/index.html` must be relative:

```html
<script src="env.js"></script>
<!-- NOT /env.js -->
```

### Step 2: Build Angular App

**Critical:** Use the `--` separator before passing arguments to Angular build:

```powershell
cd frontend\pi-planning-ui
npm install
npm run build -- --base-href /PIPlanningUI/ --deploy-url /PIPlanningUI/
```

Without `--`, npm will reject the arguments.

### Step 3: Deploy to IIS

```powershell
# Copy dist folder to IIS
robocopy dist\pi-planning-ui\browser C:\inetpub\wwwroot\PIPlanningUI /MIR /R:2 /W:2

# Ensure env.js is deployed
copy dist\pi-planning-ui\browser\env.js C:\inetpub\wwwroot\PIPlanningUI\env.js
```

### Step 4: Create IIS Application

In IIS Manager:

1. Sites → Default Web Site → Add Application
2. **Alias:** `PIPlanningUI`
3. **Application pool:** `PIPlanningUI`
4. **Physical path:** `C:\inetpub\wwwroot\PIPlanningUI`
5. Click OK

### Step 5: Add URL Rewrite (for Angular routing)

Create: `C:\inetpub\wwwroot\PIPlanningUI\web.config`

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

### Step 6: Test Frontend

```
http://localhost/PIPlanningUI
```

Should load the PI Planning Tool UI and connect to the backend.

---

## Troubleshooting & Common Issues

### Backend API Loads but Returns 404 on Endpoints

**Cause:** Migrations didn't apply to database.

**Fix:**

1. Verify AppPool has database permissions (see [Database Permissions](#database-permissions-critical) section)
2. Manually apply migrations (see [Backup: Manual Migration Apply](#backup-manual-migration-apply))
3. Restart `PIPlanningBackend` app pool

### Frontend Loads but Shows Blank Page

**Check 1: env.js is not loading (404 in Network tab)**

- Verify `env.js` exists in `C:\inetpub\wwwroot\PIPlanningUI\env.js`
- Verify `src/index.html` has: `<script src="env.js"></script>` (not `/env.js`)
- Copy manually if needed: `copy dist\pi-planning-ui\browser\env.js C:\inetpub\wwwroot\PIPlanningUI\env.js`

**Check 2: API calls fail (CORS or 404 errors)**

- Verify `env.js` has correct `apiBaseUrl`: `http://localhost/PIPlanningBackend`
- Verify backend is running: `http://localhost/PIPlanningBackend/api/boards` returns `[]`

**Check 3: Angular routing broken (404 on page refresh)**

- Verify `web.config` is deployed with correct URL Rewrite rule
- Verify rule points to: `/PIPlanningUI/` (not `/PIPlanningUI`)

### Database Login Failed (SQL Auth)

**Symptom:** "Login failed for user 'sa'" during startup

**Cause:** SQL Server may use Windows Auth only or `sa` is disabled.

**Fix 1: Use Windows Authentication** (Recommended)

```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=PIPlanningDB;Integrated Security=true;Encrypt=false;TrustServerCertificate=true;"
```

Then grant Windows login to AppPool (see Database Permissions section).

**Fix 2: Enable SQL Auth**

- In SQL Server Management Studio
- Right-click Server → Properties → Security
- Enable "SQL Server and Windows Authentication mode"
- Enable `sa` login and reset password
- Restart SQL Server service

### npm build fails with "Unknown argument" error

**Symptom:** `npm run build --base-href /PIPlanningUI/` fails

**Cause:** Missing `--` separator before arguments

**Fix:**

```powershell
npm run build -- --base-href /PIPlanningUI/ --deploy-url /PIPlanningUI/
```

The `--` tells npm to pass all following arguments to the Angular build script.

---

## Summary Checklist

Before declaring deployment complete:

- [ ] Database `PIPlanningDB` created
- [ ] IIS AppPool `PIPlanningBackend` and `PIPlanningUI` created
- [ ] Backend deployed to `C:\inetpub\wwwroot\PIPlanningBackend`
- [ ] AppPool identity granted `db_owner` role on database
- [ ] Backend API responds: `http://localhost/PIPlanningBackend/api/boards` → `[]`
- [ ] Frontend deployed to `C:\inetpub\wwwroot\PIPlanningUI`
- [ ] `env.js` deployed with relative path in `index.html`
- [ ] Frontend loads: `http://localhost/PIPlanningUI`
- [ ] Frontend connects to backend (Network tab shows `/api/boards` success)
- [ ] Create board, add team members, import features all work

---

## Support

For issues:

- Check SQL logs: SQL Server Management Studio error messages
- Check IIS logs: `C:\inetpub\logs\LogFiles\W3SVC1\`
- Check Event Viewer: Windows Logs → Application → IIS AspNetCore Module errors
