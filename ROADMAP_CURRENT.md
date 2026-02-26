# PI Planning Tool - Current Roadmap & Priorities

**Last Updated:** February 26, 2026  
**Current Status:** Phase 4.6 Complete 🎉 — Code quality controls finalized; ready for Phase 5 (IIS/SQL Server)  
**Current Branch:** `main`

---

## 🚀 NEXT PRIORITIES (Ordered by Dependency & Impact)

### ✅ PHASE 4: Backend Code Refactoring & Cleanup — COMPLETE
**Status:** Finished February 22, 2026  
**Time Spent:** ~6 hours  
**Outcome:** Clean, maintainable backend foundation established

#### Tasks Completed:
1. ✅ **Standardize Constructor Injection** - All 5 controllers use primary constructor pattern
2. ✅ **Extract & Create SprintService** - Dedicated service for sprint operations  
3. ✅ **Add RequestCorrelationMiddleware** - Request tracing with X-Correlation-ID header
4. ✅ **Create ValidationService** - 7 centralized validation methods
5. ✅ **Add Structured Logging** - 20+ logs with CorrelationId + EF Core configuration
6. ✅ **Wrap Operations in Transactions** - 6 critical methods with atomic guarantees
7. ✅ **Refactor Azure Parameters** - Skipped (minimal value, multi-tenant constraints)
8. ✅ **Organize PasswordHelper** - Extracted + upgraded to PBKDF2 (industry standard)

**Phase 4 Deliverables:**
- ✅ All services use dependency injection with constructor parameters
- ✅ SprintService registered in DI with full sprint lifecycle management
- ✅ RequestCorrelationMiddleware adds X-Correlation-ID to all requests
- ✅ ValidationService with 7 methods (all repositories injected)
- ✅ 20+ structured log statements across services
- ✅ EF Core logging configured by environment (Warning prod, Information dev)
- ✅ ITransactionService interface + TransactionService implementation wired
- ✅ 6 multi-step operations wrapped in transactions:
  - BoardService: CreateBoardAsync, FinalizeBoardAsync
  - TeamService: AddTeamMemberAsync
  - FeatureService: ImportFeatureToBoardAsync, RefreshUserStoryFromAzureAsync, DeleteFeatureAsync
- ✅ PasswordHelper extracted to Services/Utilities/ with PBKDF2 hashing
  - Random salt generation per password
  - 10,000 NIST-recommended iterations
  - Constant-time password comparison (timing attack resistant)
  - Format: "{base64_salt}:{base64_hash}"
- ✅ Build: 0 Errors, 2 Warnings (Npgsql version, unrelated)
- ✅ All tests passing, no breaking changes

**Files Created (Task 6 - Transactions):**
- `Services/Interfaces/ITransactionService.cs` - Transaction abstraction
- `Services/Implementations/TransactionService.cs` - EF Core transaction wrapper

**Files Created (Task 8 - Password Helper):**
- `Services/Utilities/PasswordHelper.cs` - PBKDF2-based password hashing

**Files Modified (Phase 4):**
- Controllers: BoardsController, TeamController, AzureController, FeaturesController, UserStoriesController (primary constructors)
- Services: BoardService, FeatureService, TeamService, SprintService (new), ValidationService (new)
- Data: AppDbContext (minimal changes)
- Infrastructure: Program.cs (registrations + middleware ordering)
- Configuration: appsettings.json, appsettings.Development.json (EF Core logging levels)

---

#### 1. Standardize Constructor Injection (1 hour)
**Current Issue:** Controllers use different injection patterns (traditional, primary constructors, arrow functions)

**Task:** Standardize all controllers to **primary constructor** pattern (.NET 8 feature):
```csharp
// Before (mixed styles)
public BoardsController(IBoardService boardService) { _boardService = boardService; }
public FeaturesController(IFeatureService featureService) : ControllerBase { ... }

// After (uniform primary constructor)
public BoardsController(IBoardService boardService) : ControllerBase { }
// Use boardService directly in methods
```

**Files to Update:**
- `Controllers/BoardsController.cs`
- `Controllers/TeamController.cs` 
- `Controllers/AzureController.cs`
- `Controllers/UserStoriesController.cs` (already using arrow function - align)

