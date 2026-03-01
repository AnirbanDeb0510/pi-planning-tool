# PI Planning Tool - Current Roadmap & Priorities

**Last Updated:** March 2, 2026  
**Current Status:** Phase 7 COMPLETE ✅  
**Current Branch:** `main`  
**Next Phase:** Phase 8 - Documentation & Integration Testing

---

## 🚀 NEXT PRIORITIES (Ordered by Dependency & Impact)

**Immediate Next Steps:**

1. **Phase 8** - Documentation & Integration Testing (3-4 hrs)
2. **Phase 9** - Cloud/Hosting Deployment (1-2 days)
3. **Phase 10** - Provider-Isolated EF Core Migrations (1-2 days, post-demo hardening)

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

### Phase 7 — Board Lock/Unlock Feature

- Completed on Mar 2, 2026
- Backend: Password-based lock/unlock endpoints with PBKDF2 hashing
- Frontend: Lock/unlock modals with password validation and error handling
- UI: Redesigned board header with status badges and action buttons
- Real-time: SignalR broadcasts for lock state changes
- Validation: All mutations blocked when board is locked (403 enforcement)
- UX: Error banners, disabled controls, dark mode support
- Quality: All strings externalized to constants, clean build/lint

---

## 📋 UPCOMING PHASES

### PHASE 7: Board Lock/Unlock Feature — ✅ COMPLETE

**Status:** Complete ✅  
**Completed:** March 2, 2026  
**Total Time:** ~4 hours  
**Depends On:** Phase 4, 5, 6
**Why:** Enables board state control; provides complete workflow control alongside Finalization

#### Core Concept: Password-Protected Lock

**Password Persistence Model:** Password is a PERSISTENT board property that survives lock/unlock cycles. Once set during first lock, it remains for authentication in future lock operations.

#### Design Pattern:

**What's Different from Finalization?**

- **Finalization:** Locks board for analysis, allows impact testing with story movement
- **Lock:** Complete read-only lock, no changes allowed at all
- **State:** Board can be BOTH finalized AND locked simultaneously (separate states)

**Lock Operation Workflow:**

1. **Scenario A: Board has NO password yet**
   - User clicks "Lock Board" button
   - Modal: "Set password to lock this board"
   - Input: Password + Confirm Password
   - Action: Hash password, set `IsLocked = true`, store hash in `PasswordHash`
   - Result: Board locked with new password

2. **Scenario B: Board ALREADY has password**
   - User clicks "Lock Board" button
   - Modal: "Enter password to lock board"
   - Input: Password (single field)
   - Action: Verify password against existing `PasswordHash`
   - If valid: Set `IsLocked = true` (keep existing password)
   - If invalid: Show error "Invalid password"
   - Result: Board locked with same password

**Unlock Operation Workflow:**

- User clicks "Unlock Board" button
- Modal: "Enter password to unlock board"
- Input: Password
- Action: Verify password against `PasswordHash`
- If valid: Set `IsLocked = false` (KEEP password hash for future locks)
- If invalid: Show error "Invalid password"
- Result: Board unlocked but password persists

**Board Load Workflow (View-Only):**

- User loads board → No password prompt during load
- Load preview → Show PAT modal (if needed for Azure features)
- Load full board data
- Display in view-only mode if `IsLocked = true`

#### Backend Implementation:

**TASK 7.1: Create DTOs** ✅ COMPLETE

- [x] `BoardLockDto` - `{ password: string }`
- [x] `BoardUnlockDto` - `{ password: string }`
- [x] Response DTOs - `BoardSummaryDto` returned with success status

**TASK 7.2: Implement Lock/Unlock Endpoints** ✅ COMPLETE

- [x] `PATCH /api/boards/{id}/lock` in `BoardsController.cs`
  - Scenario A (no password): Hash new password, set `IsLocked = true` ✅
  - Scenario B (password exists): Verify password, set `IsLocked = true` ✅
  - Broadcast SignalR event: `"BoardLockStateChanged"` with `{ boardId, isLocked: true, timestampUtc }` ✅
- [x] `PATCH /api/boards/{id}/unlock` in `BoardsController.cs`
  - Verify password against `PasswordHash` ✅
  - Set `IsLocked = false` (keep `PasswordHash`) ✅
  - Broadcast SignalR event: `"BoardLockStateChanged"` with `{ boardId, isLocked: false, timestampUtc }` ✅

**TASK 7.3: Add Lock Validation** ✅ COMPLETE

- [x] Create `ValidateBoardNotLocked()` method in `ValidationService.cs`
- [x] Apply validation to all mutations:
  - `FeatureService`: import, refresh, delete, reorder ✅
  - `UserStoryService`: move stories ✅
  - `TeamService`: add, update, delete members; update capacity ✅
  - `BoardService`: finalize, restore ✅
- [x] Return 403 Forbidden error if board is locked (via `UnauthorizedAccessException`)

**TASK 7.4: Add SignalR Events** ✅ COMPLETE

