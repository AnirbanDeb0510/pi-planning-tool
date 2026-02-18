# PI Planning Tool - Current Roadmap & Priorities

**Last Updated:** February 18, 2026  
**Status:** Phase 2 Near Complete - Board Management UI/UX  
**Branch:** `board-finalization-feature`

---

## 🎯 CURRENT PHASE: Board Management & Finalization Workflow

### ✅ COMPLETED (Phase 2: Board Management API & Finalization)

#### Board State & Finalization Endpoints
- ✅ **Board State Endpoint** - `GET /api/boards/{id}` - Full hierarchy with sprints, features, stories, team
- ✅ **Board Finalization API** - `PATCH /api/boards/{id}/finalize` - Marks board as finalized
- ✅ **Board Restore API** - `PATCH /api/boards/{id}/restore` - Unfinalizes board
- ✅ **Finalization Validation** - `GET /api/boards/{id}/finalization-warnings` - Shows pre-finalization warnings
- ✅ **Original Sprint Tracking** - Stores `OriginalSprintId` at finalization for post-plan analysis

#### Board Finalization UI/UX
- ✅ **Finalized Board Banner** - Visual indicator that board is finalized + restore button
- ✅ **Operation Guards** - Add member/feature/delete/edit capacity blocked on finalized boards
- ✅ **Visual Feedback** - Disabled buttons show 50% opacity with `cursor: not-allowed`
- ✅ **Hover Tooltips** - Shows why operation is blocked (e.g., "Cannot add members on finalized board")
- ✅ **Dark Mode Support** - All UI indicators properly visible in dark theme

#### Board Analysis Features (Post-Finalization)
- ✅ **Feature Reordering** - Users can reorder features on finalized boards for impact analysis
- ✅ **Story Movement** - Users can move stories between sprints for impact analysis
- ✅ **Story Indicators** - Icon badges (🆕/📍) show new and moved stories with sprint names
- ✅ **Feature Refresh** - `PATCH /api/boards/{id}/features/{id}/refresh` updates feature data from Azure
- ✅ **Story Impact Tracking** - Shows which stories were moved and which sprints they came from

