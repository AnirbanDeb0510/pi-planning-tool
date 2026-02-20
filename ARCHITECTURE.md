# PI Planning Tool - Architecture & Development Guide

**Version:** 1.0  
**Last Updated:** February 6, 2026  
**Team:** Full-stack development

---

## 📐 System Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        CLIENT (Angular)                         │
│                                                                 │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────┐  │
│  │  Board Component │  │  Team Component  │  │ Azure Modal  │  │
│  └────────┬─────────┘  └────────┬─────────┘  └──────┬───────┘  │
│           │                     │                   │           │
│           └─────────────────────┼───────────────────┘           │
│                                 │                               │
│                    ┌────────────▼─────────────┐                │
│                    │   HTTP Client Service   │                │
│                    │   SignalR Hub Client    │                │
│                    └────────────┬─────────────┘                │
│                                 │ REST + WebSocket              │
└─────────────────────────────────┼───────────────────────────────┘
                                  │
                    ┌─────────────▼──────────────┐
                    │   API Gateway / CORS       │
                    └─────────────┬──────────────┘
                                  │
┌─────────────────────────────────┼───────────────────────────────┐
│                        SERVER (.NET 8)                          │
│                                 │                               │
│  ┌──────────────────────────────▼──────────────────────┐       │
│  │              Controller Layer                       │       │
│  │                                                     │       │
│  │  - BoardsController    (GET, POST, PATCH)         │       │
│  │  - FeaturesController  (POST import, PATCH)       │       │
│  │  - UserStoriesController (PATCH move/refresh)     │       │
│  │  - TeamController      (GET, POST, PATCH)         │       │
│  │  - AzureController     (GET feature from Azure)   │       │
│  └──────────────────────────────┬──────────────────────┘       │
│                                 │                               │
│  ┌──────────────────────────────▼──────────────────────┐       │
│  │              Service Layer (Business Logic)        │       │
│  │                                                     │       │
│  │  - IBoardService         (create, fetch, lock)    │       │
│  │  - IFeatureService       (import, move, refresh)  │       │
│  │  - ITeamService          (capacity management)    │       │
│  │  - IAzureBoardsService   (Azure DevOps client)    │       │
│  └──────────────────────────────┬──────────────────────┘       │
│                                 │                               │
│  ┌──────────────────────────────▼──────────────────────┐       │
│  │              Repository Layer (Data Access)        │       │
│  │                                                     │       │
│  │  - IBoardRepository                               │       │
│  │  - IFeatureRepository                             │       │
│  │  - IUserStoryRepository                           │       │
│  │  - ITeamRepository                                │       │
│  └──────────────────────────────┬──────────────────────┘       │
│                                 │                               │
│  ┌──────────────────────────────▼──────────────────────┐       │
│  │              EF Core DbContext                     │       │
│  │  AppDbContext                                      │       │
│  │  ├── DbSet<Board>                                 │       │
│  │  ├── DbSet<Sprint>                                │       │
│  │  ├── DbSet<Feature>                               │       │
│  │  ├── DbSet<UserStory>                             │       │
│  │  ├── DbSet<TeamMember>                            │       │
│  │  ├── DbSet<TeamMemberSprint>                      │       │
│  │  └── DbSet<CursorPresence> (Ignored)             │       │
│  └──────────────────────────────┬──────────────────────┘       │
│                                 │                               │
│  ┌──────────────────────────────▼──────────────────────┐       │
│  │            SignalR Hub (Real-time)                 │       │
│  │  PlanningHub                                       │       │
│  │  - HandleFeatureMoved()                           │       │
│  │  - HandleStoryMoved()                             │       │
│  │  - HandleCursorUpdate()                           │       │
│  └──────────────────────────────┬──────────────────────┘       │
│                                 │                               │
└─────────────────────────────────┼───────────────────────────────┘
                                  │
