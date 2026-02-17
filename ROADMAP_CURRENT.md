# PI Planning Tool - Current Roadmap & Priorities

**Last Updated:** February 17, 2026  
**Status:** Active Development - Security & Validation Phase  
**Branch:** `boardSearchFiltering`

---

## 🎯 CURRENT PHASE: Security & Input Validation

### ✅ COMPLETED (Week of Feb 10-17)

#### Board Search & Security Hardening
- ✅ **Board Search API** - `GET /api/boards` with filters (search, organization, project, isLocked, isFinalized)
- ✅ **Board Preview Endpoint** - `GET /api/boards/{id}/preview` (secure, lightweight data)
- ✅ **PAT Validation Security Flow** - Modal validation before accessing board features
- ✅ **Organization/Project Mandatory (Frontend)** - Text input fields instead of dropdowns
- ✅ **Organization/Project Mandatory (API)** - Server-side validation with `[BindRequired]`
- ✅ **Frontend Board List UI** - Search, filtering, board cards with dynamic navigation

---

## 🚀 NEXT PRIORITIES (Ordered by Dependency & Impact)

### PHASE 1: Backend API & Validation (Current Sprint)

#### 1. **Global Exception Middleware** (Blocking) — Est. 2-3 hours
**Why:** Standardizes error responses across all endpoints; prevents information leakage

- Create `GlobalExceptionHandlingMiddleware.cs`
- Implement centralized exception handling
- Log all exceptions
- Return standardized error responses (500, 400, 404, etc.)
- Handle validation exceptions from `[BindRequired]`

**Files to Create:**
- `Middleware/GlobalExceptionHandlingMiddleware.cs`

**Files to Modify:**
- `Program.cs` - Register middleware in pipeline

**Acceptance Criteria:**
- ✅ All exceptions return standard JSON format
- ✅ Sensitive error details not exposed to clients
- ✅ Build: 0 errors

---

#### 2. **Input Validation & Error Handling** (High) — Est. 3-4 hours
**Why:** Prevents invalid data from reaching the database; improves API reliability

**Scope:**
- DTOs: Add data annotations for all inputs
  - Strings: [Required], [StringLength]
  - Numbers: [Range]
  - Collections: [MinLength], [MaxLength]
  
- Service Layer: Validate business rules
  - Team member capacity bounds
  - Sprint date validations
  - Board name uniqueness (if required)
  
- Controllers: Return validation errors cleanly (already partial via `[BindRequired]`)

**Key DTOs to Enhance:**
- BoardCreateDto
- TeamMemberDto
- UpdateFeaturePriorityDto
- MoveStoryDto
- ImportFeaturesDto

**Files to Modify:**
- `DTOs/*.cs` - Add data annotations
- `Services/Implementations/*.cs` - Add business rule validation

**Acceptance Criteria:**
- ✅ All required fields validated before business logic
- ✅ Error responses include field-specific messages
- ✅ Build: 0 errors

---

#### 3. **Board State Endpoints** (Medium) — Est. 3-4 hours
**Why:** Enables frontend to fetch full board with hierarchy; critical for board view

**Endpoints Needed:**
- `GET /api/boards/{id}` - Already partially implemented, verify it returns full hierarchy
  - Board + Sprints + Features + Stories + TeamMembers
- `GET /api/boards/{id}/sprints` - List sprints with capacity info (optional)
- `GET /api/boards/{id}/team` - Get team members (already implemented)

**Optimization:**
- Use eager loading (.Include) to avoid N+1 queries
- Consider pagination for large boards

**Acceptance Criteria:**
- ✅ Full board fetch returns all related data
- ✅ No N+1 queries
- ✅ Build: 0 errors

---

### PHASE 2: Board State Management (Next Sprint)

#### 4. **Board Lock/Unlock Endpoints** (High) — Est. 2-3 hours
**Why:** Enables board state control; foundation for finalization workflow

**Endpoints:**
- `PATCH /api/boards/{id}/lock` - Lock board (prevent further changes)
- `PATCH /api/boards/{id}/unlock` - Unlock board