- [x] Create `BoardLockStateChangedDto` DTO (unified lock/unlock event)
- [x] Broadcast `"BoardLockStateChanged"` event when board locked
- [x] Broadcast `"BoardLockStateChanged"` event when board unlocked

#### Frontend Implementation:

**TASK 7.5: Create Lock/Unlock Modals** ✅ COMPLETE

- [x] `LockBoardModal` component with password inputs in `board-modals` component
  - Set password form when no password exists (password + confirm)
  - Enter password form when password exists (single field)
  - Password validation with mismatch checking
  - Error messages displayed for invalid passwords
- [x] `UnlockBoardModal` component with password input
  - Single password field
  - Display error on invalid password
  - Modal-level error handling (no navigation away)

**TASK 7.6: Add Lock/Unlock UI to Board Header** ✅ COMPLETE

- [x] Redesigned `board-header` component with modern layout
  - LEFT section: Dev/Test toggle + Status badges (Locked, Finalized)
  - RIGHT section: Action buttons (Finalize/Restore, Lock/Unlock, Refresh)
  - Lock/Unlock button toggles based on `board().isLocked` state
  - Icons: `lock` (when unlocked) / `lock_open` (when locked)
  - Click handlers open appropriate modals
- [x] Button disabled states properly handled
  - Finalize disabled when locked
  - Restore disabled when locked OR during loading
  - Refresh disabled during loading
- [x] Status badges show lock/finalized states independently
  - Red "Locked" badge with lock icon
  - Green "Finalized" badge with check icon
  - Both can appear simultaneously

**TASK 7.7: Add Real-Time Lock State Updates** ✅ COMPLETE

- [x] Subscribe to SignalR event: `"BoardLocked"`
  - Update board state: `isLocked = true`
  - Real-time UI update (badge appears, buttons disable)
- [x] Subscribe to SignalR event: `"BoardUnlocked"`
  - Update board state: `isLocked = false`
  - Real-time UI update (badge disappears, buttons enable)
- [x] SignalR connection ID added to request headers to exclude initiator

**TASK 7.8: Add Refresh Button** ✅ COMPLETE

- [x] Refresh button in `board-header` component
  - Icon: `refresh` with spinning animation during load (CSS keyframes)
  - Click handler: `this.boardService.loadBoard(boardId)`
  - Disabled during loading
  - Positioned as rightmost button in header

**TASK 7.9: Block Operations on Locked Board** ✅ COMPLETE

- [x] All mutation operations return 403 when board is locked
- [x] UI controls disabled when `isLocked = true`:
  - Add feature button disabled
  - Add team member button disabled
  - Edit/delete buttons disabled
  - Drag-drop disabled via `[cdkDragDisabled]` binding
  - Finalize/restore buttons disabled
- [x] Error handling without navigation away:
  - Created error banner below header for finalize/restore errors
  - Lock/unlock errors shown in modals
  - HTTP client extracts `error.details` for specific messages
  - GlobalExceptionHandlingMiddleware returns 403 for UnauthorizedAccessException
- [x] Dark mode support for all new UI elements

#### Acceptance Criteria: ✅ ALL VERIFIED

- [x] Lock endpoint works - Scenario A (new password) creates and locks
- [x] Lock endpoint works - Scenario B (existing password) validates and locks
- [x] Unlock endpoint validates password and removes lock flag
- [x] Password persists across lock/unlock cycles
- [x] Invalid password attempts rejected with 403 Forbidden
- [x] All mutation operations blocked when locked (return 403)
- [x] Locked board shows lock badge independent from finalized state
- [x] Refresh button reloads board data without page refresh
- [x] SignalR broadcasts lock/unlock events to all connected users
- [x] Real-time UI updates when lock status changes via SignalR
- [x] Board can be locked without being finalized
- [x] Board can be finalized without being locked
- [x] Board can be both finalized AND locked simultaneously
- [x] Load board does NOT require password (read-only by default)
- [x] All UI controls disabled when board is locked
- [x] Build: 0 errors, 0 warnings (888.62 kB bundle)
- [x] Lint: Clean (0 errors, 0 warnings)
- [x] Manual testing: Complete and verified

---

### PHASE 8: Documentation & Integration Testing — WRAP-UP

**Status:** Not Started  
**Estimated Time:** 3-4 hours  
**Depends On:** All other phases complete
**Why:** Ensure comprehensive documentation and real-world integration testing

#### Work Items:

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
  **Mitigation:** Use clean test DBs for re-baseline and preserve production DBs until controlled clover.

- **Risk:** Team accidentally uses old migration commands  
  **Mitigation:** Add command snippets in README/guide + CI guard checks.

### Proposed Execution Order

1. Scaffold migration projects + wire `MigrationsAssembly`.
2. Regenerate baseline migrations per provider.
3. Validate on local Docker (PostgreSQL) and Windows IIS (SQL Server).
4. Update docs and CI checks.
5. Merge as post-demo hardening milestone.