┌─────────────────────────────────▼───────────────────────────────┐
│                    Database (PostgreSQL)                        │
│                                                                 │
│  ┌────────┬────────┬────────┬──────────┬──────────┬──────────┐  │
│  │ Boards │ Sprints│Features│UserStories│TeamMember│TMSprints│  │
│  └────────┴────────┴────────┴──────────┴──────────┴──────────┘  │
│                                                                 │
│  Persistence Layer: Docker volume (./db/pg-data)              │
└─────────────────────────────────────────────────────────────────┘
```

---

## � Data Validation Architecture

### Three-Layer Validation Strategy

Team member data is validated at three distinct layers for defense-in-depth:

**Layer 1: Frontend Form Validation**
- Component checks before API call (member name, capacity bounds)
- HTML constraints prevent invalid input (step="1", min="0")
- Error signals display to user, prevent form submission
- Files: board.ts, board.html, board.css

**Layer 2: DTO-Level Data Annotations**
- ASP.NET Core automatic ModelState validation
- [Required], [StringLength], [Range] attributes
- Returns HTTP 400 if validation fails (prevents controller)
- Files: TeamMemberDto.cs, UpdateTeamMemberCapacityDto.cs

**Layer 3: Service Business Logic Validation**
- Complex rules enforced in service methods
- Add/Update: Name non-empty, at least one role (Dev or Test)
- UpdateCapacity: Capacity ≤ sprint working days
- GlobalExceptionHandlingMiddleware catches ArgumentException → HTTP 400
- Files: TeamService.cs

### Capacity Type System

All capacity fields use **int** (positive integers only):
- Database columns: integer (not float)
- C# models: int type
- DTOs: int type
- TypeScript interfaces: number type (stored as int)
- HTML inputs: type="number" step="1" prevents decimals

**Working Days Calculation:**
```
totalDays = (endDate - startDate).Days + 1
workingDays = floor((totalDays / 7) * 5)
// Result: 5 working days per 7 calendar days
```

Example: Sprint Feb 10-21 (12 calendar days) = 8 working days max capacity

---
## 🔒 Security Architecture - PAT Validation

### Board Preview & Access Control

The system implements a two-phase board loading strategy to prevent unauthorized data access:

**Phase 1: Board Preview (Lightweight Metadata)**
```
GET /api/boards/{id}/preview
Returns: {
  id, name, organization, project,
  featureCount, sampleFeatureAzureId,
  isLocked, isFinalized
}
```
- NO sensitive data (features, stories, team members)
- Used to determine if PAT validation required
- Safe to call without authentication

**Phase 2: PAT Validation Flow**
```
1. User navigates to /boards/{id}
2. Frontend calls preview endpoint
3. If featureCount > 0:
   a. Show PAT modal (user not yet authenticated)
   b. User enters PAT
   c. Validate PAT by calling Azure API:
      - Organization: from preview
      - Project: from preview
      - Feature ID: sampleFeatureAzureId from preview
   d. If valid → Store PAT temporarily (10 min TTL)
   e. If invalid → Show error, allow retry
4. If featureCount === 0:
   - Skip PAT validation (no Azure features)
5. Load full board data: GET /api/boards/{id}
```

### Security Benefits

- **Data Leak Prevention:** Full board API never called until PAT validated
- **Network Tab Safety:** Sensitive data not visible in browser dev tools
- **Minimal Exposure:** Preview only returns metadata needed for validation
- **Temporary PAT Storage:** 10-minute TTL, cleared on tab close
- **Re-validation:** PAT required on each navigation (no persistent session storage)

### Implementation Files

- Backend: `BoardsController.GetBoardPreview()`, `BoardService.GetBoardPreviewAsync()`
- Frontend: `BoardService.getBoardPreview()`, `Board.ngOnInit()`, PAT modal
- Types: `BoardSummaryDto` with optional `sampleFeatureAzureId`

---
## �🔄 Data Flow Examples

### Flow 1: Create Board → Auto-Generate Sprints

```
1. Client: POST /api/boards { name, org, project, numSprints, sprintDuration, ... }
   ↓
2. BoardsController.CreateBoard(BoardCreateDto dto)
   ↓
3. BoardService.CreateBoardAsync(dto)
   - Create Board entity
   - Loop i = 0 to numSprints
     - Create Sprint i with calculated dates
   ↓
4. BoardRepository.AddAsync(board)
   ↓
5. EF Core: INSERT Board → Sprints
   ↓
6. Return: HTTP 201 Created { id, name, sprints: [...] }
   ↓
7. Client stores boardId for future requests
```

### Flow 2: Fetch Feature from Azure → Import → Placeholder

```
1. Client: Clicks "Fetch from Azure"
   - Opens modal with org, project, featureId, PAT
   ↓
2. Client: GET /api/feature/{org}/{project}/{featureId}?pat={pat}
   ↓
3. AzureController.GetFeatureWithChildren(org, project, featureId, pat)
   ↓