#### Code Quality
- ✅ **No Duplicate Methods** - Used optional `checkFinalized = true` parameter for refresh bypass
- ✅ **Interface Updates** - IFeatureService updated with optional parameter
- ✅ **CSS Optimization** - Removed opacity overlays, added proper disabled state styling
- ✅ **Dark Mode Contrast** - Improved indicator colors for dark theme (#ffd54f, #64b5f6)
- ✅ **Build Status** - Backend: 0 errors | Frontend: 0 compilation errors

---

## ✅ COMPLETED (Phase 1+: Backend Stabilization)

#### Phase 1: Board Search & Security Hardening
- ✅ **Board Search API** - `GET /api/boards` with filters
- ✅ **Board Preview Endpoint** - `GET /api/boards/{id}/preview`
- ✅ **PAT Validation Security Flow** - Modal validation
- ✅ **Frontend Board List UI** - Search, filtering, board cards

#### Phase 1: Global Exception Handling & Input Validation
- ✅ **GlobalExceptionHandlingMiddleware** - Centralized exception handling
- ✅ **ValidateModelStateFilter** - Global ModelState validation
- ✅ **DTO Validation Attributes** - Enhanced request DTOs
- ✅ **Controller Cleanup** - Removed manual ModelState checks
- ✅ **Standardized Error Responses** - Consistent JSON format
---

## 🚀 NEXT PRIORITIES (Ordered by Dependency & Impact)

### PHASE 2: Board Lock Endpoints (Pending - External Owner)
**Status:** Not Started - To be handled by another team member

#### **Board Lock/Unlock Endpoints** (High)
**Why:** Enables board state control; foundation for feature lock

**Endpoints:**
- `PATCH /api/boards/{id}/lock` - Lock board (prevent further changes)
- `PATCH /api/boards/{id}/unlock` - Unlock board

**Logic:**
- Set `Board.IsLocked` flag
- Validate user permissions (optional)
- Return updated board state

**Files to Modify:**
- `Controllers/BoardsController.cs` - Add endpoints

**Notes:**
- Finalization is complete and working
- Lock is a separate feature for different workflow requirements
- Can be implemented in parallel or as separate PR

---

### PHASE 3: Frontend UI & UX Enhancements & Collaboration (Medium-High Priority - Future)

#### **Story Refresh Confirmation** (Optional Enhancement - Backlog)
**Description:** When refreshing feature from Azure, if new user stories are found, prompt user to confirm before adding

**Implementation:**
- Backend: Return list of new stories discovered during refresh
- Frontend: Show confirmation modal with new stories
- User can accept or decline adding new stories

**Status:** Parked for future enhancement (not blocking Phase 2)

---

#### **UI Component Modularization** (Medium) — Est. 6-8 hours
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

#### **Real-time Collaboration (SignalR)** (Medium-High) — Est. 4-6 hours
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
Completed (Phase 1):
  ✅ Global Exception Middleware
  ✅ Input Validation & Error Handling
       ↓
Completed (Phase 2):
  ✅ Board State Endpoints
  ✅ Board Finalization & Analysis
  ⏳ Board Lock/Unlock (External Owner)
       ↓
Future (Phase 3):
  → UI Component Modularization
  → Real-time Collaboration (SignalR)
  → Story Refresh Confirmation Modal
```

---

## 📈 Success Metrics

- [x] ✅ Phase 1 complete: Global exception handling & input validation
- [x] ✅ Phase 2 complete: Board finalization with analysis features
- [x] ✅ API returns consistent error responses
- [x] ✅ Frontend implements all finalization UI/UX
- [x] ✅ No technical debt added
- [x] ✅ Backend: 0 errors | Frontend: 0 compilation errors
- ⏳ Phase 2.5: Board lock/unlock (External owner)
- [ ] Phase 3: Component modularization & SignalR
- [ ] Code coverage > 80% for critical paths

---

## 📝 Completed Branches & PRs

- ✅ `board-finalization-feature` - Board finalization, analysis features, UI/UX (Ready for PR)
- ✅ `boardSearchFiltering` - Board search with mandatory org/project (Completed previously)

---

## 📋 Files Modified in This Sprint

### Backend
- `Services/Implementations/BoardService.cs` - Added finalize/restore methods + warning validation
- `Services/Implementations/FeatureService.cs` - Added optional `checkFinalized` parameter for refresh
- `Services/Interfaces/IFeatureService.cs` - Updated interface with optional parameter
- `Controllers/BoardsController.cs` - Added finalization endpoints
- `Models/UserStory.cs` - Added `OriginalSprintId` tracking
- `Migrations/` - Migration for `IsFinalized` and `OriginalSprintId` fields

### Frontend
- `Components/board/board.ts` - Added finalization methods, operation guards, sprint lookup helper
- `Components/board/board.html` - Added finalization UI, operation guards on all interactive elements
- `Components/board/board.css` - Added disabled state styling with proper contrast for dark mode
- `Components/story-card/story-card.ts` - Added story indicators (moved/new) with sprint name lookup
- `Components/story-card/story-card.html` - Added indicator badge with hover label reveal
- `Components/story-card/story-card.css` - Added indicator styling + dark mode contrast improvements
- `shared/models/board.dto.ts` - Updated DTOs with finalization fields
- `angular.json` - Updated CSS budget from 16kB to 18kB

---

## 🔗 Related Documents

- **README.md** - Project overview
- **GUIDE.md** - Executive summary
- **ARCHITECTURE.md** - Technical architecture
- **CHANGELOG.md** - Version history
- **CONFIGURATION.md** - Setup & configuration

## Open Question
Should you implement story dependencies/blockers?
