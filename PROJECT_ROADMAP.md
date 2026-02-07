# PI Planning Tool - Project Roadmap & Implementation Guide

**Last Updated:** February 6, 2026  
**Status:** Active Development - Feature Implementation Phase

---

## 📊 CURRENT STATE ASSESSMENT

### ✅ COMPLETED & STABLE

#### Backend Infrastructure
- ✅ Domain model fully defined (Board, Sprint, Feature, UserStory, TeamMember, TeamMemberSprint, CursorPresence)
- ✅ EF Core configuration resolved (FK relationships, no ambiguity)
- ✅ Database migrations stable (InitialCreate applied successfully)
- ✅ PostgreSQL containerized with Docker
- ✅ Layered architecture implemented (Controllers → Services → Repositories → DbContext)
- ✅ Dependency Injection configured in Program.cs
- ✅ CORS enabled for dev convenience

#### Core APIs (Partially Implemented)
- ✅ `POST /api/boards` - Create board with auto-generated sprints
- ✅ `POST /api/v1/boards/{boardId}/features/import` - Import feature + child stories
- ✅ `PATCH /api/v1/boards/{boardId}/features/{id}/reorder` - Move feature (priority)
- ✅ `PATCH /api/boards/{boardId}/stories/{storyId}/move` - Move story (sprint assignment)
- ✅ `PATCH /api/v1/boards/{boardId}/features/{id}/refresh` - Refresh feature from Azure
- ✅ `PATCH /api/boards/{boardId}/stories/{storyId}/refresh` - Refresh story from Azure
- ✅ `POST /api/boards/{boardId}/team` - Add/update team members
- ✅ `GET /api/boards/{boardId}/team` - Get team
- ✅ `PATCH /api/boards/{boardId}/team/sprints/{sprintId}/team/{teamMemberId}` - Update capacity

#### Business Logic (Core Flows)
- ✅ Import flow with placeholder handling
- ✅ De-duplication using AzureId
- ✅ Sprint assignment to placeholder (Sprint 0) on import
- ✅ Move tracking (IsMoved flag via OriginalSprintId ↔ SprintId)
- ✅ Feature priority reordering
- ✅ Azure Boards integration (fetch feature + children)
- ✅ Team member capacity per sprint

#### Frontend Foundation
- ✅ Angular 20 setup with CDK drag-drop
- ✅ Angular Material configured
- ✅ Basic models (Feature, Story, Sprint interfaces)
- ✅ User service skeleton
- ✅ Board component structure created

---

### ⚠️ IN PROGRESS / INCOMPLETE

#### Backend APIs (Placeholder Implementations)
- ⚠️ `GET /api/boards/{id}` - Returns placeholder string, needs full board fetch with relationships
- ⚠️ No SignalR hub wired to patterns yet
- ⚠️ Board locking/finalization logic not exposed via API

#### Backend Enhancements Needed
- 🔧 Global exception handling middleware (TODO in Program.cs)
- 🔧 Request/Response logging
- 🔧 Input validation & error codes
- 🔧 Board state endpoints (GET board with full hierarchy)
- 🔧 Board lock/unlock endpoints
- 🔧 Board finalization endpoints
- 🔧 Capacity calculation endpoints (load vs. capacity per sprint/team member)

#### Frontend UI/UX (Early Stages)
- 🏗️ No styling applied (CSS present but minimal)
- 🏗️ No Material Design component integration
- 🏗️ Board view not rendering
- 🏗️ Drag-drop logic not connected to backend
- 🏗️ Azure integration UI (fetch/import flow) not built
- 🏗️ Real-time collaboration UI (cursor presence, live updates) not implemented
- 🏗️ Capacity visualization not implemented
- 🏗️ Notes feature UI not implemented

#### SignalR Integration
- 📡 Hub skeleton exists (`PlanningHub.cs`)
- 📡 No message handlers implemented
- 📡 No connection lifecycle management
- 📡 No cursor presence broadcast