4. AzureBoardsService.GetFeatureWithChildrenAsync(org, project, id, pat)
   - Calls Azure DevOps REST API
   - Returns FeatureDto with children UserStoryDtos
   ↓
5. Client receives FeatureDto, shows preview, user clicks "Add to Board"
   ↓
6. Client: POST /api/v1/boards/{boardId}/features/import { featureDto }
   ↓
7. FeaturesController.ImportFeature(boardId, featureDto)
   ↓
8. FeatureService.ImportFeatureToBoardAsync(boardId, featureDto)
   - Check for existing Feature (by AzureId)
   - Create or update Feature
   - For each child UserStory:
     - Check for existing (by AzureId + FeatureId)
     - Create or update UserStory
     - Assign SprintId = sprints[0].Id (Placeholder/Sprint 0)
   ↓
9. Repository.SaveChangesAsync()
   ↓
10. Return: HTTP 201 Created { id, title, children: [...] }
   ↓
11. Client: Board now shows feature in Placeholder column
```

### Flow 3: Move Story → Sprint

```
1. Client: Drags story card from Sprint 0 to Sprint 2
   ↓
2. CDK drag-drop event fires
   ↓
3. Client: PATCH /api/boards/{boardId}/stories/{storyId}/move
           { targetSprintId: 2 }
   ↓
4. UserStoriesController.MoveStory(boardId, storyId, dto)
   ↓
5. FeatureService.MoveUserStoryAsync(boardId, storyId, targetSprintId)
   - Fetch UserStory from DB
   - story.SprintId = targetSprintId
   - story.IsMoved = (originalSprintId != currentSprintId)
   ↓
6. Repository.UpdateAsync(story) → SaveChangesAsync()
   ↓
7. Return: HTTP 204 No Content
   ↓
8. [Future] SignalR broadcasts StoryMoved event to other clients
   ↓
9. Client: Updates local state, story appears in new sprint
```

### Flow 4: Lock Board

```
1. User clicks "Lock Board" button
   ↓
2. Client: PATCH /api/boards/{id}/lock { password?: "..." }
   ↓
3. BoardsController.LockBoard(id, password)
   ↓
4. BoardService.LockBoardAsync(id, password)
   - Fetch board
   - If board.PasswordHash exists, verify password
   - board.IsLocked = true
   ↓
5. Repository.UpdateAsync(board) → SaveChangesAsync()
   ↓
6. Return: HTTP 204 No Content
   ↓
7. Client: DisableMove operations, highlight moved stories
```

---

## 🗄️ Data Model Details

### Core Entities

#### Board
- **Purpose:** Represents a single PI planning session
- **Key Fields:**
  - `Id` (PK)
  - `Name` (e.g., "PI 25 Planning")
  - `Organization`, `Project` (Azure info)
  - `NumSprints`, `SprintDuration` (e.g., 2-week sprints)
  - `DevTestToggle` (split points or total)
  - `StartDate` (when PI starts)
  - `IsLocked` (editing disabled)
  - `PasswordHash` (optional: password protect)
  - `IsFinalized` (visual mode for moved stories)
- **Relationships:**
  - Has many Sprints (auto-generated)
  - Has many Features (imported from Azure)
  - Has many TeamMembers (configured by user)

#### Sprint
- **Purpose:** Iteration within a Board
- **Key Fields:**
  - `Id` (PK)
  - `BoardId` (FK)
  - `Name` (e.g., "Sprint 0", "Sprint 1")
  - `StartDate`, `EndDate`
- **Note:** Sprint 0 is Placeholder (always created, starts before Sprint 1)
- **Relationships:**
  - Belongs to Board
  - Has many UserStories
  - Has many TeamMemberSprints (capacity per person)

#### Feature
- **Purpose:** Epic or feature from Azure DevOps
- **Key Fields:**
  - `Id` (PK)
  - `BoardId` (FK)
  - `AzureId` (from Azure DevOps)
  - `Title` (from Azure)
  - `Priority` (manual order on board, -1 = placeholder)
  - `ValueArea` (Business/Architectural/etc.)
- **Relationships:**
  - Belongs to Board
  - Has many UserStories

#### UserStory
- **Purpose:** Work item (usually a User Story) under a Feature
- **Key Fields:**
  - `Id` (PK)
  - `FeatureId` (FK)
  - `AzureId` (from Azure DevOps)
  - `Title` (from Azure)
  - `StoryPoints` (total effort)
  - `DevStoryPoints` (if DevTestToggle=true)
  - `TestStoryPoints` (if DevTestToggle=true)
  - `SprintId` (current assignment, FK)
  - `OriginalSprintId` (baseline before moves, nullable)
  - `IsMoved` (boolean, computed: OriginalSprintId != SprintId)
  - `Notes` (user-added risks/dependencies)
- **Relationships:**
  - Belongs to Feature
  - Belongs to Sprint (current)
  - Can belong to Sprint (original)

#### TeamMember
- **Purpose:** Person on the team planning
- **Key Fields:**
  - `Id` (PK)
  - `BoardId` (FK)
  - `Name`
  - `IsDev` (can do dev work)
  - `IsTest` (can do test work)
- **Relationships:**
  - Belongs to Board
  - Has many TeamMemberSprints

#### TeamMemberSprint
- **Purpose:** Capacity per team member per sprint
- **Key Fields:**
  - `Id` (PK)
  - `TeamMemberId` (FK)
  - `SprintId` (FK)
  - `CapacityDev` (e.g., 10 points / sprint)
  - `CapacityTest` (e.g., 5 points / sprint)
- **Relationships:**
  - Belongs to TeamMember
  - Belongs to Sprint

#### CursorPresence
- **Purpose:** Real-time cursor tracking (NOT persisted)
- **Note:** Marked as `Ignored` in EF Core; only in SignalR messages
- **Usage:** Ephemeral, no DB storage

---

## 🎯 API Contract Reference

### Boards

```
POST /api/boards
  Request: BoardCreateDto { name, org, project, numSprints, sprintDuration, startDate, devTestToggle, password? }
  Response: Board { id, name, sprints: [...], ... }