---

#### 2. Extract & Create SprintService (1 hour)
**Current Issue:** Sprint logic scattered across BoardService, FeatureService

**Task:** Create dedicated `SprintService` for all sprint operations:
```csharp
Services/Implementations/SprintService.cs
├── GenerateSprintsForBoard(board, numSprints, duration)
├── GetSprintDurationWorkDays(sprintId)
├── CalculateSprintMetrics(sprintId)
└── FindSprintByDate(boardId, date)
```

**Move From:**
- `BoardService.CreateBoardAsync()` - Sprint generation
- `FeatureService.CreateOrUpdateUserStory()` - Sprint iteration logic

**Wire Up:** In `Program.cs` dependency injection

---

#### 3. Add Structured Logging (1 hour)
**Current Issue:** Limited logging - only in exception handler
**Approach:** Option 3 - Built-in ILogger<T> + RequestCorrelationMiddleware (no file logging needed)
**Why:** Console output sufficient (Docker logs, Event Viewer on IIS, terminal in dev)

**Task 1: Create RequestCorrelationMiddleware**
```csharp
Middleware/RequestCorrelationMiddleware.cs (~30 lines)
├── Extract/generate correlation ID from headers
├── Add to LogContext (enriches all logs in request)
├── Log request start: "Request started: {Method} {Path}"
├── Call next middleware
└── Log request completion: "Request completed: {StatusCode}"

// Register in Program.cs (BEFORE MapControllers)
app.UseMiddleware<RequestCorrelationMiddleware>();
```

**Task 2: Add Structured Logging to Services**
```csharp
Services/Implementations/*.cs
├── BoardService (5-6 logs): creation, finalization, restore, updates
├── FeatureService (6-7 logs): imports, refreshes, moves, deletes
├── TeamService (4-5 logs): member additions, capacity updates, deletions
├── SprintService (3-4 logs): sprint creation, updates
└── AzureBoardsService (2-3 logs): Azure fetch operations, retries
```

**Logging Pattern:**
```csharp
// Constructor - inject ILogger<ServiceName>
public BoardService(IBoardRepository repo, ILogger<BoardService> logger)
{
    _boardRepository = repo;
    _logger = logger;
}

// Operations
_logger.LogInformation("Creating board '{BoardName}' with {NumSprints} sprints", dto.Name, dto.NumSprints);

// Validations
_logger.LogWarning("Attempting to add members to finalized board {BoardId}", boardId);

// Errors (before throwing)
_logger.LogError("Failed to fetch feature {FeatureId} from Azure: {ErrorMessage}", featureId, ex.Message);
```

**Log Levels:**
- `LogInformation` - Normal operations (creation, updates, successful calls)
- `LogWarning` - Potential issues (finalized board warnings, capacity at limit)
- `LogError` - Errors handled before throwing exception

**Correlation ID Enrichment:**
- All logs automatically include correlation ID (from middleware)
- Trace complete request flow: "Request A1B2C3D4 → Service1 → Service2 → Response"
- Useful for debugging multi-step operations

---

#### 4. Create ValidationService (Exception-Based) (1 hour)
**Current Issue:** Validation scattered in service methods
**Approach:** Option 1 - Exception-based (throws exceptions, middleware converts to HTTP responses)
**Why:** Already working in codebase, simple and fail-fast

**Task:** Extract to centralized ValidationService:
```csharp
Services/Interfaces/IValidationService.cs
├── Task ValidateBoardExists(int boardId)
├── Task ValidateStoryBelongsToBoard(int storyId, int boardId)
├── Task ValidateTeamMemberBelongsToBoard(int memberId, int boardId)
├── Task ValidateSprintBelongsToBoard(int sprintId, int boardId)
├── Task ValidateFeatureBelongsToBoard(int featureId, int boardId)
├── void ValidateBoardNotFinalized(Board board, string operation)
└── void ValidateTeamMemberCapacity(int capacity, int sprintWorkDays)

Services/Implementations/ValidationService.cs
├── All methods implement above interface
├── Throw exceptions (caught by GlobalExceptionHandlingMiddleware)
└── KeyNotFoundException (404), ArgumentException (400), InvalidOperationException (400)
```