---

## 🎯 RECOMMENDED PRIORITY ORDER

### PHASE 1: BACKEND API COMPLETION (2-3 weeks)
**Goal:** Make all critical APIs production-ready so frontend can consume them.

#### Week 1: Core CRUD & State Endpoints
1. **[HIGH]** Implement `GET /api/boards/{id}` - Return full board hierarchy
   - Fetch board with sprints, features, stories, team members
   - Optimize with eager loading to avoid N+1 queries
   
2. **[HIGH]** Implement `GET /api/v1/boards/{boardId}/features` - List all features
   - Support pagination/filtering
   - Return normalized FeatureDtos with children
   
3. **[HIGH]** Implement `GET /api/boards/{boardId}/sprints` - List sprints
   - Include capacity info per team member
   
4. **[HIGH]** Implement `GET /api/boards/{boardId}/stories` - List all stories
   - Optional filtering by feature/sprint/status

5. **[MEDIUM]** Add board locking endpoints
   - `PATCH /api/boards/{id}/lock` - Lock board for edits
   - `PATCH /api/boards/{id}/unlock` - Unlock board (admin only)

#### Week 2: Validation & Error Handling
1. **[HIGH]** Add global exception handling middleware
   - Standardized error response format
   - Log exceptions to console/file
   
2. **[HIGH]** Add input validation
   - Fluent validation or data annotations
   - Meaningful error messages
   
3. **[MEDIUM]** Add capacity calculations
   - `GET /api/boards/{boardId}/capacity/{sprintId}` - Load vs. capacity per sprint
   - Helper methods in service layer

4. **[MEDIUM]** Add logging
   - Request/response logging for key endpoints
   - Use ILogger (already in DI)

#### Week 3: SignalR & Advanced Features
1. **[HIGH]** Implement SignalR message handlers
   - `FeatureMoved` - broadcast when feature reordered
   - `StoryMoved` - broadcast when story moves
   - `CursorUpdated` - ephemeral cursor presence
   
2. **[MEDIUM]** Implement cursor tracking
   - `POST /api/boards/{boardId}/cursors` - Update cursor position
   - SignalR broadcasts to other clients
   
3. **[MEDIUM]** API documentation
   - Swagger/OpenAPI enhancement
   - Comment all endpoints

---

### PHASE 2: FRONTEND CORE BOARD (2-3 weeks)
**Goal:** Build a functional, styled board where users can see sprints, features, and stories.

#### Week 1: Layout & Styling
1. **[HIGH]** Implement board layout
   - Header with board name, sprint info
   - Feature rows (left sidebar)
   - Sprint columns (top header)
   - Story cards in grid
   
2. **[HIGH]** Apply Material Design
   - Cards for stories
   - Tables/grids for layout
   - Color scheme & typography
   
3. **[MEDIUM]** Implement responsive design
   - Mobile-friendly layout
   - Sticky headers (features, sprints)

#### Week 2: Data Binding & API Integration
1. **[HIGH]** Connect board component to backend
   - Fetch board with http client
   - Populate sprints, features, stories
   - Handle loading/error states
   
2. **[HIGH]** Implement drag-drop
   - CDK drag-drop for stories (horizontal - sprint change)
   - CDK drag-drop for features (vertical - priority reorder)
   - Call move APIs on drop

3. **[MEDIUM]** Add story cards
   - Display title, story points, assignees
   - Show IsMoved flag with visual indicator
   - Add notes tooltip

#### Week 3: Team & Planning Features
1. **[HIGH]** Implement team member panel
   - Add/remove team members
   - Configure capacity per sprint
   - Show capacity vs. load visualization
   
2. **[MEDIUM]** Implement notes feature
   - Add notes modal
   - Notes per feature/story

3. **[LOW]** Implement finalization toggle
   - Visual mode for moved stories

---

### PHASE 3: AZURE INTEGRATION & REAL-TIME (2 weeks)
**Goal:** Enable fetching from Azure Boards, and real-time multi-user collaboration.