GET /api/boards/{id}
  Response: BoardResponseDto { id, name, sprints, features, teamMembers, isLocked, ... }

PATCH /api/boards/{id}/lock
  Request: { password?: string }
  Response: 204 No Content

PATCH /api/boards/{id}/unlock
  Request: { password?: string }
  Response: 204 No Content
```

### Features

```
GET /api/v1/azure/feature/{org}/{project}/{featureId} (from Azure)
  Query: ?pat={personalAccessToken}
  Response: FeatureDto { azureId, title, priority, children: [UserStoryDto, ...] }

POST /api/v1/boards/{boardId}/features/import
  Request: FeatureDto { ... }
  Response: 201 Created FeatureDto { id, title, children: [...] }

PATCH /api/v1/boards/{boardId}/features/{id}/refresh
  Query: ?org=&project=&pat=
  Response: FeatureDto { ... }

PATCH /api/v1/boards/{boardId}/features/reorder
   Request: ReorderFeatureDto { features: [{ featureId, newPriority }, ...] }
  Response: 204 No Content

DELETE /api/v1/boards/{boardId}/features/{id}
   Response: 204 No Content
```

### User Stories

```
PATCH /api/boards/{boardId}/stories/{storyId}/move
  Request: MoveStoryDto { targetSprintId: int }
  Response: 204 No Content

PATCH /api/boards/{boardId}/stories/{storyId}/refresh
  Query: ?org=&project=&pat=
  Response: UserStoryDto { ... }
```

### Team

```
POST /api/boards/{boardId}/team
  Request: List<TeamMemberDto> { name, isDev, isTest }
  Response: 200 OK

GET /api/boards/{boardId}/team
  Response: List<TeamMemberDto> { ... }

PATCH /api/boards/{boardId}/team/sprints/{sprintId}/team/{teamMemberId}
  Request: { capacityDev: double, capacityTest: double }
  Response: TeamMemberSprintDto { ... }
```

---

## 🏗️ Service Layer Patterns

### Pattern 1: Service Methods are Authoritative

**Rule:** NEVER do business logic in controllers. Controllers are thin wrappers.

```csharp
// ✅ CORRECT
[HttpPatch("{id}/move")]
public async Task<IActionResult> MoveStory(int boardId, int storyId, MoveStoryDto dto)
{
    await _featureService.MoveUserStoryAsync(boardId, storyId, dto.TargetSprintId);
    return NoContent();
}

