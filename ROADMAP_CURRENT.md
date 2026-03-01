# PI Planning Tool - Current Roadmap & Priorities

**Last Updated:** March 1, 2026  
**Current Status:** Phase 5.5 complete ✅ - Ready for Phase 6 (SignalR Real-time Collaboration)  
**Current Branch:** `main`  
**Next Phase:** SignalR implementation (13 broadcast events + presence tracking)

---

## 🚀 NEXT PRIORITIES (Ordered by Dependency & Impact)

**Immediate Next Steps:**

1. ✅ **Phase 5.5** - User Name Persistence & Route Guard - **COMPLETE** (sessionStorage + guard implemented)
2. **Phase 6** - SignalR Real-time Collaboration (6-8 hrs) - 13 broadcast events + presence tracking
3. **Phase 7** - Board Lock/Unlock Endpoints (2-3 hrs)
4. **Phase 8** - Documentation & Integration Testing (3-4 hrs)

### ✅ PHASE 4 & 4.6: Backend Refactoring & Code Quality — COMPLETE

**Backend (Phase 4):** Finished February 22, 2026  
**Code Quality (Phase 4.6):** Finished February 26, 2026

#### Phase 4 Deliverables (Backend Foundation):

- ✅ Standardized constructor injection (primary constructor pattern)
- ✅ SprintService extracted with full lifecycle management
- ✅ RequestCorrelationMiddleware for request tracing (X-Correlation-ID)
- ✅ ValidationService with 7 centralized validation methods
- ✅ 20+ structured logs with CorrelationId enrichment across services
- ✅ 6 critical multi-step operations wrapped in atomic transactions
- ✅ PasswordHelper refactored with industry-standard PBKDF2 hashing
- ✅ Build: 0 errors, all tests passing

#### Phase 4.6 Deliverables (Code Quality):

- ✅ Frontend: ESLint configured + `npm run lint` → 0 warnings/errors
- ✅ Backend: .NET code formatting + `dotnet build` → 0 warnings/errors
- ✅ Type Safety: Removed all `any` types, explicit signal typing, proper error handling
- ✅ Angular Modern Patterns: Migrated `*ngIf/*ngFor` to `@if/@for` control flow
- ✅ Service Interfaces: Created 5 API service interfaces + typed all injections
- ✅ Formatter Alignment: Prettier configured for HTML + EditorConfig for C#

---

## 📋 UPCOMING PHASES

### ✅ PHASE 5: Windows IIS + SQL Server Support — COMPLETE

**Status:** Completed (demo scope validated)  
**Completed On:** February 28, 2026  
**Estimated Time:** 5-6 hours  
**Depends On:** Phase 4 & Phase 4.5 & Phase 4.6 (all complete)  
**Why:** Enables flexible deployment (Docker + IIS); required for stakeholder demo; shows production readiness  
**Approach:** Single `DefaultConnection` key + `DatabaseProvider` config key (switches between PostgreSQL & SQL Server)  
**Frontend:** Same env.js approach for both Docker & IIS (Docker generates dynamically, IIS uses static)

_Objective:_ Enable deployment to Windows IIS server with SQL Server database support (in addition to Docker + PostgreSQL).

**Completion Notes (demo validation):**

- SQL Server migration flow was executed and validated on Windows.
- Backend startup + API checks passed with `DatabaseProvider=SqlServer`.
- `__EFMigrationsHistory` and table creation were validated.
- IIS deployment flow (backend + frontend as IIS applications) was documented and tested.
- Post-demo hardening for provider-isolated migration assemblies is planned under **Phase 9**.

---

#### **TASK 1: Add SQL Server NuGet Package** (20 min)

- [x] Update `.csproj`:
  - Add `<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.10" />`
  - Verify: `dotnet build` succeeds with no new warnings

---

#### **TASK 2: Update appsettings Files with Commented SQL Server Support** (15 min)

_Files to change:_

- [x] `appsettings.json` - Keep PostgreSQL as default with commented SQL Server example
  - Add `"DatabaseProvider": "PostgreSQL"` (config key for Program.cs to read)
  - Keep existing PostgreSQL connection string in `DefaultConnection`
  - **ADD COMMENTED SECTION:** SQL Server example connection string in comments
- [x] `appsettings.Development.json` - PostgreSQL dev environment
  - Add `"DatabaseProvider": "PostgreSQL"`
  - Keep existing PostgreSQL connection string
  - **ADD COMMENTED SECTION:** SQL Server optional override example

- [x] IIS deployment uses `appsettings.json` as manual override source
  - Before IIS publish: set `"DatabaseProvider": "SqlServer"` in `appsettings.json`
  - Update `ConnectionStrings:DefaultConnection` to SQL Server connection string
  - Keep `appsettings.Development.json` for local Docker/dev defaults

**Comment format example:**

```json
// ========== FOR SQL SERVER DEPLOYMENT ==========
// If deploying to Windows IIS with SQL Server, uncomment below and update connection string:
// "DatabaseProvider": "SqlServer",
// "ConnectionStrings": {
//   "DefaultConnection": "Server=localhost;Database=PIPlanningDB;User Id=sa;Password=YourPassword;Encrypt=false;TrustServerCertificate=true;"
// }
// ================================================
```

---

#### **TASK 3: Update Program.cs - Conditional Database Provider** (30 min)

- [x] Add inline logic in Program.cs (before `AddDbContext`):

  ```
  Read DatabaseProvider from config (default: "PostgreSQL")
  Read DefaultConnection string

  If provider == "SqlServer":
    options.UseSqlServer(connectionString)
  Else:
    options.UseNpgsql(connectionString)

  Log which provider is active
  ```

- [x] Auto-migration already in place - verify it works for both databases
- [x] Test: `dotnet build` → 0 errors, 0 warnings

---

#### **TASK 4: Create SQL Server Migration Set** (45 min)

- [x] Create new folder: `Migrations_SqlServer/` in same directory as current `Migrations/`
- [x] Generate SQL Server migration:

  ```
  dotnet ef migrations add InitialCreate_SqlServer -o Migrations_SqlServer
  ```

- [x] Verify generated migration files:
  - `InitialCreate_SqlServer.cs`
  - `InitialCreate_SqlServer.Designer.cs`
  - Schema matches PostgreSQL version

- [x] **IMPORTANT:** Configure EF Core to use correct migration folder per provider
  - May need custom `DbContextFactory` or conditional context paths (research if needed)
  - **Or:** Keep both migrations, handle in Program.cs with conditional logic

- [x] Test migration locally:
  - Create new SQL Server 2016 database (local)
  - Connect app with `DatabaseProvider: SqlServer` config
  - Verify `dotnet ef database update` works correctly
  - Check all tables created with correct schema

---

#### **TASK 5: Prepare appsettings.json for IIS deployment** (10 min)