#### Week 1: Azure Fetch UI
1. **[HIGH]** Build Azure fetch modal
   - Org, project, feature ID inputs
   - PAT input (optional: browser storage)
   - Fetch button
   - Show fetched feature preview
   
2. **[HIGH]** Implement import flow
   - Click "Add to Board"
   - Feature + stories imported to placeholder
   - Refresh user story list

#### Week 2: Real-time Collaboration
1. **[HIGH]** Wire SignalR to frontend
   - Connect to hub on board load
   - Listen to FeatureMoved, StoryMoved events
   - Broadcast local moves to hub
   
2. **[MEDIUM]** Implement cursor presence
   - Show cursor positions of other users
   - Color-coded per user
   - Auto-hide after timeout

3. **[LOW]** Toast notifications for remote updates
   - "Feature X was moved by User Y"

---

### PHASE 4: POLISH & DEPLOYMENT (1 week)
**Goal:** Make production-ready with tests, docs, cloud deployment.

1. **[HIGH]** Unit tests
   - Services & repositories (backend)
   - Components & services (frontend)
   
2. **[HIGH]** E2E tests
   - Cypress/Playwright board flow
   
3. **[MEDIUM]** Documentation
   - API docs (Swagger)
   - Frontend component docs
   
4. **[MEDIUM]** Performance optimization
   - Virtual scrolling for large boards
   - Lazy loading features
   
5. **[LOW]** Cloud deployment
   - Google Cloud Run setup
   - Environment config for prod

---

## 🔧 IMPLEMENTATION DETAILS BY AREA

### Backend: Complete Board Fetch Endpoint
```csharp
// FIRST: Extend IBoardService interface
Task<BoardResponseDto> GetBoardWithHierarchyAsync(int boardId);

// BoardService implementation
public async Task<BoardResponseDto> GetBoardWithHierarchyAsync(int boardId)
{
    var board = await _boardRepository.GetBoardWithSprintsAsync(boardId);
    if (board == null) throw new NotFoundException("Board not found");
    
    var features = await _featureRepository.GetFeaturesWithStoriesByBoardAsync(boardId);
    var teamMembers = await _teamRepository.GetTeamWithCapacityAsync(boardId);
    
    return new BoardResponseDto
    {
        Id = board.Id,
        Name = board.Name,
        Sprints = board.Sprints.Select(s => new SprintDto { ... }).ToList(),
        Features = features.Select(f => new FeatureDto { ... }).ToList(),
        TeamMembers = teamMembers.Select(t => new TeamMemberDto { ... }).ToList(),
        IsLocked = board.IsLocked,
        IsFinalized = board.IsFinalized,
        DevTestToggle = board.DevTestToggle
    };
}

// BoardRepository: Add efficient query with eager loading
public async Task<Board?> GetBoardWithSprintsAsync(int boardId)
{
    return await _context.Boards
        .Include(b => b.Sprints)
        .Include(b => b.Features)
            .ThenInclude(f => f.UserStories)
        .FirstOrDefaultAsync(b => b.Id == boardId);
}
```

### Backend: Add Board Lock Endpoint
```csharp
// In BoardsController
[HttpPatch("{id}/lock")]
public async Task<IActionResult> LockBoard(int id, [FromBody] string? password)
{
    var result = await _boardService.LockBoardAsync(id, password);
    if (!result) return Unauthorized("Incorrect password");
    return NoContent();
}

[HttpPatch("{id}/unlock")]
public async Task<IActionResult> UnlockBoard(int id, [FromBody] string? password)
{
    var result = await _boardService.UnlockBoardAsync(id, password);
    if (!result) return Unauthorized("Incorrect password");
    return NoContent();
}

// Service layer
public async Task<bool> LockBoardAsync(int boardId, string? password)
{
    var board = await _boardRepository.GetByIdAsync(boardId);
    if (board == null) return false;
    
    if (board.PasswordHash != null && !PasswordHelper.VerifyPassword(password, board.PasswordHash))
        return false;
    
    board.IsLocked = true;
    await _boardRepository.UpdateAsync(board);
    await _boardRepository.SaveChangesAsync();
    return true;
}
```