// Then in Service:
public async Task MoveUserStoryAsync(int boardId, int storyId, int targetSprintId)
{
    // Fetch + validate
    var story = await _storyRepo.GetByIdAsync(storyId);
    if (story == null || story.Feature?.BoardId != boardId) 
        throw new UnauthorizedException();
    
    // Business logic
    story.SprintId = targetSprintId;
    story.IsMoved = story.OriginalSprintId != story.SprintId;
    
    // Persist
    await _storyRepo.UpdateAsync(story);
    await _storyRepo.SaveChangesAsync();
}

// ❌ WRONG
[HttpPatch("{id}/move")]
public async Task<IActionResult> MoveStory(int boardId, int storyId, int targetSprintId)
{
    // No! This embeds logic in controller
    var story = await _context.UserStories.FindAsync(storyId);
    story.SprintId = targetSprintId;
    story.IsMoved = story.OriginalSprintId != story.SprintId;
    await _context.SaveChangesAsync();
    return NoContent();
}
```

### Pattern 2: Repositories are Thin

**Rule:** Repositories only query & persist. No cross-entity decisions.

```csharp
// ✅ CORRECT
public async Task<UserStory?> GetByIdAsync(int id)
{
    return await _context.UserStories.FindAsync(id);
}

public async Task<List<UserStory>> GetByAzureIdAsync(string azureId, int featureId)
{
    return await _context.UserStories
        .Where(u => u.AzureId == azureId && u.FeatureId == featureId)
        .ToListAsync();
}

// ❌ WRONG
public async Task<bool> MoveUserStoryAsync(int storyId, int targetSprintId)
{
    // No! Business logic doesn't belong in repository
    var story = await _context.UserStories.FindAsync(storyId);
    story.SprintId = targetSprintId;
    story.IsMoved = story.OriginalSprintId != story.SprintId;
    return true;
}
```

### Pattern 3: Use DTOs for API Contracts

**Rule:** DTOs represent UI intent. Entities represent storage truth.

```csharp
// ✅ CORRECT
[HttpPost("import")]
public async Task<IActionResult> ImportFeature(int boardId, [FromBody] FeatureDto dto)
{
    // Mapping happens in service, not here
    var created = await _featureService.ImportFeatureToBoardAsync(boardId, dto);
    return CreatedAtAction(nameof(GetFeature), new { boardId, id = created.Id }, created);
}

// ❌ WRONG
[HttpPost("import")]
public async Task<IActionResult> ImportFeature(int boardId, [FromBody] Feature entity)
{
    // Entities should not be in HTTP contracts
    await _featureRepo.AddAsync(entity);
    return Ok();
}
```

### Pattern 4: Eager Load > Lazy Load

**Rule:** For board fetch, get everything in one query.

```csharp
// ✅ CORRECT
public async Task<Board?> GetBoardWithHierarchyAsync(int boardId)
{
    return await _context.Boards
        .Include(b => b.Sprints)
        .Include(b => b.Features)
            .ThenInclude(f => f.UserStories)
        .Include(b => b.TeamMembers)
            .ThenInclude(tm => tm.TeamMemberSprints)
        .FirstOrDefaultAsync(b => b.Id == boardId);
}

