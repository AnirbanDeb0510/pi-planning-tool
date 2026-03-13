# Security Guide

**Version:** 1.0  
**Last Updated:** March 7, 2026  
**Audience:** DevOps, Security Engineers, System Administrators

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Authentication & Authorization](#authentication--authorization)
3. [Password Security](#password-security)
4. [Input Validation](#input-validation)
5. [Data Protection](#data-protection)
6. [Network Security](#network-security)
7. [Azure DevOps Integration Security](#azure-devops-integration-security)
8. [CORS Configuration](#cors-configuration)
9. [Audit & Logging](#audit--logging)
10. [Security Best Practices](#security-best-practices)
11. [Vulnerability Disclosure](#vulnerability-disclosure)
12. [Compliance & Standards](#compliance--standards)

---

## Overview

The PI Planning Tool implements **defense-in-depth security** with multiple layers of protection:

| Security Layer               | Technology                        | Purpose                         |
| ---------------------------- | --------------------------------- | ------------------------------- |
| **Password Hashing**         | PBKDF2-HMAC-SHA256                | Protect board lock passwords    |
| **Input Validation**         | DataAnnotations + ModelState      | Prevent invalid/malicious input |
| **SQL Injection Protection** | Entity Framework Core             | Parameterized queries           |
| **XSS Prevention**           | Angular DomSanitizer              | Sanitize user-generated content |
| **CORS**                     | ASP.NET Core CORS Middleware      | Control cross-origin access     |
| **PAT Temporary Storage**    | In-memory cache (10 min TTL)      | Minimize credential exposure    |
| **Audit Logging**            | Correlation IDs + Structured Logs | Track security events           |
| **HTTPS/TLS**                | Reverse Proxy (Nginx/Caddy)       | Encrypt data in transit         |

**Security Posture:**

- ✅ **No plaintext passwords** stored in database
- ✅ **No long-lived credentials** persisted (PATs expire after 10 minutes)
- ✅ **No user authentication system** (stateless API, board-level access control)
- ✅ **SQL injection resistant** via EF Core parameterization
- ✅ **XSS resistant** via Angular automatic escaping
- ⚠️ **No rate limiting** (recommended to add at reverse proxy layer)
- ⚠️ **No API authentication** (public API, secure via board passwords if needed)

---

## Authentication & Authorization

### Authentication Model

The PI Planning Tool uses a **lightweight authentication model** without user accounts:

**No Global Authentication:**

- No user registration or login system
- No JWT tokens or session management
- API endpoints are publicly accessible (except board-specific operations)

**Board-Level Authentication:**

- **Board Locking**: Password-protected (PBKDF2 hashing)
- **Azure DevOps Integration**: Personal Access Token (PAT) validation

### Access Control Matrix

| Operation               | Authentication Required | Authorization Method                    |
| ----------------------- | ----------------------- | --------------------------------------- |
| Create Board            | None                    | Open (anyone can create)                |
| View Board              | None (unless locked)    | Board ID knowledge                      |
| Search Boards           | None                    | Open (returns public metadata)          |
| Edit Board              | Password (if locked)    | Password verification                   |
| Lock Board              | Password (set by user)  | Password stored as PBKDF2 hash          |
| Unlock Board            | Password                | Password verification                   |
| Import Azure Features   | Azure PAT               | PAT validation against Azure DevOps API |
| Real-Time Collaboration | None                    | SignalR connection via WebSocket        |

### Board Preview Security

**Purpose:** Prevent data leaks when sharing board IDs.

**Endpoint:** `GET /api/boards/{id}/preview`

**Response (Limited Data):**

```json
{
  "id": 123,
  "name": "PI 2024 Q2",
  "organization": "my-org",
  "project": "MyProject",
  "isLocked": true,
  "isFinalized": true,
  "hasAzureFeatures": true
}
```

**Benefits:**

- Does not expose features, stories, or team members
- Safe to use for board existence checks
- Used by frontend to determine if PAT is required before full load

---

## Password Security

### PBKDF2 Implementation

**Algorithm:** PBKDF2-HMAC-SHA256 (Password-Based Key Derivation Function 2)

**File:** `backend/pi-planning-backend/Services/Utilities/PasswordHelper.cs`

**Configuration:**

```csharp
private const int SaltSize = 16;      // 128 bits (cryptographically random)
private const int Iterations = 10000; // NIST recommended minimum
private const int HashSize = 20;      // 160 bits output hash
private const HashAlgorithmName = HashAlgorithmName.SHA256;
```

**Storage Format:**

```
salt:hash
[Base64-encoded salt]:[Base64-encoded hash]
```

**Example stored hash:**

```
aBcD1234eFgH5678iJkL9012:mNoPqRsTuVwXyZ1234567890aBcDeF==
```

### Password Hashing Process

**1. Hash Password (on Lock):**

```csharp
public static string HashPassword(string password)
{
    // Generate 128-bit cryptographically random salt
    byte[] salt = new byte[16];
    using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
    {
        rng.GetBytes(salt);
    }

    // Hash password with PBKDF2 (10,000 iterations)
    using Rfc2898DeriveBytes pbkdf2 = new(password, salt, 10000, HashAlgorithmName.SHA256);
    byte[] hash = pbkdf2.GetBytes(20);

    // Return "salt:hash" format
    return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
}
```

**2. Verify Password (on Unlock):**

```csharp
public static bool VerifyPassword(string password, string storedHash)
{
    // Parse stored "salt:hash"
    string[] parts = storedHash.Split(':');
    byte[] salt = Convert.FromBase64String(parts[0]);
    byte[] storedHashBytes = Convert.FromBase64String(parts[1]);

    // Re-hash input password with extracted salt
    using Rfc2898DeriveBytes pbkdf2 = new(password, salt, 10000, HashAlgorithmName.SHA256);
    byte[] computedHash = pbkdf2.GetBytes(20);

    // Constant-time comparison (prevent timing attacks)
    return CryptographicOperations.FixedTimeEquals(storedHashBytes, computedHash);
}
```

### Timing Attack Prevention

**Issue:** Comparing hashes byte-by-byte can leak information via timing differences.

**Solution:** `CryptographicOperations.FixedTimeEquals()`

**Example:**

```csharp
// ❌ Vulnerable to timing attacks
return storedHash == computedHash;

// ✅ Constant-time comparison (secure)
return CryptographicOperations.FixedTimeEquals(storedHashBytes, computedHash);
```

**Mitigation:** Comparison always takes the same time regardless of where the mismatch occurs.

### Password Policy

**Current Rules:**

- **Minimum Length:** 6 characters
- **Maximum Length:** 100 characters
- **Character Requirements:** None (allows any character)
- **Expiration:** Never (passwords don't expire)

**Recommended Enhancements (Future):**

- Increase minimum to 12 characters
- Enforce complexity (uppercase, lowercase, digits, symbols)
- Implement password strength meter in UI
- Add optional expiration (e.g., 90 days)
- Track failed unlock attempts (rate limiting)

---

## Input Validation

### 3-Layer Validation Strategy

**Layer 1: Client-Side (Angular)**

- HTML5 validation attributes (`required`, `minlength`, `maxlength`, `pattern`)
- Immediate feedback to users (before API call)
- **Not security-critical** (can be bypassed via browser DevTools)

**Layer 2: DTO Validation (DataAnnotations)**

- Declarative validation on model properties
- Enforced at API boundary via `[ApiController]` attribute
- Returns 400 Bad Request with field-level errors

**Layer 3: Business Logic Validation (Service Layer)**

- Complex validation rules (e.g., "Cannot finalize board with unassigned stories")
- Database consistency checks
- Returns 400 with descriptive error message

### DataAnnotations Example

**File:** `backend/pi-planning-backend/DTOs/BoardCreateDto.cs`

```csharp
public class BoardCreateDto
{
    [Required(ErrorMessage = "Board name is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be 3-100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Organization is required")]
    [StringLength(50, ErrorMessage = "Organization must not exceed 50 characters")]
    public string Organization { get; set; } = string.Empty;

    [Required(ErrorMessage = "Project is required")]
    [StringLength(50, ErrorMessage = "Project must not exceed 50 characters")]
    public string Project { get; set; } = string.Empty;

    [Range(1, 10, ErrorMessage = "Number of sprints must be between 1 and 10")]
    public int NumSprints { get; set; }

    [Range(1, 4, ErrorMessage = "Sprint duration must be 1-4 weeks")]
    public int SprintDuration { get; set; }
}
```

### Automatic Validation Enforcement

**Global Filter:** `ValidateModelStateFilter` (registered in `Program.cs`)

```csharp
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidateModelStateFilter>();
});
```

**Behavior:**

- Intercepts all controller actions with `[FromBody]` parameters
- Returns 400 Bad Request if validation fails
- No need for manual `if (!ModelState.IsValid)` checks

**Error Response Format:**

```json
{
  "error": {
    "message": "Validation failed",
    "details": "One or more validation errors occurred",
    "errors": {
      "Name": ["Board name is required"],
      "NumSprints": ["Number of sprints must be between 1 and 10"]
    },
    "timestamp": "2026-03-07T14:30:00Z"
  }
}
```

### SQL Injection Prevention

**Technology:** Entity Framework Core (EF Core)

**Mechanism:** All queries are **parameterized** by default.

**Example (Safe):**

```csharp
// EF Core automatically parameterizes values
var board = await _dbContext.Boards
    .Where(b => b.Name == userInput)  // userInput is parameterized
    .FirstOrDefaultAsync();

// Generated SQL (safe):
// SELECT * FROM Boards WHERE Name = @p0
// Parameters: @p0 = 'userInput'
```

**Dangerous Pattern (Avoided):**

```csharp
// ❌ NEVER DO THIS (vulnerable to SQL injection)
var sql = $"SELECT * FROM Boards WHERE Name = '{userInput}'";
var board = await _dbContext.Boards.FromSqlRaw(sql).FirstOrDefaultAsync();
```

**Current Codebase Status:**

- ✅ **No raw SQL queries** in the entire codebase
- ✅ All database access via EF Core LINQ
- ✅ All user inputs automatically parameterized

---

## Data Protection

### XSS Prevention (Cross-Site Scripting)

**Frontend:** Angular Framework

**Mechanism:** Angular automatically escapes all data bindings.

**Example (Safe):**

```html
<!-- Angular template (auto-escaped) -->
<div>{{ board.name }}</div>

<!-- Even if board.name contains <script>alert('XSS')</script>,
     Angular renders it as plain text, not executable code -->
```

**Dangerous Pattern (Avoided):**

```html
<!-- ❌ NEVER DO THIS (vulnerable to XSS) -->
<div [innerHTML]="board.name"></div>
```

**Sanitization Strategy:**

- Use `DomSanitizer` for user-generated content when displaying in UI
- Whitelist safe HTML if rich text features are added
- Strip `<script>`, `onclick`, and other dangerous patterns

**Current Codebase Status:**

- ✅ No `[innerHTML]` bindings without sanitization
- ✅ All user inputs rendered as plain text

### Data Encryption

**Database:**

- **At Rest:** PostgreSQL/SQL Server encrypted via OS-level encryption (LUKS, BitLocker)
- **In Transit:** TLS/SSL for database connections (configure in connection string)

**Connection String with TLS:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=pi-postgres;Database=PIPlanningDB;Username=postgres;Password=...;SSL Mode=Require;Trust Server Certificate=false;"
  }
}
```

**API:**

- **In Transit:** HTTPS/TLS via reverse proxy (Nginx, Caddy)
- **WebSocket (SignalR):** WSS (WebSocket Secure) over HTTPS

### Sensitive Data Handling

| Data Type                | Storage                | Encryption         | Retention                       |
| ------------------------ | ---------------------- | ------------------ | ------------------------------- |
| **Board Lock Passwords** | PostgreSQL (hashed)    | PBKDF2-HMAC-SHA256 | Permanent (until board deleted) |
| **Azure PATs**           | In-memory cache        | None (temporary)   | 10 minutes TTL                  |
| **Board Data**           | PostgreSQL (plaintext) | At-rest (OS-level) | Permanent                       |
| **Audit Logs**           | File system            | None               | 30 days (rotate)                |

**Recommendations:**

- ✅ Passwords are hashed (secure)
- ✅ PATs are temporary (secure)
- ⚠️ Board data is plaintext (acceptable for most use cases)

---

## Network Security

### HTTPS/TLS Configuration

**Production Requirements:**

- ✅ **Mandatory HTTPS** for all external traffic
- ✅ **TLS 1.2+** (disable TLS 1.0/1.1)
- ✅ **Valid SSL certificate** (Let's Encrypt, commercial CA)
- ✅ **HTTP → HTTPS redirect** (301 Moved Permanently)

**Nginx Configuration:**

```nginx
server {
    listen 80;
    server_name yourdomain.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name yourdomain.com;

    # SSL Certificate
    ssl_certificate /etc/nginx/ssl/cert.pem;
    ssl_certificate_key /etc/nginx/ssl/key.pem;

    # SSL Security
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers 'ECDHE-RSA-AES128-GCM-SHA256:ECDHE-RSA-AES256-GCM-SHA384';
    ssl_prefer_server_ciphers on;

    # Security Headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Frame-Options "DENY" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Content-Security-Policy "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';" always;

    # Proxy to backend
    location /api {
        proxy_pass http://pi-backend:8080;
    }

    # WebSocket (SignalR)
    location /hub/planning {
        proxy_pass http://pi-backend:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
    }
}
```

### Security Headers Explained

| Header                        | Value              | Purpose                       |
| ----------------------------- | ------------------ | ----------------------------- |
| **Strict-Transport-Security** | `max-age=31536000` | Force HTTPS for 1 year (HSTS) |
| **X-Frame-Options**           | `DENY`             | Prevent clickjacking attacks  |
| **X-Content-Type-Options**    | `nosniff`          | Prevent MIME type sniffing    |
| **X-XSS-Protection**          | `1; mode=block`    | Enable browser XSS filter     |
| **Content-Security-Policy**   | Varies             | Control resource loading      |

### Firewall Rules

**Recommended Firewall Configuration:**

```bash
# Allow HTTPS
sudo ufw allow 443/tcp

# Allow HTTP (for redirect to HTTPS)
sudo ufw allow 80/tcp

# Deny direct database access (internal only)
sudo ufw deny 5432/tcp

# Deny direct backend access (reverse proxy only)
sudo ufw deny 8080/tcp

# Enable firewall
sudo ufw enable
```

**Docker Network Isolation:**

```yaml
networks:
  pi-net:
    driver: bridge
    internal: false # Set to true to disable external access
```

---

## Azure DevOps Integration Security

### Personal Access Token (PAT) Handling

**Lifecycle:**

1. **User Entry**: User enters PAT in frontend form (import feature dialog)
2. **API Transmission**: PAT sent to backend via HTTPS POST request
3. **Validation**: Backend validates PAT by calling Azure DevOps API
4. **Temporary Storage**: PAT cached in-memory for 10 minutes (configurable)
5. **Expiration**: PAT removed from cache after TTL expires

**Storage Details:**

- **Technology**: ASP.NET Core `IMemoryCache`
- **TTL**: 10 minutes (configured via `PAT_TTL_MINUTES` environment variable)
- **Scope**: Per-board cache key (different PATs per board)
- **Persistence**: Never written to database or disk
- **Restart Behavior**: All cached PATs lost on application restart

**Security Benefits:**

- ✅ **Short-lived storage** (10 minutes)
- ✅ **No disk persistence** (memory only)
- ✅ **Per-board isolation** (PATs don't leak between boards)
- ⚠️ **No encryption in memory** (acceptable for temporary cache)

**Configuration:**

```yaml
# docker-compose.yml
frontend:
  environment:
    PAT_TTL_MINUTES: "10" # Adjust as needed (1-60 minutes)
```

### Azure API Validation

**Validation Endpoint:**

```csharp
// GET /api/v1/azure/feature/{org}/{project}/{featureId}
// Requires PAT in X-Azure-PAT header

public async Task<IActionResult> GetFeatureWithChildren(
    string organization,
    string project,
    int featureId,
    [FromHeader(Name = "X-Azure-PAT")] string pat)
{
    // Validate PAT by calling Azure DevOps API
    var feature = await _azureService.GetFeatureWithChildrenAsync(
        organization, project, featureId, pat);

    return Ok(feature);
}
```

**Error Handling:**

| Error Code           | Scenario                          | Response                                  |
| -------------------- | --------------------------------- | ----------------------------------------- |
| **400 Bad Request**  | Missing PAT or invalid parameters | `{"error": "PAT is required"}`            |
| **401 Unauthorized** | Invalid or expired PAT            | `{"error": "Invalid Azure PAT"}`          |
| **404 Not Found**    | Feature does not exist            | `{"error": "Feature not found"}`          |
| **403 Forbidden**    | PAT lacks required scope          | `{"error": "PAT missing vso.work scope"}` |

### Required Azure Scopes

**PAT Permissions:**

- **Work Items (Read)**: `vso.work`
- **Read-only access** to Features and User Stories

**Minimal Privilege Principle:**

- ✅ No write permissions required
- ✅ No admin permissions required
- ✅ Scope limited to Work Items only

**Create PAT in Azure DevOps:**

1. Go to **User Settings** → **Personal Access Tokens**
2. Click **New Token**
3. Set **Organization**: Select the organization
4. Set **Scopes**: **Work Items** → **Read**
5. Set **Expiration**: 90 days (or shorter)
6. Click **Create** and copy the token

---

## CORS Configuration

### Purpose

**CORS (Cross-Origin Resource Sharing)** controls which domains can access the API from a browser.

**Scenario:**

- Frontend hosted on `https://app.example.com`
- Backend API hosted on `https://api.example.com`
- Browser blocks API calls due to cross-origin policy
- CORS configuration allows the frontend domain

### Configuration Options

**File:** `backend/pi-planning-backend/appsettings.json`

**Option 1: Allow All Origins (Development Only):**

```json
{
  "Cors": {
    "AllowedOrigins": ["*"]
  }
}
```

**Option 2: Allow Specific Origins (Production):**

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://piplanning.example.com",
      "https://app.example.com"
    ]
  }
}
```

**Option 3: Block All (Except Localhost):**

```json
{
  "Cors": {
    "AllowedOrigins": []
  }
}
```

**Note:** Localhost (any port) is automatically allowed in all environments.

### Implementation Details

**File:** `backend/pi-planning-backend/Program.cs`

```csharp
bool IsCorsOriginAllowed(string origin)
{
    // Allow all if "*" is configured
    if (allowAllOrigins)
    {
        return true;
    }

    // Parse origin URL
    if (!Uri.TryCreate(origin, UriKind.Absolute, out Uri? originUri))
    {
        return false;
    }

    // Allow localhost (any port)
    if (originUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
        originUri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    // Check against configured origins
    return configuredOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed(IsCorsOriginAllowed)
            .AllowCredentials());  // Required for SignalR
});
```

### Security Implications

| Configuration                                 | Security | Use Case                      |
| --------------------------------------------- | -------- | ----------------------------- |
| `AllowedOrigins: ["*"]`                       | ⚠️ Low   | Development only              |
| `AllowedOrigins: ["https://app.example.com"]` | ✅ High  | Production (specific domains) |
| `AllowedOrigins: []`                          | ✅ High  | Localhost-only testing        |

**Production Recommendation:**

- ✅ Specify exact frontend domain(s)
- ✅ Use HTTPS origins only
- ❌ Never use `"*"` in production

---

## Audit & Logging

### Request Correlation

**Purpose:** Track requests across distributed components (frontend → backend → database).

**Implementation:**

**Middleware:** `RequestCorrelationMiddleware`

```csharp
public async Task InvokeAsync(HttpContext context)
{
    // Generate or extract correlation ID
    string correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
        ?? Guid.NewGuid().ToString();

    // Store in HttpContext for service access
    context.Items["CorrelationId"] = correlationId;

    // Add to response headers
    context.Response.Headers.Add("X-Correlation-ID", correlationId);

    await _next(context);
}
```

**Log Example:**

```
[14:30:15 INF] [CorrelationId: abc-123-def] Board created: Id=456, Name="PI 2024 Q2"
[14:30:16 INF] [CorrelationId: abc-123-def] Feature imported: FeatureId=789
```

### Structured Logging

**Technology:** Serilog (via ASP.NET Core ILogger)

**Log Levels:**

| Level           | Usage                   | Example                                   |
| --------------- | ----------------------- | ----------------------------------------- |
| **Trace**       | Detailed debugging      | `DbCommand executed in 5ms`               |
| **Debug**       | Development diagnostics | `PAT cached for board 123`                |
| **Information** | General events          | `Board created: Id=456`                   |
| **Warning**     | Potential issues        | `Board finalization has 3 warnings`       |
| **Error**       | Recoverable errors      | `Azure API call failed: 401 Unauthorized` |
| **Critical**    | Unrecoverable errors    | `Database migration failed`               |

**Configuration:**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

### Security Event Logging

**Recommended Events to Log:**

| Event                  | Log Level   | Example                                        |
| ---------------------- | ----------- | ---------------------------------------------- |
| Board Locked           | Information | `[INFO] Board 123 locked by user "Alice"`      |
| Board Unlocked         | Information | `[INFO] Board 123 unlocked successfully`       |
| Failed Unlock Attempt  | Warning     | `[WARN] Failed unlock attempt for board 123`   |
| PAT Validation Failure | Warning     | `[WARN] Invalid Azure PAT for board 123`       |
| CORS Rejection         | Warning     | `[WARN] CORS blocked origin: https://evil.com` |
| Database Error         | Error       | `[ERROR] Database connection failed`           |
| Migration Failure      | Critical    | `[CRITICAL] Migration failed: ...`             |

**Future Enhancements:**

- Track failed unlock attempts per IP address (rate limiting)
- Alert on multiple failed PAT validations (brute force detection)
- Log all board deletions (audit trail)

---

## Security Best Practices

### Deployment Security

**1. Change Default Database Password:**

```bash
# docker-compose.yml
POSTGRES_PASSWORD: "YourStrong!Passw0rd"  # ❌ Change this!
```

**2. Use Docker Secrets (Production):**

```yaml
services:
  db:
    environment:
      POSTGRES_PASSWORD_FILE: /run/secrets/db_password
    secrets:
      - db_password

secrets:
  db_password:
    file: ./secrets/db_password.txt
```

**3. Disable Swagger in Production:**

```json
{
  "Swagger": {
    "Enabled": false
  }
}
```

**4. Set Environment to Production:**

```yaml
backend:
  environment:
    ASPNETCORE_ENVIRONMENT: "Production"
```

**5. Remove Database Port Exposure:**

```yaml
# Remove this in production:
# ports:
#   - "5432:5432"
```

### API Security

**1. Implement Rate Limiting:**

Use Nginx `limit_req` or ASP.NET Core rate limiting middleware:

```nginx
http {
    limit_req_zone $binary_remote_addr zone=api:10m rate=10r/s;

    server {
        location /api {
            limit_req zone=api burst=20 nodelay;
            proxy_pass http://backend;
        }
    }
}
```

**2. Add API Key Authentication (Optional):**

```csharp
// Middleware to validate API key
app.Use(async (context, next) =>
{
    if (!context.Request.Headers.TryGetValue("X-API-Key", out var apiKey)
        || apiKey != "your-secret-api-key")
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Unauthorized");
        return;
    }
    await next();
});
```

**3. Implement Request Size Limits:**

```json
{
  "Kestrel": {
    "Limits": {
      "MaxRequestBodySize": 10485760 // 10 MB
    }
  }
}
```

### Database Security

**1. Use Least Privilege DB User:**

```sql
-- Create dedicated user (not postgres superuser)
CREATE USER piuser WITH PASSWORD 'strongpassword';
GRANT CONNECT ON DATABASE PIPlanningDB TO piuser;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO piuser;
REVOKE CREATE ON SCHEMA public FROM piuser;
```

**2. Enable SSL for Database Connections:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=pi-postgres;Database=PIPlanningDB;Username=piuser;Password=...;SSL Mode=Require;"
  }
}
```

**3. Regular Backups:**

```bash
# Automated daily backup
0 2 * * * /path/to/backup-script.sh
```

---

## Vulnerability Disclosure

### Reporting Security Issues

**Contact:**

- **Email:** security@yourcompany.com
- **GitHub:** Open a private security advisory (not public issue)

**Response SLA:**

- **First Response:** Within 48 hours
- **Patch Release:** Within 30 days (depending on severity)

**Please Include:**

- Description of the vulnerability
- Steps to reproduce
- Affected versions
- Suggested fix (if known)

### Security Updates

**GitHub Security Advisories:**

- [View Security Advisories](https://github.com/yourusername/pi-planning-tool/security/advisories)

**Changelog:**

- Check the repository commit history and release notes for security-related changes

---

## Compliance & Standards

### Standards Alignment

| Standard                         | Compliance Status | Notes                                                           |
| -------------------------------- | ----------------- | --------------------------------------------------------------- |
| **OWASP Top 10**                 | ✅ Partial        | Addressed: SQL Injection, XSS, Security Misconfiguration        |
| **NIST Cybersecurity Framework** | ⚠️ In Progress    | Identify, Protect implemented; Detect, Respond, Recover pending |
| **ISO 27001**                    | ❌ Not Certified  | Internal use only (not customer-facing)                         |
| **GDPR**                         | ⚠️ Depends        | No PII collected unless entered in notes                        |
| **SOC 2**                        | ❌ Not Certified  | Future consideration for SaaS offering                          |

### OWASP Top 10 Coverage

| Risk                                 | Status       | Mitigation                                 |
| ------------------------------------ | ------------ | ------------------------------------------ |
| **A01: Broken Access Control**       | ✅ Mitigated | Board-level password protection            |
| **A02: Cryptographic Failures**      | ✅ Mitigated | PBKDF2 password hashing, HTTPS             |
| **A03: Injection**                   | ✅ Mitigated | EF Core parameterized queries              |
| **A04: Insecure Design**             | ✅ Mitigated | Defense-in-depth, least privilege          |
| **A05: Security Misconfiguration**   | ⚠️ Partial   | Swagger disabled in prod, CORS configured  |
| **A06: Vulnerable Components**       | ⚠️ Ongoing   | Dependabot enabled, regular updates        |
| **A07: Auth & Session Failures**     | ✅ N/A       | No user authentication system              |
| **A08: Software & Data Integrity**   | ⚠️ Partial   | No code signing (future enhancement)       |
| **A09: Security Logging Failures**   | ⚠️ Partial   | Correlation IDs implemented, SIEM pending  |
| **A10: Server-Side Request Forgery** | ✅ Mitigated | No user-controlled URLs (except Azure API) |

---

## Additional Resources

### Related Documentation

- [User Guide](USER_GUIDE.md): End-user documentation
- [API Reference](API_REFERENCE.md): REST API and SignalR documentation
- [Architecture Guide](ARCHITECTURE.md): System design and patterns
- [Configuration Guide](CONFIGURATION.md): Environment variables and settings
- [Docker Deployment Guide](DOCKER_DEPLOYMENT_GUIDE.md): Container deployment
- [IIS Deployment Guide](IIS_DEPLOYMENT_GUIDE.md): Windows deployment

### Security Tools

**Recommended Tools:**

- **Static Analysis**: SonarQube, CodeQL
- **Dependency Scanning**: Dependabot, Snyk
- **Container Scanning**: Trivy, Clair
- **Penetration Testing**: Burp Suite, OWASP ZAP
- **Secrets Detection**: TruffleHog, GitGuardian

### Security Checklist

**Pre-Deployment:**

- [ ] Change default database password
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Configure CORS for production domain
- [ ] Disable Swagger
- [ ] Enable HTTPS/TLS
- [ ] Remove database port exposure
- [ ] Set resource limits (CPU/memory)
- [ ] Enable automated backups
- [ ] Configure log rotation
- [ ] Review security headers

**Post-Deployment:**

- [ ] Run vulnerability scan
- [ ] Test HTTPS certificate
- [ ] Verify CORS configuration
- [ ] Test password locking/unlocking
- [ ] Verify audit logs are working
- [ ] Set up monitoring alerts
- [ ] Document incident response plan

---

## Changelog

### Version 1.0 (March 2026)

- Initial security guide documentation
- PBKDF2 password hashing detailed
- Input validation and SQL injection prevention documented
- CORS configuration explained
- Azure PAT handling security documented
- Network security best practices (HTTPS/TLS, firewalls)
- Audit logging with correlation IDs
- OWASP Top 10 compliance matrix
- Security checklist for deployment

---

**Questions or security concerns?** Contact security@yourcompany.com or open a private GitHub security advisory.

**Stay Secure! 🔒**