**Logic:**
- Set `Board.IsLocked` flag
- Validate user permissions (optional)
- Return updated board state

**Files to Create/Modify:**
- `Controllers/BoardsController.cs` - Add endpoints

**Acceptance Criteria:**
- ✅ Lock/unlock endpoints work
- ✅ Locked status prevents modifications (validator + service layer)
- ✅ Build: 0 errors

---

#### 5. **Board Finalization Mode** (High) — Est. 2-3 hours
**Why:** Enables board completion workflow; prevents accidental changes

**Endpoints:**
- `PATCH /api/boards/{id}/finalize` - Finalize board (marks as complete)
- `PATCH /api/boards/{id}/unfinalize` - Restore finalization (if needed)

**Logic:**
- Set `Board.IsFinalized` flag
- Validate board state (all sprints assigned, etc.)
- Return updated board state

**Files to Modify:**
- `Controllers/BoardsController.cs` - Add endpoints

**Acceptance Criteria:**
- ✅ Finalization endpoints work
- ✅ Cannot finalize incomplete boards (validation)
- ✅ Build: 0 errors

---

### PHASE 3: Frontend UI & UX (After Backend Stabilization)

#### 6. **UI Component Modularization** (Medium) — Est. 6-8 hours
**Why:** Reduces complexity; improves maintainability; reduces `board.ts` from 800+ to 300 LOC

**New Components:**
- `TeamMemberBar` - Team member display + add-member
- `CapacityRow` - Sprint capacity visualization
- `SprintColumn` - Sprint header + cards
- `FeatureRow` - Feature with child stories
- `UserStoryCard` - Story card
- `BoardHeader` - Title, search, filters

**Files to Create:**
- `Components/board/team-member-bar/team-member-bar.component.ts|html|css`
- `Components/board/capacity-row/capacity-row.component.ts|html|css`
- `Components/board/sprint-column/sprint-column.component.ts|html|css`
- `Components/board/feature-row/feature-row.component.ts|html|css`

**Files to Modify:**
- `Components/board/board.component.ts` - Refactor to use child components
- `Components/board/board.html` - Use new component tags

**Acceptance Criteria:**
- ✅ Board component < 300 LOC
- ✅ All functionality preserved
- ✅ Build: 0 errors

---

#### 7. **Real-time Collaboration (SignalR)** (Medium-High) — Est. 4-6 hours
**Why:** Enables multi-user concurrent editing; core feature of the tool

**Features:**
- Cursor presence broadcast (show user position)
- Live move updates (when story/feature moved)
- Live team member updates
- Conflict resolution for concurrent moves

**Files to Modify:**
- `Hubs/PlanningHub.cs` - Implement message handlers
- Frontend: Create SignalR service
- Frontend: Board component - Subscribe to updates

**Acceptance Criteria:**
- ✅ Cursor presence works for multiple users
- ✅ Move updates broadcast to all clients
- ✅ Build: 0 errors

---

## 📊 Dependency Chain

```
Phase 1 (Backend):
  Global Exception Middleware
       ↓ (blocks validation layer)
  Input Validation & Error Handling
       ↓ (depends on clean API)
  Board State Endpoints

Phase 2 (Board Management):
  Board Lock/Unlock
  Board Finalization
       ↓ (need clean API before frontend can use)

Phase 3 (Frontend):
  UI Component Modularization
  Real-time Collaboration (SignalR)
```

---

## 📈 Success Metrics

- [ ] All 7 priorities completed with 0 build errors
- [ ] API returns consistent error responses
- [ ] Frontend consumes all new endpoints
- [ ] No technical debt added
- [ ] Code coverage > 80% for critical paths

---

## 📝 Completed Branches

- ✅ `boardSearchFiltering` - Board search with mandatory org/project (Ready for PR)

---

## 🔗 Related Documents

- **README.md** - Project overview
- **GUIDE.md** - Executive summary
- **ARCHITECTURE.md** - Technical architecture
- **CHANGELOG.md** - Version history
- **CONFIGURATION.md** - Setup & configuration

## Open Question
Should you implement story dependencies/blockers?