// ❌ AVOID (N+1 queries)
public async Task<Board?> GetBoardAsync(int boardId)
{
    var board = await _context.Boards.FindAsync(boardId);
    var sprints = await _context.Sprints.Where(s => s.BoardId == boardId).ToListAsync();
    var features = await _context.Features.Where(f => f.BoardId == boardId).ToListAsync();
    // ... etc. Multiple DB round-trips!
    return board;
}
```

---

## 🛠️ Development Workflow

### Adding a New Feature

1. **Define Domain Model** (Models/*)
   - Create entity class
   - Add properties
   - Configure EF relationships

2. **Create Repository Interface** (Repositories/Interfaces/*)
   ```csharp
   public interface IMyRepository
   {
       Task<MyEntity?> GetByIdAsync(int id);
       Task<List<MyEntity>> GetAllAsync();
       Task AddAsync(MyEntity entity);
       Task UpdateAsync(MyEntity entity);
       Task SaveChangesAsync();
   }
   ```

3. **Implement Repository** (Repositories/Implementations/*)
   ```csharp
   public class MyRepository : IMyRepository
   {
       private readonly AppDbContext _context;
       public MyRepository(AppDbContext context) => _context = context;
       
       // Implement methods
   }
   ```

4. **Create Service Interface** (Services/Interfaces/*)
   ```csharp
   public interface IMyService
   {
       Task<MyDto> CreateAsync(MyCreateDto dto);
       Task<MyDto?> GetAsync(int id);
   }
   ```

5. **Implement Service** (Services/Implementations/*)
   ```csharp
   public class MyService : IMyService
   {
       private readonly IMyRepository _repo;
       // Implement business logic
   }
   ```

6. **Create DTO** (DTOs/*)
   ```csharp
   public class MyDto
   {
       public int Id { get; set; }
       public string Name { get; set; }
   }
   ```

7. **Create Controller** (Controllers/*)
   ```csharp
   [ApiController]
   [Route("api/[controller]")]
   public class MyController(IMyService service) : ControllerBase
   {
       [HttpPost]
       public async Task<IActionResult> Create([FromBody] MyCreateDto dto)
       {
           var result = await service.CreateAsync(dto);
           return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
       }
   }
   ```

8. **Register in DI** (Program.cs)
   ```csharp
   builder.Services.AddScoped<IMyRepository, MyRepository>();
   builder.Services.AddScoped<IMyService, MyService>();
   ```

9. **Create Migration**
   ```bash
   dotnet ef migrations add AddMyEntity
   dotnet ef database update
   ```

10. **Test**
    - Postman/Swagger POST request
    - Verify DB insert
    - Check API response

---

## 📚 Design Decisions & Why

| Decision | Reason |
|----------|--------|
| **Placeholder Sprint 0** | User control over distribution; avoids auto-split confusion |
| **SprintId + OriginalSprintId** | Enables move tracking; clean finalization logic |
| **DevTestToggle** | Flexible story point model; fits Agile (some stories test, some code) |
| **Reuse DTOs** | Simpler; Azure response → Import DTOs → API response are same shape |
| **Service-centric logic** | Testable, reusable, controllers stay thin |
| **Eager loading** | Single round-trip better than N+1; reasonable for board sizes |
| **CursorPresence ignored** | Ephemeral; no need for DB storage; SignalR only |
| **Password hashing** | Basic security for board locking |

---

## 🎨 Frontend UI Component Architecture (Phase 3A+3B - Feb 20, 2026)

### Phase 3A: Board Component Refactoring

The board component has been modernized using Angular 15+ standalone component architecture. The monolithic board component is now decomposed into 6 focused subcomponents, each with scoped styling and clear responsibilities.

### Component Hierarchy

```
Board (Main Container)
├── BoardHeader (Toggle & Dev/Test Mode)
├── TeamBar (Team Members Management)
├── CapacityRow (Sprint Capacity Display & Edit)
├── SprintHeader (Column Headers & Metrics)
├── FeatureRow × N (Feature Cards with Stories)
└── BoardModals (Import, Finalize, Delete Dialogs)
```

### Subcomponent Details

| Component | Purpose | Key Features | Files |
|-----------|---------|--------------|-------|
| **BoardHeader** | Top bar with mode toggles | Dev/Test toggle, finalization banner | board-header.ts/html/css |
| **TeamBar** | Team member management | Add/edit/delete members, modal dialogs | team-bar.ts/html/css |
| **CapacityRow** | Team capacity visualization | Edit modal with 60/20/20 layout, dark mode | capacity-row.ts/html/css |
| **SprintHeader** | Column headers | Sprint names, load/capacity bars, metrics | sprint-header.ts/html/css |
| **FeatureRow** | Feature & story container | Drag-drop zones, story cards, dev/test split | feature-row.ts/html/css |
| **BoardModals** | Dialog overlays | Feature import, finalization warnings, delete | board-modals.ts/html/css |

### Phase 3B: Application Architecture Restructuring

Migrated from flat folder structure to **domain-driven architecture** for improved scalability and maintainability:

**New Folder Structure:**
```
frontend/pi-planning-ui/src/
├── app/
│   ├── core/
│   │   ├── services/
│   │   │   └── user.service.ts        # PAT & user state (persistent)
│   │   └── guards/
│   │
│   ├── shared/
│   │   ├── models/                    # DTOs & data types
│   │   ├── components/               # Reusable: story-card, enter-your-name
│   │   └── utilities/
│   │
│   ├── features/
│   │   ├── board/
│   │   │   ├── components/           # Main board + 6 subcomponents
│   │   │   ├── services/             # 5 split services (see below)
│   │   │   │   ├── board.service.ts
│   │   │   │   ├── feature.service.ts
│   │   │   │   ├── team.service.ts
│   │   │   │   ├── story.service.ts
│   │   │   │   └── sprint.service.ts
│   │   │   ├── models/
│   │   │   └── index.ts              # Barrel export
│   │   │
│   │   └── home/
│   │       ├── components/
│   │       └── index.ts              # Barrel export
│   │
│   ├── app.component.ts
│   ├── app.routes.ts
│   └── app.config.ts
│
├── styles/
└── index.html
```

**Service Split (from 850-line monolith):**

| Service | Responsibility | LOC | Key Methods |
|---------|-----------------|-----|------------|
| **board.service.ts** | Board fetch, PAT handling, state | 200 | `getBoard()`, `validatePAT()`, `setBoardState()` |
| **feature.service.ts** | Feature import/refresh/reorder | 150 | `importFeatures()`, `refreshFeature()`, `reorderFeatures()` |
| **team.service.ts** | Team members & capacity | 120 | `addMember()`, `updateCapacity()`, `deleteMember()` |
| **story.service.ts** | Story movement between sprints | 100 | `moveStory()`, `refreshStory()`, `getStoryPosition()` |
| **sprint.service.ts** | Sprint utilities & calculations | 80 | `calculateMetrics()`, `getSprintPath()`, `formatSprintName()` |

**Barrel Exports (8 files):**
- `core/index.ts` - UserService
- `shared/models/index.ts` - All DTOs
- `shared/components/index.ts` - Story-card, Enter-your-name
- `features/board/index.ts` - Board component + services
- `features/board/components/index.ts` - All subcomponents
- `features/board/services/index.ts` - All 5 services
- `features/board/models/index.ts` - Board-specific types
- `features/home/index.ts` - Home component

**TypeScript Path Aliases (tsconfig.json):**
```json
{
  "compilerOptions": {
    "paths": {
      "@core/*": ["src/app/core/*"],
      "@shared/*": ["src/app/shared/*"],
      "@features/*": ["src/app/features/*"]
    }
  }
}
```

**Import Pattern Before & After:**
```typescript
// Before (flat structure)
import { BoardService } from '../../../services/board.service';