**Implementation Pattern:**
```csharp
public async Task ValidateStoryBelongsToBoard(int storyId, int boardId)
{
    var story = await _featureRepo.GetUserStoryByIdAsync(storyId);
    if (story == null || story.Feature?.BoardId != boardId)
        throw new KeyNotFoundException("Story not found or doesn't belong to board");
}

public void ValidateBoardNotFinalized(Board board, string operation)
{
    if (board.IsFinalized)
        throw new InvalidOperationException($"Cannot {operation} on finalized board");
}
```

**Files to Update:**
- `Services/Interfaces/IValidationService.cs` - Create interface
- `Services/Implementations/ValidationService.cs` - Create service (7 methods)
- Replace inline checks in: `BoardService.cs`, `FeatureService.cs`, `TeamService.cs`
  - Remove: `if (story == null) return;` or `throw new Exception(...)`
  - Add: `await _validator.ValidateStoryBelongsToBoard(...)`
- `Program.cs` - Register: `services.AddScoped<IValidationService, ValidationService>();`

**Exception Mapping (Already Working):**
- GlobalExceptionHandlingMiddleware automatically handles:
  - KeyNotFoundException → HTTP 404
  - ArgumentException → HTTP 400
  - InvalidOperationException → HTTP 400

**Benefits:**
- ✅ Centralized, reusable validation
- ✅ Fail-fast (invalid data stops immediately)
- ✅ Automatic HTTP response handling
- ✅ Clean, readable service code
- ✅ No extra setup needed (middleware already in place)

---

#### 5. Add Transaction Management (1 hour)
**Current Issue:** Multi-step operations lack atomic guarantees

**Task:** Wrap complex operations in EF Core transactions:
```csharp
// Complex operations needing transactions:
- FeatureService.ImportFeatureToBoardAsync() 
  (Create/update feature + create/update stories)
- FeatureService.RefreshFeatureFromAzureAsync()
  (Fetch from Azure + update DB records)
- BoardService.FinalizeBoardAsync()
  (Set flag + track OriginalSprintIds + log operation)

Using(var tx = await _db.Database.BeginTransactionAsync()) {
  // Multi-step operation
  await tx.CommitAsync();
}
```

**Pattern:** 
- Wrap in service methods
- Logging captures transaction start/success/rollback
- Middleware exception handler ensures cleanup

---

#### 6. Refactor Azure Service Parameter Handling (30 min)
**Current Issue:** Each method requires `organization`, `project`, `pat` parameters

**Task:** Consider options:
- **Option A:** Store in request context (ThreadLocal or HttpContext.Items)
- **Option B:** Create `AzureContext` object passed through calls
- **Option C:** Store in board session/cache (for efficiency)

**Recommendation:** Start with **Option B** (pass object) - simplest & testable:
```csharp
public class AzureContext
{
    public string Organization { get; set; }
    public string Project { get; set; }
    public string Pat { get; set; }
}

// Before
await _azureService.GetFeatureWithChildrenAsync(org, project, featureId, pat);

// After
var azureContext = new AzureContext { Organization = org, Project = project, Pat = pat };
await _azureService.GetFeatureWithChildrenAsync(featureId, azureContext);
```

---

#### 7. Locate & Organize PasswordHelper (30 min)
**Current Issue:** `PasswordHelper.HashPassword()` used but location unclear

**Task:** 
- Find current implementation
- If custom: Move to `Services/Utilities/PasswordHelper.cs`
- If external: Document dependency in README
- Add XML documentation

---

## Acceptance Criteria:
- ✅ All controllers use primary constructor pattern (5 controllers: Boards, Team, Azure, Features, UserStories)
- ✅ SprintService created with 4+ methods (GenerateSprintsForBoard, GetSprintDurationWorkDays, CalculateSprintMetrics, FindSprintByDate)
- ✅ ValidationService created with 7 methods + IValidationService interface
  - Throws exceptions (KeyNotFoundException, ArgumentException, InvalidOperationException)
  - Wired in Program.cs: services.AddScoped<IValidationService, ValidationService>()