### Frontend: Basic Board Component Structure
```typescript
// board.ts
import { Component, OnInit } from '@angular/core';
import { BoardService } from '../../services/board.service';
import { Board, Sprint, Feature, UserStory } from '../../models';

@Component({
  selector: 'app-board',
  templateUrl: './board.html',
  styleUrls: ['./board.css']
})
export class BoardComponent implements OnInit {
  board: Board | null = null;
  loading = true;
  error: string | null = null;
  
  constructor(private boardService: BoardService) {}
  
  ngOnInit() {
    const boardId = 1; // from route param
    this.boardService.getBoard(boardId).subscribe({
      next: (board) => {
        this.board = board;
        this.loading = false;
      },
      error: (err) => {
        this.error = err.message;
        this.loading = false;
      }
    });
  }
  
  onStoryDropped(event: any) {
    const storyId = event.item.data.id;
    const targetSprintId = event.container.data.sprintId;
    
    this.boardService.moveStory(this.board!.id, storyId, targetSprintId)
      .subscribe({
        next: () => {
          // Update local state
        },
        error: (err) => console.error('Move failed', err)
      });
  }
  
  onFeatureDropped(event: any) {
    const featureId = event.item.data.id;
    const newPriority = event.currentIndex;
    
    this.boardService.reorderFeature(this.board!.id, featureId, newPriority)
      .subscribe({
        error: (err) => console.error('Reorder failed', err)
      });
  }
}
```

### Frontend: Material Design Board Template
```html
<!-- board.html -->
<div class="board-container" *ngIf="!loading">
  <header class="board-header">
    <h1>{{ board?.name }}</h1>
    <div class="board-actions">
      <button mat-button (click)="onFetchFromAzure()">Fetch from Azure</button>
      <button mat-button (click)="onAddTeamMember()">Add Team Member</button>
      <button mat-button [disabled]="board?.isLocked" (click)="onLockBoard()">
        {{ board?.isLocked ? 'Board Locked' : 'Lock Board' }}
      </button>
    </div>
  </header>
  
  <div class="board-view">
    <!-- Feature rows on left, Sprint columns across top -->
    <div class="features-column">
      <div *ngFor="let feature of board?.features" class="feature-row">
        <strong>{{ feature.title }}</strong>
      </div>
    </div>
    
    <div class="sprints-grid">
      <div class="sprints-header">
        <div *ngFor="let sprint of board?.sprints" class="sprint-header">
          {{ sprint.name }}
        </div>
      </div>
      
      <div class="stories-matrix">
        <div *ngFor="let feature of board?.features" class="feature-stories">
          <div *ngFor="let sprint of board?.sprints" 
               cdkDropZone
               [data]="{ sprintId: sprint.id }"
               (cdkDropListDropped)="onStoryDropped($event)"
               class="sprint-column">
            <div *ngFor="let story of getStoriesForFeatureAndSprint(feature.id, sprint.id)"
                 cdkDrag
                 [data]="{ id: story.id }"
                 class="story-card"
                 [class.moved]="story.isMoved">
              <div class="story-header">{{ story.title }}</div>
              <div class="story-points">{{ story.storyPoints }} pts</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</div>

<div *ngIf="loading" class="spinner">Loading board...</div>
<div *ngIf="error" class="error">{{ error }}</div>
```

---

## 📋 TASK CHECKLIST FOR NEXT STEPS

### Immediate (Next 2-3 days)
- [ ] Create `BoardResponseDto` class for full board fetch
- [ ] Implement `GetBoardWithHierarchyAsync` in service + repository
- [ ] Test `GET /api/boards/{id}` with Postman
- [ ] Create `GET /api/v1/boards/{boardId}/features` endpoint
- [ ] Add global exception handling middleware

