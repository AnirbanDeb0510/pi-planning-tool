# PI Planning Tool - Current Roadmap & Priorities

**Last Updated:** March 1, 2026  
**Current Status:** Phase 6 COMPLETE ✅ (All milestones A + B + C complete, manual testing validated)  
**Current Branch:** `main`  
**Next Phase:** Phase 7 - Board Lock/Unlock Endpoints

---

## 🚀 NEXT PRIORITIES (Ordered by Dependency & Impact)

**Immediate Next Steps:**

1. **Phase 7** - Board Lock/Unlock Endpoints (2-3 hrs)
2. **Phase 8** - Documentation & Integration Testing (3-4 hrs)
3. **Phase 9** - Cloud/Hosting Deployment (1-2 days)
4. **Phase 10** - Provider-Isolated EF Core Migrations (1-2 days, post-demo hardening)

## ✅ COMPLETED PHASES (Summary)

### Phase 4 & 4.6 — Backend Refactoring + Code Quality

- Completed on Feb 22 and Feb 26, 2026
- Core backend refactoring, validation, logging, transactions, and code quality pass completed

### Phase 5 — IIS + SQL Server Support

- Completed on Feb 28, 2026
- Dual-provider support validated (PostgreSQL + SQL Server), deployment guide added

### Phase 5.5 — User Name Persistence + Guard

- Completed on Mar 1, 2026
- sessionStorage persistence + route guard flow delivered

### Phase 6 — SignalR Real-time Collaboration

- Completed on Mar 1, 2026
- Milestones A/B/C delivered and manually validated
- Production-ready: presence, cursor sync, and all mutation broadcasts

---

## 📋 UPCOMING PHASES

### PHASE 7: Board Lock/Unlock Endpoints — NEXT PRIORITY

**Status:** Not Started  
**Estimated Time:** 2-3 hours  
**Depends On:** Phase 4, 5, 6
**Why:** Enables board state control; provides complete workflow control alongside Finalization

#### What's Different from Finalization?

- **Finalization:** Locks board for analysis, allows impact testing with story movement
- **Lock:** Complete read-only lock, no changes allowed at all
- **State:** Board can be BOTH finalized AND locked simultaneously (separate states)

#### Backend Changes:

- `Controllers/BoardsController.cs` - Add new endpoints
  - `PATCH /api/boards/{id}/lock` - Lock board (set IsLocked flag)
  - `PATCH /api/boards/{id}/unlock` - Unlock board
- `Models/Board.cs` - Add `IsLocked` property (if not exists)
- `Services/Implementations/BoardService.cs` - Add lock/unlock methods
- `Services/Interfaces/IBoardService.cs` - Update interface

#### Frontend Changes:

- Add lock/unlock buttons to board header
- Show locked state indicator (independent from finalized banner)
- Block all operations when board is locked

#### Acceptance Criteria:

- [ ] Lock endpoint sets IsLocked flag in database
- [ ] Unlock endpoint clears IsLocked flag
- [ ] UI shows locked state clearly (separate from finalized state)
- [ ] All operations blocked when locked (add/edit/delete/move)
- [ ] Board can be both finalized AND locked
- [ ] Build: 0 errors

---

### PHASE 8: Documentation & Integration Testing — WRAP-UP

**Status:** Not Started  
**Estimated Time:** 3-4 hours  
**Depends On:** All other phases complete
**Why:** Ensure comprehensive documentation and real-world integration testing

#### Workstreams:

- [ ] **Architecture docs** (ERD, service flow, component hierarchy, SignalR flow)
- [ ] **API docs** (endpoints, request/response examples, error handling, auth notes)
- [ ] **Deployment docs** (Docker + PostgreSQL, IIS + SQL Server, SSL/HTTPS notes)
- [ ] **User guide** (board lifecycle, planning flow, finalize/restore, lock/unlock)
- [ ] **Code docs** (README refresh + key service behavior notes)
- [ ] **Integration testing** (end-to-end board flow + multi-user scenarios)
- [ ] **Performance check** (concurrency + query behavior + SignalR throughput)
- [ ] **Security review** (input validation, CORS, PAT handling, auth surface)

#### Acceptance Criteria:

- [ ] All features documented (user-facing)
- [ ] All APIs documented (developer-facing)
- [ ] Deployment guides complete (Docker + IIS)
- [ ] Integration testing complete (all major workflows)
- [ ] Performance benchmarks documented
- [ ] Security audit completed with findings documented
- [ ] README.md comprehensive and current
- [ ] Zero issues blocking production deployment

---

### PHASE 9: Cloud/Hosting Deployment — NEXT AFTER DOCS

**Status:** Not Started  
**Estimated Time:** 1-2 days  
**Depends On:** Phase 7, 8  
**Why:** Make the product publicly accessible in a stable hosted environment beyond local Docker/IIS setups

#### Scope:

- [ ] Select target hosting path (e.g., Azure App Service, AWS ECS/Fargate, or Render/Railway)
- [ ] Deploy backend API with environment-based configuration and secure secret handling
- [ ] Deploy frontend as static hosting (or reverse-proxy path) with runtime API base URL
- [ ] Provision managed database (PostgreSQL/SQL Server based on deployment choice)
- [ ] Configure domain, HTTPS/TLS, and CORS for hosted endpoints
- [ ] Add health checks, basic monitoring/logging, and restart policy
- [ ] Publish deployment runbook for repeatable releases