- ✅ RequestCorrelationMiddleware created and registered (enables correlation IDs across logs)
- ✅ Structured logging added to services (~20-25 log statements across 4-5 services)
  - BoardService, FeatureService, TeamService, SprintService, AzureBoardsService
  - Using ILogger<T> constructor injection
  - Log levels: Information, Warning, Error
  - Correlation IDs automatically enriched in logs
- ✅ Complex operations wrapped in EF Core transactions (ImportFeatureToBoardAsync, RefreshFeatureFromAzureAsync, FinalizeBoardAsync)
- ✅ Azure service refactored with AzureContext object (replaces org, project, pat parameters)
- ✅ PasswordHelper located and organized properly
- ✅ Build: 0 errors, 0 warnings
- ✅ All existing functionality preserved (refactoring only, no behavioral changes)
- ✅ Console logging works in all environments (Docker, IIS, development)

---

### PHASE 4.5: UI String Constants Consolidation — HIGH PRIORITY
**Status:** In Planning (branch: `uiStringConsolidation`)  
**Estimated Time:** 2 hours  
**Why:** Consolidate scattered string literals for better maintainability, i18n support, and consistency
**Runs in Parallel with Phase 4** (Combined: ~4.5 hours)

#### Structure:
Create modular constants organization in `shared/constants/`:
```typescript
src/app/shared/constants/
├── index.ts                    (exports all constants)
├── messages.constants.ts       (User messages, errors, notifications)
├── labels.constants.ts         (Button labels, form labels, headings)
├── placeholders.constants.ts   (Input placeholders, hints)
├── validations.constants.ts    (Validation error messages)
├── tooltips.constants.ts       (Hover titles, help text, aria-labels)
└── endpoints.constants.ts      (API routes, navigation paths)
```

#### Example Implementation:
```typescript
// shared/constants/messages.constants.ts
export const MESSAGES = {
  BOARD: {
    FINALIZED: 'This board is finalized and read-only',
    LOADING: 'Loading board...',
    ERROR: 'Error loading board',
    RESTORED: 'Board restored - editing is now allowed'
  },
  PAT: {
    REQUIRED: 'Azure DevOps PAT Required',
    HINT: 'Your PAT will not be stored. It\'s used only to verify access.',
    INVALID: 'Invalid Personal Access Token',
  },
  FEATURE: {
    IMPORTED: 'Feature imported successfully',
    REFRESHING: 'Refreshing feature from Azure...',
    REFRESH_ERROR: 'Failed to refresh feature from Azure',
  },
  TEAM: {
    MEMBER_ADDED: 'Team member added successfully',
    MEMBER_REMOVED: 'Team member removed',
    CAPACITY_UPDATED: 'Capacity updated successfully',
  },
  ERROR: {
    UNEXPECTED: 'An unexpected error occurred',
    CANNOT_ADD_MEMBERS: 'Cannot add members on finalized board',
    CANNOT_ADD_FEATURES: 'Cannot add features to finalized board',
    CANNOT_DELETE: 'Cannot delete items on finalized board',
  }
};

// shared/constants/labels.constants.ts
export const LABELS = {
  BUTTONS: {
    VALIDATE: 'Validate & Open Board',
    CANCEL: 'Cancel',
    RESTORE: 'Restore',
    FINALIZE: 'Finalize Board',
    DELETE: 'Delete',
    SAVE: 'Save',
    CLOSE: 'Close',
  },
  FORM: {
    PAT_INPUT: 'Personal Access Token (PAT)',
    BOARD_NAME: 'Board Name',
    ORGANIZATION: 'Organization',
    PROJECT: 'Project',
  }
};

// shared/constants/index.ts
export * from './messages.constants';
export * from './labels.constants';
export * from './placeholders.constants';
export * from './validations.constants';
export * from './tooltips.constants';
export * from './endpoints.constants';
```

#### Files to Update:
- All component templates (`*.html`) - Replace hardcoded strings with `{{ CONSTANTS.LABEL }}`
- All component TypeScript (`*.ts`) - Replace hardcoded strings with constant references
- Component imports: `import { MESSAGES, LABELS } from '@shared/constants';`