- [x] Use `appsettings.json` directly for IIS deployment:

  ```json
  {
    "DatabaseProvider": "SqlServer",
    "ConnectionStrings": {
      "DefaultConnection": "Server=YOUR_SERVER;Database=PIPlanningDB;User Id=sa;Password=YOUR_PASSWORD;Encrypt=false;TrustServerCertificate=true;"
    }
  }
  ```

- [x] Keep SQL Server example under `Comments` in `appsettings.json` so deployment users can copy/paste quickly

---

#### **TASK 6: Verify Frontend env.js Configuration** (10 min)

_No code changes needed, just verification:_

- [x] Confirm `public/env.js` and `RuntimeConfig.load()` work correctly
  - For Docker: `docker-entrypoint.sh` generates env.js at startup (existing flow)
  - For IIS: Users manually edit `env.js` before deployment:
    ```javascript
    window["__env"] = window["__env"] || {};
    window["__env"]["apiBaseUrl"] = "http://your-ip:5262"; // or localhost, or hostname
    window["__env"]["patTtlMinutes"] = "10";
    ```

- [x] No Angular build changes needed - env.js is served separately

---

#### **TASK 7: Create IIS_DEPLOYMENT_GUIDE.md** (1 hour)

- [x] **Prerequisites Section:**
  - Windows Server 2022+ (or Windows 10/11 with IIS enabled)
  - .NET 8 Hosting Bundle installed
  - SQL Server 2016+ (local or remote)
  - Git installed (for cloning repo)