#### Acceptance Criteria:

- [ ] Frontend and backend are reachable via hosted URLs
- [ ] End-to-end board workflow works in hosted environment
- [ ] SignalR real-time events work across multiple external clients
- [ ] Secrets are not hard-coded and are managed via platform secret store
- [ ] HTTPS enabled with valid certificate
- [ ] Basic monitoring/log access available for troubleshooting

---

## 📋 DECISIONS & RESOLUTIONS

### Active decisions (kept current)

**Phase 7**

- Finalized and locked are separate states (board can be finalized-only, locked-only, or both)

**Phase 8**

- Scope remains documentation + integration testing + performance/security checks

**Phase 9**

- Cloud-first deployment on a managed hosting platform with HTTPS and managed secrets

**Phase 10**

- Use provider-isolated migration assemblies to avoid cross-provider migration issues

---

## 🔄 Current Dependency Chain

`7 → 8 → 9 → 10`

- Phase 7: Board lock/unlock endpoints
- Phase 8: Documentation and integration validation
- Phase 9: Cloud/hosting deployment
- Phase 10: Migration hardening (post-demo)

---

## 🛡️ FUTURE SCOPE: Multi-Provider Migration Hardening

### PHASE 10: Provider-Isolated EF Core Migrations (Industry Standard)

**Status:** Planned (post-demo)  
**Priority:** High (stability + maintainability)  
**Estimated Time:** 1-2 days  
**Why now:** Demo path works, but current dual-provider migration setup can still cause provider cross-over risks when migrations are generated/applied in mixed environments.

### Problem Context (Current Limitation)

- Using `-o Migrations_SqlServer` only changes output folder; it does **not** isolate migration discovery at runtime.
- EF Core can still discover migration metadata from the same assembly/context path.
- This creates risk of PostgreSQL migration SQL being attempted on SQL Server (or vice versa).
- Team has already observed this class of issue during Windows validation.

### Industry-Standard Target Architecture

- Keep single backend runtime project (`pi-planning-backend`) for app logic.
- Split migrations into separate migration assemblies/projects:
  - `pi-planning-backend.migrations.postgres`
  - `pi-planning-backend.migrations.sqlserver`
- In `Program.cs`, choose both provider **and** `MigrationsAssembly(...)` based on `DatabaseProvider`.
- Keep `db.Database.Migrate()` in startup; it will apply only migrations from the selected provider assembly.

---

### **TASK 10.1: Create Separate Migration Projects**

- [ ] Create two class library projects for migrations only (PostgreSQL + SQL Server).
- [ ] Add references from each migration project to backend project (for `AppDbContext` and entities).
- [ ] Add required EF provider package per migration project.

### **TASK 10.2: Wire Provider-Specific Migration Assemblies in Runtime**

- [ ] Update `Program.cs` DbContext registration:
  - PostgreSQL path: `UseNpgsql(connectionString, x => x.MigrationsAssembly("...postgres"))`
  - SQL Server path: `UseSqlServer(connectionString, x => x.MigrationsAssembly("...sqlserver"))`
- [ ] Keep existing `DatabaseProvider` config switch.
- [ ] Validate startup logs clearly show active provider + migration assembly.

### **TASK 10.3: Re-Scaffold Baseline Migrations per Provider**

- [ ] Generate baseline migration in postgres migration project.
- [ ] Generate baseline migration in sqlserver migration project.
- [ ] Ensure each project has its own `AppDbContextModelSnapshot.cs`.
- [ ] Remove legacy mixed migration ambiguity from backend project after verification.

### **TASK 10.4: Update Commands/Runbooks**

- [ ] Update `IIS_DEPLOYMENT_GUIDE.md` with provider-specific EF commands using:
  - `--project <migrations-project>`
  - `--startup-project <backend-project>`
- [ ] Update local dev and Docker instructions for PostgreSQL migration flow.
- [ ] Add a short “Do/Don’t” section to prevent using folder-only migration separation.

### **TASK 10.5: Add Validation Gates (CI + Manual)**

- [ ] CI check: generate/apply migrations for PostgreSQL path in isolated test DB.
- [ ] CI check: generate/apply migrations for SQL Server path in isolated test DB.
- [ ] Manual smoke tests:
  - Backend startup + auto-migrate works for each provider.
  - `__EFMigrationsHistory` contains only expected provider migration set.

---

### Acceptance Criteria for Phase 10

- [ ] Provider-specific migration assemblies exist and are used by runtime.
- [ ] No provider cross-over during `dotnet ef migrations add` / `database update`.
- [ ] SQL Server and PostgreSQL migrations can be generated independently without config hacks.
- [ ] Startup migration succeeds cleanly for both deployment paths.
- [ ] Documentation updated with exact provider-specific command examples.

### Risks & Mitigations

- **Risk:** Existing migration history mismatch in test environments  
  **Mitigation:** Use clean test DBs for re-baseline and preserve production DBs until controlled cutover.

- **Risk:** Team accidentally uses old migration commands  
  **Mitigation:** Add command snippets in README/guide + CI guard checks.

### Proposed Execution Order

1. Scaffold migration projects + wire `MigrationsAssembly`.
2. Regenerate baseline migrations per provider.
3. Validate on local Docker (PostgreSQL) and Windows IIS (SQL Server).
4. Update docs and CI checks.
5. Merge as post-demo hardening milestone.