#### Coverage Areas:
- Board component messages & labels (finalization, restoration, loading)
- PAT modal text
- Team member management text
- Feature import/refresh messages
- Error messages & validation text
- Form placeholders & hints
- Button labels across all components
- Tooltips & aria-labels for accessibility

#### Acceptance Criteria:
- ✅ All user-facing strings moved to constants
- ✅ Constants organized in 6-7 files by category
- ✅ Exported via barrel export (index.ts)
- ✅ All templates & components use constant references
- ✅ No hardcoded strings in `.html` files
- ✅ No hardcoded strings in `.ts` for UI messages
- ✅ Build: 0 errors
- ✅ No functional changes - constants only

#### Future Benefits:
- 🌍 Ready for i18n/translation tool integration
- 🔄 Single source of truth for all UI text
- ✅ Type-safe string references
- 🐛 Easier to fix typos globally
- 🎨 Consistent terminology across app

---

### PHASE 4.6: Address All Warnings (Backend & Frontend) — OPEN PHASE
**Status:** Not Started  
**Estimated Time:** 1-3 hours (flexible, as warnings are discovered)  
**Why:** Eliminate compiler warnings incrementally to maintain clean, production-ready codebase
**Runs After:** Phase 4 & Phase 4.5 (can be done in parallel or after)
**Note:** This phase remains open - add warnings & fixes here as they surface during refactoring

#### Backend Warnings (.NET 8):
**Task:** Identify and fix all compiler warnings from `dotnet build` (as discovered):
- 📋 **NuGet package warnings** - Check for outdated dependencies
- 📋 **Nullable reference type warnings** - Add `!` operators or null checks where needed
- 📋 **Async/await warnings** - Ensure all async methods properly awaited
- 📋 **Obsolete API warnings** - Replace deprecated methods
- 📋 **Configuration warnings** - Resolve any setup/startup warnings
- 📋 *(Add more as discovered during Phase 4)*

**Process:**
1. Run `dotnet build --no-incremental 2>&1 | grep -i warning` to list all warnings
2. Document each warning with file, line, and category
3. Fix each warning with minimal code change
4. Verify no new warnings introduced after each fix
5. Clean rebuild to confirm 0 warnings

**Files to Audit:**
- `Program.cs` - Configuration & middleware setup
- `Controllers/*.cs` - API endpoint definitions
- `Services/Implementations/*.cs` - Business logic
- `Repositories/Implementations/*.cs` - Data access
- `Models/*.cs` - Entity definitions
- `Data/AppDBContext.cs` - EF Core configuration

#### Frontend Warnings (Angular 20):
**Task:** Identify and fix all warnings from `ng build` and `ng serve` (as discovered):
- 📋 **Strict mode warnings** - TypeScript strict mode violations
- 📋 **Deprecation warnings** - Angular deprecated APIs
- 📋 **ESLint warnings** - Code quality issues
- 📋 **Unused imports** - Clean up import statements
- 📋 **Bundle warnings** - CSS/JS budget exceed notices
- 📋 **Lifecycle warnings** - Improper component lifecycle usage
- 📋 **Type warnings** - Missing or incorrect type annotations
- 📋 *(Add more as discovered during Phase 4.5)*

**Process:**
1. Run `npm run build 2>&1 | grep -i warning` to capture all warnings
2. Run `ng lint` if available for code quality checks
3. Document each warning (file, line, category, severity)
4. Fix each warning prioritizing:
   - **Critical:** Functional issues
   - **High:** Performance/security
   - **Medium:** Type safety
   - **Low:** Code style
5. Verify compilation/build after each significant fix

**Files to Audit:**
- All component `.ts` files (signals, lifecycle, type safety)
- All component `.html` templates (bindings, null safety)
- All service `.ts` files (async operations, error handling)
- `app.config.ts` - Application configuration
- CSS files - Media query warnings, CSS3 compatibility

#### Bundle & Performance Warnings:
- **Current:** 768.05 kB bundle with "exceeded maximum budget" warning
- **Status:** Monitor and document as they surface
- **Future Consideration:** Implement lazy loading or tree-shaking if needed

