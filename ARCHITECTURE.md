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

## 🔄 Data Flow Examples

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
GET /api/feature/{org}/{project}/{featureId} (from Azure)
  Query: ?pat={personalAccessToken}
  Response: FeatureDto { azureId, title, priority, children: [UserStoryDto, ...] }

POST /api/v1/boards/{boardId}/features/import
  Request: FeatureDto { ... }
  Response: 201 Created FeatureDto { id, title, children: [...] }

PATCH /api/v1/boards/{boardId}/features/{id}/refresh
  Query: ?org=&project=&pat=
  Response: FeatureDto { ... }

PATCH /api/v1/boards/{boardId}/features/{id}/reorder
  Request: ReorderFeatureDto { newPriority: int }
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