// After (domain-driven with alias)
import { BoardService } from '@features/board/services';
```

### State Management & Service Layer

- **Signal-based:** Board owns `showDevTest` signal, passes as @Input to children
- **Reactive:** Changes propagate immediately through component tree
- **Per-component:** Each component manages its own local state (modals, edits)
- **Service Isolation:** Each service handles one domain; no circular dependencies

### CSS Architecture & Dark Mode

**CSS Distribution (Total: 2046 lines):**
- board.css: 214 lines (global layout, PAT modal, responsive)
- board-header.css: 106 lines (toggle styles, banner)
- board-modals.css: 470 lines (modal dialogs, form styling)
- capacity-row.css: 352 lines (capacity display, edit modal)
- feature-row.css: 311 lines (drag-drop styles, story cards)
- sprint-header.css: 193 lines (header, metrics display)
- team-bar.css: 400 lines (member chips, member modals)

**Dark Mode Coverage (All 5 Routes):**
- Home (/) - Gradient bg + bright text
- Board List (/boards) - Cards, filters, search
- Create Board (/boards/new) - Form inputs, checkboxes
- Board View (/boards/:id) - Main board + 6 subcomponents
- Welcome (/name) - Modal + input

**Dark Theme Implementation:**
- All 80+ UI elements use `:host-context(.dark-theme)` selectors (70+ instances) for app-controlled theming (not OS-detected)
- Text color: #e8f0ff (light blue) for improved contrast vs #374151 default
- Input styling: box-sizing border-box to prevent overflow

### Development Guidelines

1. **Adding Features:** Create or extend components; don't add to board.ts
2. **Styling:** Keep CSS scoped to component; use :host-context(.dark-theme) for dark mode
3. **State:** Use signals for reactive state; pass immutable data via @Input
4. **Modals:** Place in appropriate subcomponent or board-modals; add dark-mode styles
5. **Performance:** Each component is standalone (no dependency chains); lazy loading ready
6. **Services:** Keep focused on single domain; use barrel exports for clean imports
7. **Types:** Use shared DTOs from models/ folder; avoid duplication

---

## 🎨 Frontend UI Component Architecture (Phase 3A - Feb 20, 2026)

### Board Component Refactoring

The board component has been modernized using Angular 15+ standalone component architecture. The monolithic board component is now decomposed into 6 focused subcomponents, each with scoped styling and clear responsibilities.

### Component Hierarchy

```
Board (Main Container)
├── BoardHeader (Toggle & Dev/Test Mode)
├── TeamBar (Team Members Management)
├── CapacityRow (Sprint Capacity Display & Edit)
├── SprintHeader (Column Headers & Metrics)
├── FeatureRow × N (Feature Cards with Stories)
└── BoardModals (Import, Finalize, Delete Dialogs)
```

### Subcomponent Details

| Component | Purpose | Key Features | Files |
|-----------|---------|--------------|-------|
| **BoardHeader** | Top bar with mode toggles | Dev/Test toggle, finalization banner | board-header.ts/html/css |
| **TeamBar** | Team member management | Add/edit/delete members, modal dialogs | team-bar.ts/html/css |
| **CapacityRow** | Team capacity visualization | Edit modal with 60/20/20 layout, dark mode | capacity-row.ts/html/css |
| **SprintHeader** | Column headers | Sprint names, load/capacity bars, metrics | sprint-header.ts/html/css |
| **FeatureRow** | Feature & story container | Drag-drop zones, story cards, dev/test split | feature-row.ts/html/css |
| **BoardModals** | Dialog overlays | Feature import, finalization warnings, delete | board-modals.ts/html/css |

### State Management

- **Signal-based:** Board owns `showDevTest` signal, passes as @Input to children
- **Reactive:** Changes propagate immediately through component tree
- **Per-component:** Each component manages its own local state (modals, edits)

### CSS Architecture

**CSS Distribution (Total: 2046 lines):**
- board.css: 214 lines (global layout, PAT modal, responsive)
- board-header.css: 106 lines (toggle styles, banner)
- board-modals.css: 470 lines (modal dialogs, form styling)
- capacity-row.css: 352 lines (capacity display, edit modal)
- feature-row.css: 311 lines (drag-drop styles, story cards)
- sprint-header.css: 193 lines (header, metrics display)
- team-bar.css: 400 lines (member chips, member modals)

**Dark Mode:** All 80+ UI elements use `:host-context(.dark-theme)` selectors (70+ instances) for app-controlled theming (not OS-detected).

### Development Guidelines

1. **Adding Features:** Create or extend components; don't add to board.ts
2. **Styling:** Keep CSS scoped to component; use :host-context(.dark-theme) for dark mode
3. **State:** Use signals for reactive state; pass immutable data via @Input
4. **Modals:** Place in appropriate subcomponent or board-modals; add dark-mode styles
5. **Performance:** Each component is standalone (no dependency chains); lazy loading ready

---

## 🚀 Local Development Quick Start

```bash
# Clone & navigate
git clone <repo>
cd pi-planning-tool