#### Acceptance Criteria:
- ✅ Backend: Fix all discovered compiler warnings (incremental)
- ✅ Frontend: Fix all discovered ESLint critical/high warnings (incremental)
- ✅ Frontend: Fix all discovered TypeScript strict mode violations (incremental)
- ✅ Document each warning fixed with file, line, and fix description
- ✅ Phase remains "open" - new warnings added as discovered
- ✅ No deadline pressure (quality over speed)

#### Post-Phase 4.6 Benefits:
- 🟢 Green build pipeline (no warnings)
- 📦 Production-ready codebase
- 🔍 Easier to spot new warnings (no noise)
- 🚀 Better developer experience (clean compilation)
- 📊 Accurate code quality metrics

---

### PHASE 5: Windows IIS + SQL Server Support — HIGH PRIORITY (FOR STAKEHOLDER DEMO)
**Status:** Not Started  
**Estimated Time:** 5-6 hours  
**Depends On:** Phase 4 & Phase 4.5 & Phase 4.6 (all complete)
**Why:** Enables flexible deployment (Docker + IIS); required for stakeholder demo; shows production readiness

*Objective:* Enable deployment to Windows IIS server with SQL Server database support (in addition to Docker + PostgreSQL).

*Backend Tasks:*
- [ ] Conditional database provider setup in Program.cs
  - Environment variable: `DB_PROVIDER` (values: `PostgreSQL`, `SqlServer`)
  - PostgreSQL connection string in appsettings.json
  - SQL Server connection string in appsettings.json
  - DbContext configuration switches provider based on `DB_PROVIDER`
- [ ] SQL Server migrations created (separate migration set for SQL Server)
  - EF Core migration: `Add-Migration InitialCreate_SqlServer -Context AppDBContext`
  - All tables, relationships, constraints match PostgreSQL schema
  - Tested against SQL Server 2019+ (or Azure SQL Database)
- [ ] Program.cs initialization:
  - Detects `DB_PROVIDER` and configures appropriate connection string
  - Runs EF Core migrations automatically on startup (both providers)
  - Logs which database provider is active
- [ ] No Docker dependency required (self-contained executable)
  - Publish profile for Windows IIS: bin/Release/net8.0/win-x64/publish
  - appsettings.Production.json configured for IIS