### Week 1
- [ ] Implement board lock/unlock endpoints
- [ ] Add input validation to all endpoints
- [ ] Create comprehensive Swagger/OpenAPI docs
- [ ] Set up frontend board component with Material Design
- [ ] Implement drag-drop for stories (horizontal)

### Week 2
- [ ] Wire frontend to backend APIs
- [ ] Implement team member UI
- [ ] Add capacity calculations
- [ ] Implement feature reordering drag-drop
- [ ] Add loading/error handling to frontend

### Week 3+
- [ ] Build Azure fetch modal
- [ ] Implement SignalR messaging
- [ ] Add cursor presence UI
- [ ] Write unit tests
- [ ] Performance optimization & polish

---

## 🚀 QUICK START COMMANDS

```bash
# Backend: Run with hot reload
cd backend/pi-planning-backend
dotnet watch run

# Frontend: Run with hot reload
cd frontend/pi-planning-ui
ng serve

# Full Docker stack
docker-compose up

# Test API with curl
curl -X GET http://localhost:5000/api/swagger
```

---

## 🎓 KEY ARCHITECTURAL PATTERNS TO REMEMBER

1. **Services are authoritative** - All business logic lives here, not in controllers
2. **DTOs for data transfer** - Reuse same DTOs for requests/responses
3. **Repositories are thin** - No business logic, just EF Core queries
4. **Placeholder pattern** - Imported items start in Sprint 0, user moves to real sprints
5. **Move tracking** - OriginalSprintId vs SprintId enables highlighting moved stories
6. **Eager loading** - Use `.Include()` to avoid N+1 queries on board fetch
7. **SignalR as mirror** - Real-time is informational, not authoritative (backend is source of truth)

---

## 🔗 KEY FILES TO KNOW

**Backend:**
- [Program.cs](./backend/pi-planning-backend/Program.cs) - DI configuration
- [AppDbContext.cs](./backend/pi-planning-backend/Data/AppDBContext.cs) - EF Core mapping
- [FeatureService.cs](./backend/pi-planning-backend/Services/Implementations/FeatureService.cs) - Core business logic
- [FeaturesController.cs](./backend/pi-planning-backend/Controllers/FeaturesController.cs) - API endpoints

**Frontend:**
- [board.ts](./frontend/pi-planning-ui/src/app/components/board/board.ts) - Main board component
- [app.routes.ts](./frontend/pi-planning-ui/src/app/app.routes.ts) - Routing config
- [Models/](./frontend/pi-planning-ui/src/app/Models/) - TypeScript interfaces

---

## 💡 NOTES & GOTCHAS

- **Sprint 0 is Placeholder** - Created automatically, starts before all numbered sprints
- **DevTestToggle** - When true, use DevStoryPoints; when false, use StoryPoints
- **IsMoved flag** - Only meaningful after board is locked
- **Eager loading critical** - Board fetch should include everything in one query
- **PasswordHash** - Use PasswordHelper utility for hashing (check if it exists; create if not)
- **CursorPresence is ignored** - It's not in migrations, it's ephemeral (SignalR only)

---

## ❓ QUESTIONS TO ANSWER AS YOU BUILD

1. Should team members have roles (Scrum Master, Product Owner, etc.)?
2. Should you implement story dependencies/blockers?
3. Should you track historical versions of board state?
4. Should notes be collaborative (real-time editing)?
5. Do you want capacity override per story (not just per sprint)?

---

## 🎉 NEXT IMMEDIATE ACTION

**Start with Phase 1, Week 1, Step 1:**

1. Create `BoardResponseDto` class to model the full board response
2. Update `IBoardService` interface to add `GetBoardWithHierarchyAsync`
3. Implement in `BoardService`
4. Update repository to use eager loading
5. Test with Swagger/Postman

This unblocks the frontend team and gives you a solid foundation for all other reads.