# Option A: Docker Compose (Everything)
docker-compose up

# Option B: Manual (Better for debugging)

# Terminal 1: Database
docker-compose up db

# Terminal 2: Backend
cd backend/pi-planning-backend
dotnet restore
dotnet watch run

# Terminal 3: Frontend
cd frontend/pi-planning-ui
npm install
ng serve

# Browser
http://localhost:4200   # Frontend
http://localhost:5000/swagger  # API docs
```

---

## ⚠️ Common Gotchas

1. **N+1 Queries:** Always use `.Include()` for related data
2. **Circular References:** Use DTOs to break serialization cycles
3. **Sprint 0 Edge Cases:** Always check if SprintId = 0 in filtering
4. **Password Hashing:** NEVER compare plaintext; always use VerifyPassword()
5. **Async All The Way:** Use `async/await`; don't block with `.Result`
6. **Validation:** Validate in service, not just controller
7. **IsMoved Logic:** Set only after board is locked (or when originalSprintId != sprintId)

---

## 📞 Testing Checklist

Before marking a feature "done":

- [ ] API returns correct schema (check Swagger)
- [ ] API handles edge cases (null IDs, negative numbers, nonexistent records)
- [ ] Error responses are meaningful (not stack trace)
- [ ] Database updated correctly (query DB directly)
- [ ] Related entities fetched with eager loading
- [ ] DTOs serialize/deserialize correctly
- [ ] Pagination/filtering works (if applicable)
- [ ] Authentication/authorization works (if applicable)
- [ ] Performance acceptable for ~100 stories

---

**Keep this as your reference. Update as you build!**