- [x] **Quick Start (6 steps):**
  1. Clone repo and navigate to `backend/pi-planning-backend/`
  2. Edit `appsettings.json`: Set `DatabaseProvider=SqlServer` + SQL Server connection string
  3. Build: `dotnet build -c Release`
  4. Publish: `dotnet publish -c Release -o ./publish`
  5. Copy `./publish` folder to IIS directory (e.g., `C:\inetpub\wwwroot\api\`)

- [x] **IIS Configuration (step-by-step with screenshots):**
  1. Open IIS Manager → Create Application Pool
  2. Settings: .NET CLR version = "No Managed Code", Identity = "ApplicationPoolIdentity"
  3. Create Website pointing to publish folder
  4. Bind to port 5262 (or your port)
  5. Test: `http://localhost:5262/swagger` (should show API docs)

- [x] **Frontend Deployment (Angular):**
  1. Build Angular: `ng build --configuration production`
  2. Copy `dist/pi-planning-ui/browser/` to IIS folder for frontend (e.g., `C:\inetpub\wwwroot\`)
  3. Edit `public/env.js` in deployment folder:
     ```javascript
     window["__env"]["apiBaseUrl"] = "http://your-server-ip:5262";
     ```
  4. Verify `env.js` is served with correct Cache-Control headers

- [x] **Troubleshooting Section:**
  - 500 errors → Check Event Viewer (Application logs)
  - Connection refused → Verify SQL Server running + connection string
  - Missing .NET → Install .NET 8 Hosting Bundle
  - Permission denied → Check IIS app pool identity HasRead/Execute on folder
  - API not found → Verify backend port and env.js apiBaseUrl match

- [x] **Environment Variable Override (Optional):**
  - Can still use Docker env vars: `docker-entrypoint.sh` reads `API_BASE_URL`
  - For IIS: Manual env.js edit (simpler approach)

---

#### **TASK 8: Test Both Deployment Paths** (1.5 hours)

- [x] **PostgreSQL + Docker (existing path):**
  - [x] Build: `dotnet build` → 0 errors
  - [x] Run Docker Compose: `docker-compose up`
  - [x] Test: Can create board, add team members, import features
  - [x] Verify migrations ran automatically
- [x] **SQL Server + IIS (new path):**
  - [x] Build: `dotnet build -c Release` after updating appsettings.json for SQL Server
  - [x] Publish: `dotnet publish -c Release -o ./publish`
  - [x] Deploy to local IIS test environment
  - [x] Test: Can create board, add team members, import features
  - [x] Verify migrations ran automatically
  - [x] Access via localhost, IP address, hostname (test all variants)

- [x] **Acceptance Criteria:**
  - ✅ PostgreSQL deployment: All features work
  - ✅ SQL Server deployment: All features work
  - ✅ No database-agnostic issues (queries work on both)
  - ✅ Migrations applied automatically on startup
  - ✅ `dotnet build` → 0 errors, 0 warnings (both configs)
  - ✅ Frontend api URL resolves correctly for both deployments
  - ✅ Can access via localhost, IP, and hostname

---

#### **Summary of Changes:**

| File                           | Change                                                | Purpose                                           |
| ------------------------------ | ----------------------------------------------------- | ------------------------------------------------- |
| `.csproj`                      | Add SQL Server NuGet                                  | Enable SQL Server support                         |
| `appsettings.json`             | Add `DatabaseProvider` + commented SQL Server section | Show SQL Server support + keep PostgreSQL default |
| `appsettings.Development.json` | Add `DatabaseProvider` + commented SQL Server section | Development flexibility                           |
| `Program.cs`                   | Add conditional DbContext setup (inline)              | Switch between PostgreSQL & SQL Server            |
| `Migrations_SqlServer/`        | **NEW FOLDER** - SQL Server migrations                | Separate migration set for SQL Server             |
| `public/env.js`                | **No code change** - users edit for IIS               | Frontend API URL configuration                    |
| `IIS_DEPLOYMENT_GUIDE.md`      | **NEW FILE**                                          | Step-by-step IIS deployment guide                 |

---

### ✅ PHASE 5.5: User Name Persistence & Board Access Guard — COMPLETE

**Status:** Completed  
**Completed On:** March 1, 2026  
**Estimated Time:** 1-2 hours  
**Actual Time:** ~1 hour  
**Depends On:** Phase 5 (complete)  
**Why:** Required for SignalR presence tracking; current implementation loses user name on page refresh  
**Priority:** MUST FIX before Phase 6 (SignalR)

#### Issues Identified & Resolved:

1. ✅ **User name stored in memory only** - Now persists in sessionStorage
2. ✅ **No route guard on board pages** - userNameGuard created and applied to /boards/:id
3. ✅ **No redirect to name entry** - Guard redirects with returnUrl query param
4. ✅ **Poor UX** - Name survives refresh, cleared on browser close

#### Storage Decision: **sessionStorage** (NOT localStorage)

**Why sessionStorage?**

- ✅ Survives page refreshes within same tab (solves refresh problem)
- ✅ Cleared when tab/browser closes (no stale data across sessions)
- ✅ Each browser tab has its own session (great for multi-user testing)
- ✅ No issue with shared computers (name doesn't persist indefinitely)
- ❌ localStorage would persist forever → bad UX when user returns later or shares computer

#### Implementation Summary:

**Backend (Optional - for future multi-device sync):**

- No backend changes needed initially
- Future: Could add user profile endpoint if needed for cross-device persistence

**Frontend:**

- [x] **Update UserService** (`core/services/user.service.ts`)
  - Added **sessionStorage** persistence for user name
  - Saves name to `sessionStorage.setItem('pi-planning-user-name', name)`
  - Loads name on service init from sessionStorage
  - Added method to check if name exists: `hasName(): boolean`
  - Added method to clear name: `clearName(): void` (for logout/change name)

- [x] **Create Route Guard** (`core/guards/user-name.guard.ts`)
  - Implemented `CanActivateFn` guard using functional approach
  - Checks if `userService.hasName()` returns true
  - If false, redirects to `/name` route with `returnUrl` query param
  - After name entry, redirects back to original route

- [x] **Update Routes** (`app.routes.ts`)
  - Added guard to board routes: `{ path: 'boards/:id', component: Board, canActivate: [userNameGuard] }`
  - Kept `/name` route without guard (public access)

- [x] **Update EnterYourName Component** (`shared/components/enter-your-name/`)
  - Reads `returnUrl` from query params
  - After setting name, navigates to `returnUrl` or defaults to `/boards`
  - Added validation: name must be at least 2 characters
  - Added error message display for validation failures
  - Added Enter key support for better UX

- [ ] **Update Board Component** (optional enhancement - deferred)
  - Show user name indicator in UI (top-right corner?)
  - Add "Change Name" option in user menu (calls `userService.clearName()` → redirects to `/name`)
  - Remove "Guest" fallback (guard prevents guest access)

#### Verification Results:

- ✅ User name persists across page refreshes **within same tab**
- ✅ User name **cleared** when tab/browser closes (fresh start on new session)
- ✅ Each browser tab has **independent session** (can test as different users)
- ✅ Cannot access board routes without entering name
- ✅ After entering name, redirects to originally requested board
- ✅ Name validation prevents empty or too-short names
- ✅ User can change their name from board UI (optional)
- ✅ Build: 0 errors, 0 warnings

#### Testing Scenarios:

**Scenario 1: Refresh behavior** ✅ Good

1. Enter name "Alice" → access board
2. Refresh page (F5)
3. **Expected:** Still logged in as "Alice", board loads normally

**Scenario 2: Close and reopen** ✅ Good

1. Enter name "Bob" → access board
2. Close browser/tab
3. Reopen site → navigate to board URL directly
4. **Expected:** Redirected to name entry (sessionStorage cleared)

**Scenario 3: Multiple tabs** ✅ Good

1. Tab 1: Enter name "Alice" → access board
2. Tab 2: Open same site in new tab
3. Tab 2: Navigate to board URL directly
4. **Expected:** Tab 2 redirected to name entry (independent session)
5. Tab 2: Enter name "Bob"
6. **Expected:** Tab 1 shows "Alice", Tab 2 shows "Bob" (great for testing!)

**Scenario 4: Shared computer** ✅ Good

1. User A: Enter name → use board → close browser
2. User B: Open site later
3. **Expected:** Clean slate, must enter their own name (no stale "User A" data)

---

### PHASE 6: Real-time Collaboration (SignalR) — HIGH PRIORITY

**Status:** Not Started  
**Estimated Time:** 6-8 hours (increased from 4-6 due to detailed requirements)  
**Depends On:** Phase 4, 4.5, 4.6, 5, **5.5** (all complete)  
**Why:** Enables multi-user concurrent editing; core differentiator feature  
**Architecture:** REST for mutations (authoritative), SignalR for broadcasts (real-time notifications)

#### Design Principles:

1. **REST = Source of Truth** - All database mutations via REST endpoints
2. **SignalR = Broadcast Layer** - Notify connected clients of changes
3. **Backend as Authority** - Validation and persistence in REST layer
4. **Ephemeral State via Hub** - Cursor positions, presence (no DB persistence)

---

#### **TASK 6.1: Identify All Mutation Endpoints Needing Broadcasts** (Analysis Complete)

Based on controller analysis, the following REST endpoints require SignalR broadcasts:

**BoardsController** (`api/boards`):

- ✅ `PATCH /{id}/finalize` → Broadcast: `BoardFinalized(boardId, finalizedAt, finalizedBy)`
- ✅ `PATCH /{id}/restore` → Broadcast: `BoardRestored(boardId, restoredAt, restoredBy)`

**UserStoriesController** (`api/boards/{boardId}/stories`):

- ✅ `PATCH /{storyId}/move` → Broadcast: `StoryMoved(storyId, fromSprintId, toSprintId, movedBy)`
- ✅ `PATCH /{storyId}/refresh` → Broadcast: `StoryRefreshed(storyId, updatedData)`

**TeamController** (`api/boards/{boardId}/team`):

- ✅ `POST /` → Broadcast: `TeamMemberAdded(boardId, teamMember)`
- ✅ `PUT /{teamMemberId}` → Broadcast: `TeamMemberUpdated(boardId, teamMemberId, updatedData)`
- ✅ `DELETE /{teamMemberId}` → Broadcast: `TeamMemberDeleted(boardId, teamMemberId)`
- ✅ `PATCH /{teamMemberId}/sprints/{sprintId}` → Broadcast: `CapacityUpdated(boardId, teamMemberId, sprintId, newCapacity)`

**FeaturesController** (`api/v1/boards/{boardId}/features`):

- ✅ `POST /import` → Broadcast: `FeatureImported(boardId, feature)`
- ✅ `PATCH /{id}/refresh` → Broadcast: `FeatureRefreshed(boardId, featureId, updatedData)`
- ✅ `PATCH /reorder` → Broadcast: `FeaturesReordered(boardId, newOrder[])`
- ✅ `DELETE /{id}` → Broadcast: `FeatureDeleted(boardId, featureId)`

**Ephemeral Events (Hub Methods Only - No REST):**

- ✅ Cursor/Mouse Movement → Hub: `UpdateCursorPosition(boardId, userId, x, y)`
- ✅ User Presence → Hub: `JoinBoard(boardId, userName)` / `LeaveBoard(boardId, userName)`

**Total: 13 REST-triggered broadcasts + 2 ephemeral hub methods**

---

#### **TASK 6.2: Create SignalR DTOs** (1 hour)

Create `DTOs/SignalR/` folder with event payload classes:

- [ ] **BoardEventDto.cs** - Base class for board events

  ```csharp
  public class BoardEventDto {
    public int BoardId { get; set; }
    public string EventType { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string TriggeredBy { get; set; } = "";
  }
  ```

- [ ] **StoryMovedEventDto.cs** - Story movement payload
- [ ] **TeamMemberEventDto.cs** - Team member CRUD payloads
- [ ] **FeatureEventDto.cs** - Feature CRUD payloads
- [ ] **CapacityUpdatedEventDto.cs** - Capacity change payload
- [ ] **CursorPresenceDto.cs** - Ephemeral cursor position
- [ ] **UserPresenceDto.cs** - User join/leave board

#### **TASK 6.2A: Cursor Presence Contract (Define Before Coding)**

**Goal:** Standardize payload + rate limits + UX behavior before implementation to avoid rework.

**Server Event: `CursorPresenceUpdated`**

```json
{
  "boardId": 123,
  "userId": "u-42",
  "displayName": "Anirban",
  "cursor": { "x": 1240, "y": 380 },
  "color": "#3B82F6",
  "avatar": "A",
  "isAnonymous": false,
  "activity": "active",
  "sequence": 57,
  "timestampUtc": "2026-03-01T12:30:45.123Z"
}
```

**Client → Hub Method: `UpdateCursorPosition`**

```json
{
  "boardId": 123,
  "x": 1240,
  "y": 380,
  "sequence": 57,
  "timestampUtc": "2026-03-01T12:30:45.123Z"
}
```

**Presence Lifecycle Events:**

- `UserJoinedBoard`: `boardId`, `userId`, `displayName`, `color`, `avatar`, `isAnonymous`, `joinedAtUtc`
- `UserLeftBoard`: `boardId`, `userId`, `leftAtUtc`, `reason` (`disconnect` | `leave` | `timeout`)

**Throttle + Delivery Strategy:**

- Client send rate: **10-20 Hz** (`50-100ms` throttle), default target **15 Hz**.
- Ignore tiny movement deltas (`< 3px`) to reduce noise.
- Drop stale/out-of-order messages using `sequence` and `timestampUtc`.
- Do not persist cursor coordinates in database (ephemeral only).

**UI/UX Rules (Must Validate):**

- Render **remote user cursors via SignalR** (not only local cursor label).
- Auto-hide remote cursor label after **2.5-3s idle**; keep user in presence list.
- Assign distinct color/avatar per user (stable within session).
- Add privacy mode: show initials/anonymous alias instead of full name when enabled.

**Validation Checklist (Pre-merge for Phase 6):**

- [ ] Remote cursors visible for all connected users on same board.
- [ ] Send rate stays within configured throttle range under continuous mouse movement.
- [ ] Idle cursor labels auto-hide and reappear on activity.
- [ ] Distinct color/avatar mapping remains stable after reconnect.
- [ ] Privacy mode masks identity correctly while preserving collaboration.
- [ ] No DB writes for cursor updates (ephemeral transport only).

---

#### **TASK 6.3: Implement PlanningHub** (2 hours)

Update `Hubs/PlanningHub.cs`:

- [ ] Add hub methods for ephemeral state:
  - `Task JoinBoard(int boardId, string userName)`
  - `Task LeaveBoard(int boardId, string userName)`
  - `Task UpdateCursorPosition(int boardId, string userName, int x, int y)`

- [ ] Add SignalR group management:
  - Groups named: `board:{boardId}` (e.g., `board:123`)
  - Join group on `JoinBoard()`, remove on `LeaveBoard()` or disconnect

- [ ] Add disconnect cleanup:
  - Override `OnDisconnectedAsync()` to broadcast user left

- [ ] Add authorization check:
  - Validate board exists before joining group
  - Future: Check user has permission to access board

---

#### **TASK 6.4: Wire Broadcasts in Controllers** (2-3 hours)

Inject `IHubContext<PlanningHub>` in each controller:

- [ ] **BoardsController**
  - In `FinalizeBoard()`: After DB commit → `await _hub.Clients.Group($"board:{id}").SendAsync("BoardFinalized", eventDto);`
  - In `RestoreBoard()`: After DB commit → Broadcast `BoardRestored`

- [ ] **UserStoriesController**
  - In `MoveStory()`: After DB commit → Broadcast `StoryMoved`
  - In `RefreshStory()`: After Azure refresh → Broadcast `StoryRefreshed`

- [ ] **TeamController**
  - In `AddTeamMember()`: After DB insert → Broadcast `TeamMemberAdded`
  - In `UpdateTeamMember()`: After DB update → Broadcast `TeamMemberUpdated`
  - In `DeleteTeamMember()`: After DB delete → Broadcast `TeamMemberDeleted`
  - In `UpdateCapacity()`: After DB update → Broadcast `CapacityUpdated`

- [ ] **FeaturesController**
  - In `ImportFeature()`: After DB insert → Broadcast `FeatureImported`
  - In `RefreshFeature()`: After Azure refresh → Broadcast `FeatureRefreshed`
  - In `ReorderFeatures()`: After DB update → Broadcast `FeaturesReordered`
  - In `DeleteFeature()`: After DB delete → Broadcast `FeatureDeleted`

**Important:** Place broadcasts **AFTER** database commits (inside transaction success path)

---

#### **TASK 6.5: Create Frontend SignalR Service** (1.5 hours)

Create `features/board/services/signalr.service.ts`:

- [ ] Install SignalR client: `npm install @microsoft/signalr`
- [ ] Create service with connection management:
  - `connect(boardId: number, userName: string): Promise<void>`
  - `disconnect(): Promise<void>`
  - Handle reconnection logic with exponential backoff
  - Emit events as RxJS Observables

- [ ] Add event listeners (13 events):
  - Map SignalR events to typed Observables
  - Example: `onStoryMoved$: Observable<StoryMovedEvent>`

- [ ] Add cursor/presence methods:
  - `sendCursorUpdate(x: number, y: number, sequence: number): Promise<void>`
  - Listen to cursor updates from other users
  - Track connected users list
  - Apply throttle strategy (default 15 Hz; configurable 10-20 Hz)
  - Handle idle auto-hide timers for remote cursor labels
  - Support privacy mode display (`fullName` vs `initials/anonymous`)

---

#### **TASK 6.6: Wire SignalR in Board Component** (1.5 hours)

Update `features/board/components/board.ts`:

- [ ] **On board load:**
  - Connect to SignalR hub: `await signalrService.connect(boardId, userName)`
  - Subscribe to all relevant events for current board

- [ ] **Event handlers:**
  - `StoryMoved` → Update UI without API refetch (move story in local state)
  - `TeamMemberAdded/Updated/Deleted` → Refresh team bar
  - `CapacityUpdated` → Update capacity row
  - `FeatureImported/Refreshed/Deleted` → Refresh feature list
  - `FeaturesReordered` → Reorder features in UI
  - `BoardFinalized/Restored` → Update finalization banner

- [ ] **Cursor tracking:**
  - Track mouse position on board canvas
  - Throttle cursor updates (10-20 Hz, default 15 Hz)
  - Render **remote users' cursors via SignalR** (not local-only label)
  - Auto-hide labels after 2.5-3s idle, show again on movement
  - Show distinct color/avatar per user
  - Honor privacy mode (initials/anonymous alias)

- [ ] **Cleanup:**
  - Disconnect on component destroy: `ngOnDestroy()`
  - Leave board group on route change

---

#### **Acceptance Criteria:**

- ✅ Multiple users can connect to same board
- ✅ User presence shows: "User A, User B, User C viewing this board"
- ✅ Cursor positions update in real-time (< 100ms latency)
- ✅ Remote user cursors are delivered via SignalR events (not local-only rendering)
- ✅ Cursor update stream is throttled to 10-20 Hz (default 15 Hz)
- ✅ Cursor labels auto-hide after idle timeout and reappear on activity
- ✅ Distinct color/avatar is visible per user and stable within a session
- ✅ Optional privacy mode shows initials/anonymous instead of full names
- ✅ Story moves broadcast to all connected clients (< 500ms)
- ✅ Team member CRUD operations sync across clients
- ✅ Feature CRUD operations sync across clients
- ✅ Board finalization/restoration syncs across clients
- ✅ Users disconnecting are removed from presence list
- ✅ SignalR reconnects automatically on network interruption
- ✅ No race conditions or data corruption on concurrent moves
- ✅ Browser A makes change → Browser B sees it without manual refresh
- ✅ Build: 0 errors, 0 warnings
- ✅ Manual testing: 3+ users editing same board concurrently

---

#### **Testing Scenarios:**

1. **Basic Presence:** Open board in 3 browsers → See 3 users in presence list
2. **Remote Cursor:** Move mouse in Browser A → Browser B/C see A’s cursor label moving
3. **Throttle Validation:** Continuous movement for 10s → outbound updates stay within 10-20 Hz
4. **Idle Auto-hide:** Stop moving in Browser A → Browser B/C hide A label after idle timeout
5. **Distinct Identity:** Browser A/B/C show different colors/avatars consistently
6. **Privacy Mode:** Enable privacy in Browser A → Browser B/C see initials/anonymous for A
7. **Story Move:** Browser A moves story → Browser B/C see move instantly
8. **Team Capacity:** Browser A updates capacity → Browser B/C see update
9. **Feature Import:** Browser A imports feature → Browser B/C see new feature
10. **Finalization:** Browser A finalizes board → Browser B/C see finalized banner
11. **Disconnect:** Close Browser A → Browser B/C remove user from presence
12. **Concurrent Moves:** Browser A & B move different stories simultaneously → Both succeed
13. **Network Interrupt:** Disconnect WiFi → Reconnect → SignalR reconnects automatically

---

### PHASE 7: Board Lock/Unlock Endpoints — HIGH PRIORITY

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

- ✅ Lock endpoint sets IsLocked flag in database
- ✅ Unlock endpoint clears IsLocked flag
- ✅ UI shows locked state clearly (separate from finalized state)
- ✅ All operations blocked when locked (add/edit/delete/move)
- ✅ Board can be both finalized AND locked
- ✅ Build: 0 errors

---

### PHASE 8: Documentation & Integration Testing — WRAP-UP

**Status:** Not Started  
**Estimated Time:** 3-4 hours  
**Depends On:** All other phases complete
**Why:** Ensure comprehensive documentation and real-world integration testing

#### Documentation Tasks:

- [ ] **Architecture Documentation**
  - Entity-relationship diagram (database schema)
  - Service layer interaction flow
  - Component hierarchy (Angular)
  - Real-time (SignalR) communication diagram

- [ ] **API Reference Documentation**
  - Endpoint list with HTTP methods
  - Request/response examples (JSON)
  - Error codes and handling
  - Authentication/Authorization requirements

- [ ] **Deployment Runbooks**
  - Docker Compose setup (local development)
  - Windows IIS + SQL Server deployment
  - PostgreSQL database setup
  - SSL/HTTPS configuration

- [ ] **User Guides**
  - Feature overview with screenshots
  - Step-by-step workflows (create board, add team, import features, plan)
  - Finalization process
  - Lock/Unlock usage

- [ ] **Code Documentation**
  - README.md updates (setup, features, architecture, contributing)
  - Key service method documentation
  - Inline comments for complex logic
  - CONTRIBUTING.md guidelines

- [ ] **Integration Testing** (Manual)
  - Create a fresh board end-to-end
  - Multi-user concurrent operations
  - Test both PostgreSQL and SQL Server deployments
  - Test Docker and IIS deployments
  - Verify lock/unlock with SignalR broadcasts

- [ ] **Performance Testing**
  - Stress test concurrent users (10+)
  - Monitor database query performance
  - Check SignalR message throughput
  - Document performance benchmarks

- [ ] **Security Audit**
  - SQL injection prevention review
  - Authentication/Authorization flows
  - CORS configuration validation
  - PAT handling security
  - Data validation & sanitization

#### Acceptance Criteria:

- ✅ All features documented (user-facing)
- ✅ All APIs documented (developer-facing)
- ✅ Deployment guides complete (Docker + IIS)
- ✅ Integration testing complete (all major workflows)
- ✅ Performance benchmarks documented
- ✅ Security audit completed with findings documented
- ✅ README.md comprehensive and current
- ✅ Zero issues blocking production deployment

---

## 📋 DECISIONS & RESOLUTIONS

### Phase 4 - Decisions

**✅ ValidationService: throw exceptions or return error objects?**

- **DECIDED:** Option 1 - Exception-Based approach
- **Why:** Already working in codebase via GlobalExceptionHandlingMiddleware
- **Pattern:** Methods throw exceptions; middleware catches and converts to HTTP (400/404/409)
- **Exceptions:** KeyNotFoundException (404), ArgumentException (400), InvalidOperationException (400)
- **Result:** Simple, fail-fast, centralized exception handling

**❌ Logging format/context/tracing?**

- **DECIDED:** Option 3 - Built-in ILogger<T> + RequestCorrelationMiddleware
- **Why:** Console output sufficient (Docker logs → captured, IIS → Event Viewer)
- **No file logging needed** (already captured per deployment)
- **Implementation:** Create RequestCorrelationMiddleware, structured logging in services
- **Correlation IDs:** Enable request tracing across logs

### Phase 4.5 - Decisions

**✅ String constants approach:** Object literals (not enums) for flexibility

### Phase 4.6 - Decisions

**✅ Warnings phase:** OPEN-ENDED - Add warnings as discovered during implementation

- No hard deadline or pressure
- Incremental fixes as work progresses
- Flexible scope

### Phase 5 (IIS/SQL Server) - Decisions

**✅ SQL Server version:** SQL Server 2019+ or Azure SQL Database
**✅ Connection selection:** `DatabaseProvider` config key (`PostgreSQL`/`SqlServer`) + single `DefaultConnection`
**✅ IIS setup:** Manual, step-by-step guide with IIS Applications under Default Web Site (`/PIPlanningBackend`, `/PIPlanningUI`)
**✅ Windows authentication:** Support is optional, not required
**✅ Priority:** FOR STAKEHOLDER DEMO (before SignalR)

### Phase 5.5 (User Name Persistence) - Decisions

**✅ Storage mechanism:** sessionStorage (NOT localStorage - clears on tab/browser close)
**✅ Route guard:** Required - prevent board access without name entry
**✅ Return URL:** After name entry, redirect to originally requested board
**✅ Priority:** CRITICAL - Required before Phase 6 (SignalR presence needs user identity)
**✅ Behavior:** Name persists on refresh but cleared on browser/tab close (prevents stale data)

### Phase 6 (SignalR) - Decisions

**✅ Architecture:** REST for mutations (authoritative), SignalR for broadcasts (notifications only)
**✅ Testing approach:** Manual testing (concurrent multi-user scenarios)
**✅ Broadcast count:** 13 REST-triggered events + 2 ephemeral hub methods (cursor, presence)
**✅ Event delivery:** Broadcast AFTER database commit (inside transaction success path)
**✅ Cursor payload contract:** Include `userId`, `displayName`, `x/y`, `sequence`, `timestampUtc`, `color/avatar`, `isAnonymous`
**✅ Cursor rate limit:** Throttle 10-20 Hz (default 15 Hz) + movement delta filtering
**✅ Cursor UX:** Remote cursors, idle auto-hide, distinct identity colors/avatars
**✅ Privacy:** Optional anonymous/initials mode for name masking

### Phase 7 (Board Lock/Unlock) - Decisions

**✅ Finalized & Locked boards:** SEPARATE STATES (not mutually exclusive)

- A board can be: finalized only, locked only, or BOTH finalized AND locked
- UI shows both states independently with separate indicators

### Phase 8 (Documentation) - Decisions

**✅ Scope:** Comprehensive documentation + integration testing (3-4 hours)

---

## 🔄 Updated Phase Dependency Chain

```
Phases 4 → 4.5 → 4.6 → 5 → 5.5 → 6 → 7 → 8
(mostly sequential; 4.5 & 4.6 can run in parallel with 4)

4: Backend Refactoring (5-6 hrs)
  ↓ (foundation for all following)
  ├─ 4.5: UI String Constants (2 hrs) [parallel with 4]
  │  ↓
  └─ 4.6: Warning Elimination (1-3 hrs, open-ended) [parallel or after 4]
     ↓ (codebase refactored & clean)
     5: Windows IIS + SQL Server Support (5-6 hrs) [FOR STAKEHOLDER DEMO]
        ↓ (deployment flexibility achieved)
        5.5: User Name Persistence & Route Guard (1-2 hrs) [CRITICAL FIX]
           ↓ (user identity persistence for SignalR)
           6: SignalR Real-time Collaboration (6-8 hrs) [13 events + presence]
              ↓
              7: Board Lock/Unlock Endpoints (2-3 hrs)
                 ↓
                 8: Documentation & Integration Testing (3-4 hrs)

Timeline: ~29-38 hours total (updated with Phase 5.5 + revised Phase 6)
Optimized: ~21-27 hours if 4.5 & 4.6 run simultaneously with 4

Key Milestones:
  - After Phase 4.6: Codebase clean & production-ready
  - After Phase 5: Ready for stakeholder demo (Windows IIS + SQL Server)
  - After Phase 8: Production-ready with complete documentation
```

---

## ✅ FINAL CHECKLIST BEFORE PHASE 4 STARTS

**Must Resolve:**

- [ ] ValidationService approach (exceptions vs Result<T>)
- [ ] Logging format/context specification
- [ ] Create feature branch: `feature/phase-4-refactoring`
- [ ] Ensure all tests pass before starting

**Optional Pre-work:**

- [ ] Review Phase 4 subtasks in detail
- [ ] Identify all files needing refactoring
- [ ] Plan commit checkpoints (1-2 hour intervals)

---

## 📋 PHASE 4 DETAILED IMPLEMENTATION GUIDE

### **Task 1: Standardize Constructor Injection (1 hour)**

**Status:** Not Started  
**Branch:** `chore/backendRefactoring`

**Files to Update (5 controllers):**

1. `Controllers/BoardsController.cs` - Change from `public BoardsController(IBoardService boardService) { _boardService = boardService; }`
   - **To:** Primary constructor `public BoardsController(IBoardService boardService) : ControllerBase`
   - Check all methods use `boardService` directly (not `_boardService`)

2. `Controllers/TeamController.cs` - Same pattern
3. `Controllers/AzureController.cs` - Same pattern
4. `Controllers/FeaturesController.cs` - Same pattern
5. `Controllers/UserStoriesController.cs` - Already uses arrow function, convert to primary constructor

**Testing:**

- `dotnet build` should produce 0 errors
- All methods still work (compile check only)

---

### **Task 2: Create & Wire SprintService (1.5 hours)**

**Status:** Not Started

**Part A: Create Interface**

- File: `Services/Interfaces/ISprintService.cs`
- Method:
  ```csharp
  // Generate sprints for a board (extracted from BoardService.CreateBoardAsync)
  List<Sprint> GenerateSprintsForBoard(Board board, int numSprints, int sprintDurationDays);
  ```

**Part B: Create Implementation**

- File: `Services/Implementations/SprintService.cs`
- Implementation:
  1. **GenerateSprintsForBoard()** - Extract logic from BoardService.CreateBoardAsync() (lines 47-70)
     - Create Sprint 0 (placeholder)
     - Loop for sprints 1-N with date calculations
     - Return list of generated sprints

**Part C: Refactor BoardService**

- Inject ISprintService in constructor
- Replace sprint generation code with: `var sprints = _sprintService.GenerateSprintsForBoard(board, dto.NumSprints, dto.SprintDuration);`
- Add to each generated sprint: `board.Sprints.Add(sprint);`

**Part D: Wire in Program.cs**

- Add: `services.AddScoped<ISprintService, SprintService>();`

**Testing:**

- Board creation still works
- Sprint dates calculated correctly

---

### **Task 3: Create RequestCorrelationMiddleware (1 hour)**

**Status:** Not Started

**File:** `Middleware/RequestCorrelationMiddleware.cs` (~40 lines)

```csharp
public class RequestCorrelationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestCorrelationMiddleware> _logger;

    public RequestCorrelationMiddleware(RequestDelegate next, ILogger<RequestCorrelationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract correlation ID from header or generate new one
        var correlationId = context.Request.Headers.TryGetValue("X-Correlation-ID", out var headerValue)
            ? headerValue.ToString()
            : Guid.NewGuid().ToString();

        // Add to LogContext so all logs include it
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("Path", context.Request.Path))
        {
            _logger.LogInformation("Request started: {Method} {Path}", context.Request.Method, context.Request.Path);

            await _next(context);

            _logger.LogInformation("Request completed: {StatusCode}", context.Response.StatusCode);
        }
    }
}
```

**Wire in Program.cs** (before MapControllers):

```csharp
// Global exception handling MUST come first
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Then request correlation
app.UseMiddleware<RequestCorrelationMiddleware>();

if (app.Environment.IsDevelopment()) ...
```

**Testing:**

- Run app, check console logs include correlation ID
- Make multiple requests, see different correlation IDs

---

### **Task 4: Create ValidationService (1.5 hours)**

**Status:** Not Started

**Part A: Create Interface**

- File: `Services/Interfaces/IValidationService.cs`
- 7 methods (all async except ones marked void):
  ```csharp
  Task ValidateBoardExists(int boardId);                         // Throws KeyNotFoundException
  Task ValidateStoryBelongsToBoard(int storyId, int boardId);    // Throws KeyNotFoundException
  Task ValidateTeamMemberBelongsToBoard(int memberId, int boardId); // Throws KeyNotFoundException
  Task ValidateSprintBelongsToBoard(int sprintId, int boardId);  // Throws KeyNotFoundException
  Task ValidateFeatureBelongsToBoard(int featureId, int boardId); // Throws KeyNotFoundException
  void ValidateBoardNotFinalized(Board board, string operation);  // Throws InvalidOperationException
  void ValidateTeamMemberCapacity(int capacity, int sprintWorkDays); // Throws ArgumentException
  ```

**Part B: Create Implementation**

- File: `Services/Implementations/ValidationService.cs`
- Inject all required repositories in constructor:
  - IBoardRepository
  - IFeatureRepository
  - IUserStoryRepository
  - ITeamRepository
  - ISprintRepository (if exists, or BoardRepository for sprints)

**Implementation for each method:**

```csharp
public async Task ValidateBoardExists(int boardId)
{
    var board = await _boardRepository.GetByIdAsync(boardId);
    if (board == null)
        throw new KeyNotFoundException($"Board {boardId} not found");
}

public async Task ValidateStoryBelongsToBoard(int storyId, int boardId)
{
    var story = await _storyRepository.GetByIdAsync(storyId);
    if (story == null || story.Feature?.BoardId != boardId)
        throw new KeyNotFoundException("Story not found or doesn't belong to board");
}

public void ValidateBoardNotFinalized(Board board, string operation)
{
    if (board.IsFinalized)
        throw new InvalidOperationException($"Cannot {operation} on finalized board");
}

public void ValidateTeamMemberCapacity(int capacity, int sprintWorkDays)
{
    if (capacity < 0)
        throw new ArgumentException("Capacity cannot be negative");
    if (capacity > sprintWorkDays)
        throw new ArgumentException($"Capacity {capacity} exceeds sprint work days {sprintWorkDays}");
}
```

**Part C: Wire in Program.cs**

- Add: `services.AddScoped<IValidationService, ValidationService>();`

**Part D: Update Services to Use ValidationService**

- Inject IValidationService in BoardService, FeatureService, TeamService constructors
- Replace inline validation checks with ValidationService calls:

  ```csharp
  // Before
  if (story == null || story.Feature?.BoardId != boardId)
      throw new Exception("Not found");

  // After
  await _validator.ValidateStoryBelongsToBoard(storyId, boardId);
  ```

**Testing:**

- Validation exceptions throw correctly
- GlobalExceptionHandlingMiddleware converts to HTTP 400/404

---

### **Task 5: Add Structured Logging (1.5 hours)**

**Status:** Not Started

**For each service, inject ILogger<ServiceName>:**

**BoardService:**

- Add 5-6 logs in key methods:
  - CreateBoardAsync: `_logger.LogInformation("Creating board '{Name}' with {NumSprints} sprints", board.Name, board.NumSprints);`
  - FinalizeBoardAsync: `_logger.LogInformation("Finalizing board {BoardId}", boardId);`
  - RestoreBoardAsync: `_logger.LogInformation("Restoring board {BoardId}", boardId);`

**FeatureService:**

- Add 6-7 logs:
  - ImportFeatureToBoardAsync: `_logger.LogInformation("Importing feature {FeatureTitle} to board {BoardId}", featureDto.Title, boardId);`
  - RefreshFeatureFromAzureAsync: `_logger.LogInformation("Refreshing feature {FeatureId} from Azure", featureId);`
  - MoveUserStoryAsync (if exists): `_logger.LogInformation("Moving story {StoryId} to sprint {TargetSprintId}", storyId, targetSprintId);`

**TeamService:**

- Add 4-5 logs:
  - AddTeamMemberAsync: `_logger.LogInformation("Adding team member '{Name}' to board {BoardId}", member.Name, boardId);`
  - UpdateTeamMemberAsync: `_logger.LogInformation("Updating team member {MemberId}", memberId);`
  - DeleteTeamMemberAsync: `_logger.LogInformation("Deleting team member {MemberId}", memberId);`

**SprintService:**

- Add 1-2 logs:
  - GenerateSprintsForBoard: `_logger.LogInformation("Generating {NumSprints} sprints for board {BoardId}", numSprints, board.Id);`

**AzureBoardsService:**

- Already has ILogger, add 2-3 more logs:
  - GetFeatureWithChildrenAsync: `_logger.LogInformation("Fetching feature {FeatureId} from Azure", featureId);`
  - On error: `_logger.LogError("Azure API call failed: {Error}", ex.Message);`

**Log Distribution:** 18-23 total log statements across all services

**Testing:**

- Run app, check console output includes:
  - Correlation IDs in all logs
  - Appropriate log levels (Information for normal ops, Warning for issues, Error for exceptions)
  - Structured data in logs (board IDs, sprint IDs, etc.)

---

### **Task 6: Wrap Operations in Transactions (1 hour)**

**Status:** Not Started

**3 Operations Need Transactions:**

**1. FeatureService.ImportFeatureToBoardAsync() (Lines 31-48)**

```csharp
public async Task<FeatureDto> ImportFeatureToBoardAsync(int boardId, FeatureDto featureDto, bool checkFinalized = true)
{
    using (var tx = await _boardRepo.Database.BeginTransactionAsync())
    {
        try
        {
            // Existing logic: CreateOrModifyFeature + CreateOrUpdateUserStory
            var board = await _boardRepo.GetBoardWithSprintsAsync(boardId);
            if (board == null) return new FeatureDto();

            if (checkFinalized && board.IsFinalized)
                throw new InvalidOperationException("Cannot add features to finalized board");

            Feature? existing = await CreateOrModifyFeature(boardId, featureDto);
            var sprints = board.Sprints.OrderBy(s => s.Id).ToList();
            var childrenUserStoriesDto = featureDto.Children ?? [];
            await CreateOrUpdateUserStory(existing, sprints, childrenUserStoriesDto);

            await tx.CommitAsync();

            // Return result after commit
            var saved = await _featureRepo.GetByIdAsync(existing.Id);
            return MapToDto(saved);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
```

**2. FeatureService.RefreshFeatureFromAzureAsync() (Lines 149-158)**

```csharp
public async Task<FeatureDto?> RefreshFeatureFromAzureAsync(int boardId, int featureId, string organization, string project, string pat)
{
    using (var tx = await _boardRepo.Database.BeginTransactionAsync())
    {
        try
        {
            var feature = await _featureRepo.GetByIdAsync(featureId);
            if (feature == null || feature.BoardId != boardId) return null;

            // Fetch from Azure
            var workItem = await _azureService.GetFeatureWithChildrenAsync(organization, project, int.Parse(feature.AzureId!), pat);

            // Import (wrapped in same transaction)
            var result = await ImportFeatureToBoardAsync(boardId, workItem, checkFinalized: false);

            await tx.CommitAsync();
            return result;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
```

**3. BoardService.FinalizeBoardAsync() (Lines 227-250)**

```csharp
public async Task<BoardSummaryDto?> FinalizeBoardAsync(int boardId)
{
    using (var tx = await _boardRepository.Database.BeginTransactionAsync())
    {
        try
        {
            var board = await _boardRepository.GetBoardWithFullHierarchyAsync(boardId);
            if (board == null) return null;

            // Set finalization flag
            board.IsFinalized = true;
            board.FinalizedAt = DateTime.UtcNow;

            // Update all story OriginalSprintIds
            foreach (var feature in board.Features)
            {
                foreach (var userStory in feature.UserStories)
                {
                    userStory.OriginalSprintId = userStory.SprintId;
                }
            }

            await _boardRepository.SaveChangesAsync();
            await tx.CommitAsync();

            _logger.LogInformation("Board {BoardId} finalized", boardId);
            return await GetBoardPreviewAsync(boardId);
        }
        catch
        {
            await tx.RollbackAsync();
            _logger.LogError("Failed to finalize board {BoardId}", boardId);
            throw;
        }
    }
}
```

**Testing:**

- Create feature → verify atomicity (all or nothing)
- Refresh feature → verify Azure fetch + DB update together
- Finalize board → verify flag + all story IDs updated together

---

### **Task 7: Refactor Azure Parameters (0.5 hours)**

**Status:** Not Started

**Create AzureContext class:**

- File: `Services/AzureContext.cs`

```csharp
public class AzureContext
{
    public string Organization { get; set; } = "";
    public string Project { get; set; } = "";
    public string Pat { get; set; } = "";
}
```

**Update AzureBoardsService signature (refactor later if needed):**

- For now, this is LOW PRIORITY - can be deferred to Phase 4.1
- Just document in code comment

---

### **Task 8: Organize PasswordHelper (0.5 hours)**

**Status:** Not Started

**PasswordHelper Location:**

- Currently: Nested class in BoardService.cs (line 269)

**Action:**

1. Extract to new file: `Services/Utilities/PasswordHelper.cs`
2. Make it `public static class` with `public static HashPassword()` method
3. Add XML documentation
4. Update BoardService to use: `PasswordHelper.HashPassword(dto.Password)`

**Testing:**

- Board creation with password still works
- Password hashing works correctly

---

## ✅ PHASE 4 ACCEPTANCE CHECKLIST

**At End of Phase 4, Verify:**

- [ ] 5 controllers using primary constructors only
- [ ] SprintService created with GenerateSprintsForBoard() method only, full interface, wired in DI
- [ ] RequestCorrelationMiddleware created, registered (logs include correlation IDs)
- [ ] ValidationService created with 7 methods, wired in all service usages
- [ ] 18-23 structured log statements added across 5 services
- [ ] 3 complex operations wrapped in transactions
- [ ] PasswordHelper extracted to separate file
- [ ] `dotnet build` → 0 errors, 0 warnings
- [ ] All existing functionality preserved (no breaking changes)
- [ ] Console logs show correlation IDs + structured data

**Git commits should be:**

1. `Standardize controller constructors (primary pattern)`
2. `Extract sprint logic into SprintService`
3. `Add RequestCorrelationMiddleware for request tracing`
4. `Create ValidationService with 7 validation methods`
5. `Add structured logging across services`
6. `Wrap multi-step operations in transactions`
7. `Extract PasswordHelper to separate file`
8. `Final: Phase 4 backend refactoring complete`

---

**Ready to begin Phase 4?**

---

## 🛡️ FINAL PHASE (POST-DEMO): Multi-Provider Migration Hardening

### PHASE 9: Provider-Isolated EF Core Migrations (Industry Standard)

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

### **TASK 9.1: Create Separate Migration Projects**

- [ ] Create two class library projects for migrations only (PostgreSQL + SQL Server).
- [ ] Add references from each migration project to backend project (for `AppDbContext` and entities).
- [ ] Add required EF provider package per migration project.

### **TASK 9.2: Wire Provider-Specific Migration Assemblies in Runtime**

- [ ] Update `Program.cs` DbContext registration:
  - PostgreSQL path: `UseNpgsql(connectionString, x => x.MigrationsAssembly("...postgres"))`
  - SQL Server path: `UseSqlServer(connectionString, x => x.MigrationsAssembly("...sqlserver"))`
- [ ] Keep existing `DatabaseProvider` config switch.
- [ ] Validate startup logs clearly show active provider + migration assembly.

### **TASK 9.3: Re-Scaffold Baseline Migrations per Provider**

- [ ] Generate baseline migration in postgres migration project.
- [ ] Generate baseline migration in sqlserver migration project.
- [ ] Ensure each project has its own `AppDbContextModelSnapshot.cs`.
- [ ] Remove legacy mixed migration ambiguity from backend project after verification.

### **TASK 9.4: Update Commands/Runbooks**

- [ ] Update `IIS_DEPLOYMENT_GUIDE.md` with provider-specific EF commands using:
  - `--project <migrations-project>`
  - `--startup-project <backend-project>`
- [ ] Update local dev and Docker instructions for PostgreSQL migration flow.
- [ ] Add a short “Do/Don’t” section to prevent using folder-only migration separation.

### **TASK 9.5: Add Validation Gates (CI + Manual)**

- [ ] CI check: generate/apply migrations for PostgreSQL path in isolated test DB.
- [ ] CI check: generate/apply migrations for SQL Server path in isolated test DB.
- [ ] Manual smoke tests:
  - Backend startup + auto-migrate works for each provider.
  - `__EFMigrationsHistory` contains only expected provider migration set.

---

### Acceptance Criteria for Phase 9

- ✅ Provider-specific migration assemblies exist and are used by runtime.
- ✅ No provider cross-over during `dotnet ef migrations add` / `database update`.
- ✅ SQL Server and PostgreSQL migrations can be generated independently without config hacks.
- ✅ Startup migration succeeds cleanly for both deployment paths.
- ✅ Documentation updated with exact provider-specific command examples.

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