*Deployment Documentation:*
- [ ] **IIS_DEPLOYMENT_GUIDE.md** created with:
  - **Prerequisites:** Windows Server 2022+, .NET 8 hosting bundle, SQL Server 2019+ (or connection string to remote SQL Server)
  - **Automated Setup (PowerShell):** Script to automate basic IIS setup (optional)
  - **Manual Setup Steps:**
    1. Clone repo: `git clone <repo>`
    2. Configure appsettings.json with SQL Server connection string + `"DB_PROVIDER": "SqlServer"`
    3. Build: `dotnet build`
    4. Publish: `dotnet publish -c Release -o ./publish`
    5. Copy publish folder to IIS directory (e.g., `C:\inetpub\wwwroot\pi-planning\`)
    6. Create IIS application pool (.NET CLR version: No Managed Code, Identity: ApplicationPoolIdentity)
    7. Create IIS website pointing to publish folder
    8. Set app pool identity permissions on publish folder (Read, Execute)
    9. Verify web.config exists in publish folder (auto-generated by .NET)
    10. Test: Navigate to `http://localhost/pi-planning/`
  - **Windows authentication setup** (optional): If using Windows auth instead of app auth
  - **Troubleshooting:**
    - 500 errors → Check Event Viewer (Application logs)
    - Database connection failures → Verify connection string in appsettings.json
    - Missing .NET runtime → Install .NET 8 Hosting Bundle
    - Permission errors → Check app pool identity + folder permissions
  - **Logs:** IIS logs in `C:\inetpub\logs\LogFiles\`

*Frontend (Angular):*
- [ ] API base URL configurable for IIS deployment
  - environment.ts: `apiUrl: 'http://localhost:4200/api'` (dev)
  - environment.prod.ts: `apiUrl: '/pi-planning/api'` (IIS path)
  - Build: `ng build --configuration production --base-href=/pi-planning/`

*Testing:*
- [ ] PostgreSQL: Fresh rebuild and verify all operations
- [ ] SQL Server: Fresh rebuild with SQL Server connection string
- [ ] IIS Deployment: Deploy to test IIS, verify app runs
- [ ] Build: `dotnet build` → 0 errors, 0 warnings (both providers)
- [ ] No Docker required (self-hosted deployment works)

*Acceptance Criteria:*
- ✅ App runs equally on Docker (PostgreSQL) and Windows IIS (SQL Server)
- ✅ Database agnostic architecture (provider can be switched without code changes)
- ✅ IIS deployment guide allows anyone to deploy without Docker knowledge
- ✅ Zero breaking changes to API or UI
- ✅ All existing functionality works on both platforms
- ✅ Stakeholder can demo on Windows IIS with SQL Server

---

### PHASE 6: Real-time Collaboration (SignalR) — HIGH PRIORITY
**Status:** Not Started  
**Estimated Time:** 4-6 hours  
**Depends On:** Phase 4, 4.5, 4.6, 5 (all complete)
**Why:** Enables multi-user concurrent editing; core differentiator feature

#### Features to Implement:
1. **Cursor Presence Broadcast** - Show which user is viewing what section
2. **Live Move Updates** - When one user moves a story, all users see it instantly
3. **Live Team Member Updates** - Real-time team/capacity changes across clients
4. **Conflict Resolution** - Handle concurrent story movements safely

#### Backend Changes:
- `Hubs/PlanningHub.cs` - Implement message handlers
  - `BroadcastCursorUpdate(userId, boardId, position)`
  - `BroadcastStoryMove(storyId, fromSprintId, toSprintId)`
  - `BroadcastTeamMemberUpdate(boardId, teamMemberId)`
  - `BroadcastFeatureUpdate(featureId, newData)`

#### Frontend Changes:
- Create `features/board/services/signalr.service.ts` - Hub connection management
- Update board components to subscribe to hub events
- Add cursor position tracking
- Add live update handlers for stories, team members, features

#### Acceptance Criteria:
- ✅ Multiple users can connect to same board
- ✅ Cursor positions update in real-time
- ✅ Story moves broadcast to all connected clients
- ✅ Team member updates sync across clients
- ✅ No race conditions or data corruption on concurrent moves
- ✅ Build: 0 errors
- ✅ Manual testing: Concurrent multi-user scenarios work

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
**✅ Connection strings:** Both in appsettings.json, active one selected via `DB_PROVIDER` env var
**✅ IIS setup:** Mixture of automated PowerShell script + manual step-by-step guide
**✅ Windows authentication:** Support is optional, not required
**✅ Priority:** FOR STAKEHOLDER DEMO (before SignalR)

### Phase 6 (SignalR) - Decisions
**✅ Testing approach:** Manual testing (concurrent multi-user scenarios)

### Phase 7 (Board Lock/Unlock) - Decisions
**✅ Finalized & Locked boards:** SEPARATE STATES (not mutually exclusive)
- A board can be: finalized only, locked only, or BOTH finalized AND locked
- UI shows both states independently with separate indicators

### Phase 8 (Documentation) - Decisions
**✅ Scope:** Comprehensive documentation + integration testing (3-4 hours)

---

## 🔄 Updated Phase Dependency Chain

```
Phases 4 → 4.5 → 4.6 → 5 → 6 → 7 → 8
(mostly sequential; 4.5 & 4.6 can run in parallel with 4)

4: Backend Refactoring (5-6 hrs)
  ↓ (foundation for all following)
  ├─ 4.5: UI String Constants (2 hrs) [parallel with 4]
  │  ↓
  └─ 4.6: Warning Elimination (1-3 hrs, open-ended) [parallel or after 4]
     ↓ (codebase refactored & clean)
     5: Windows IIS + SQL Server Support (5-6 hrs) [FOR STAKEHOLDER DEMO]
        ↓ (deployment flexibility achieved)
        6: SignalR Real-time Collaboration (4-6 hrs)
           ↓
           7: Board Lock/Unlock Endpoints (2-3 hrs)
              ↓
              8: Documentation & Integration Testing (3-4 hrs)

Timeline: ~26-34 hours total
Optimized: ~19-23 hours if 4.5 & 4.6 run simultaneously with 4

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

