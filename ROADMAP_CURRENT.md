# PI Planning Tool - Current Roadmap & Priorities

**Last Updated:** February 20, 2026  
**Status:** ✅ Phase 3 COMPLETE - UI Refactoring, Folder Restructuring & Dark-Mode Support  
**Branch:** `chore/uiRefactoring` (Ready for PR & merge to main)

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

## ✅ COMPLETED (Phase 3: Full UI & Architecture Refactoring)

### Phase 3 Overview
- **3A:** Component modularization + scoped CSS (6 subcomponents, dark mode selectors)
- **3B:** Folder restructuring + service split (core/shared/features, 5 focused services)
- **Full Dark Mode:** All 5 routes styled with optimized contrast (#e8f0ff text)

#### Board Component Architecture Modernization
- ✅ **6 Standalone Subcomponents** - BoardHeader, TeamBar, CapacityRow, SprintHeader, FeatureRow, BoardModals
- ✅ **Component Isolation** - Each component with standalone: true, scoped CSS, clear responsibilities
- ✅ **Reduced Complexity** - Board.ts from 593 LOC (includes state logic), board.css from 1277 to 214 lines
- ✅ **Angular 15+ Patterns** - Signals, standalone bootstrap, explicit imports, no module dependencies

#### CSS Consolidation & Optimization
- ✅ **Scoped Component Styling** - 2046 total CSS lines distributed: board (214) + board-header (106) + board-modals (470) + capacity-row (352) + feature-row (311) + sprint-header (193) + team-bar (400)
- ✅ **No Global CSS Conflicts** - Each component owns its styles with proper scoping
- ✅ **Maintained Responsiveness** - All media queries and responsive patterns preserved
- ✅ **CSS Budget Compliance** - Main board.css no longer exceeds style budget limits

#### Dark-Mode Theme System
- ✅ **App-Controlled Theming** - Migrated from OS-detected `@media (prefers-color-scheme: dark)` to app-controlled `:host-context(.dark-theme)` class selectors
- ✅ **Comprehensive Coverage** - 70+ `:host-context(.dark-theme)` selectors across all 7 component files
- ✅ **Proper Contrast** - All UI elements readable in both light and dark modes
- ✅ **Special Cases** - Over-capacity text stays red, form inputs readable, modal backgrounds appropriate

#### Dev/Test Toggle Signal Wiring
- ✅ **Signal-Based State** - Board owns `showDevTest` signal with update method
- ✅ **Child Component Integration** - Passed as @Input to BoardHeader, SprintHeader, FeatureRow, CapacityRow
- ✅ **Reactive Updates** - Changes propagate immediately through component tree
- ✅ **Conditional UI** - Role selector hidden when toggle off, Dev/Test split shows only when on

#### UI/UX Polish & Layout Improvements
- ✅ **Capacity Modal Redesign** - 60% name width, 20% gap space, 20% input fields for better proportions
- ✅ **Consistent Input Display** - Both Dev/Test inputs always visible in dev/test mode (not conditionally hidden)
- ✅ **Font Color Fixes** - Dark-mode font colors applied to capacity edit rows and role labels
- ✅ **Visual Spacing** - Increased gap between name and input fields (16px → 24px)

#### Build & Code Quality
- ✅ **Compilation Success** - Build passes with zero TypeScript errors
- ✅ **Bundle Status** - 768.05 kB initial bundle (warning only, acceptable for feature-rich SPA)
- ✅ **No Breaking Changes** - Full backward compatibility; all existing functionality preserved
- ✅ **Code Organization** - Clear component hierarchy, easy to extend and maintain

#### Phase 3B: Folder Restructuring & Service Split
- ✅ **Application Architecture** - Migrated to domain-driven structure: `core/` → `shared/` → `features/`
- ✅ **Service Consolidation** - Split 850-line monolithic service into 5 focused services
  - `board.service.ts` (200 LOC) - Board state, PAT handling
  - `feature.service.ts` (150 LOC) - Feature import/refresh/reorder logic
  - `team.service.ts` (120 LOC) - Team members and capacity management
  - `story.service.ts` (100 LOC) - User story movement between sprints
  - `sprint.service.ts` (80 LOC) - Sprint utilities and helpers
- ✅ **Barrel Exports** - 8 index.ts files for clean module boundaries and simplified imports
- ✅ **TypeScript Path Aliases** - Using `@core`, `@shared`, `@features` for consistent import structure
- ✅ **Zero Breaking Changes** - All components still functional, improved maintainability

#### Dark Mode Completeness (All 5 Routes)
- ✅ **Route: `/`** - Home component with dark gradient (#1a1a2e → #16213e) + bright text
- ✅ **Route: `/boards`** - Board list with filters, search, cards fully styled + box-sizing fix
- ✅ **Route: `/boards/new`** - Create board form with all inputs, checkboxes, buttons dark-themed
- ✅ **Route: `/boards/:id`** - Main board view with 6 subcomponents all dark-mode compliant
- ✅ **Route: `/name`** - Enter name modal with dark styling
- ✅ **Text Optimization** - Changed to #e8f0ff (light blue) for improved contrast
- ✅ **Input Box Sizing** - Fixed overflow issues with `box-sizing: border-box`

---

## ✅ COMPLETED (Previous Phases: Backend Stabilization)

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

## 🚀 NEXT PRIORITIES (Ordered by Dependency & Impact)

### PHASE 3B: Real-time Collaboration (SignalR) (High Priority - Next Up)
**Status:** Not Started

#### **Real-time Collaboration (SignalR)** (High) — Est. 4-6 hours
**Why:** Enables multi-user concurrent editing; core feature of the tool

**Features:**
- Cursor presence broadcast (show user position)
- Live move updates (when story/feature moved)
- Live team member updates
- Conflict resolution for concurrent moves

**Files to Modify:**
- `Hubs/PlanningHub.cs` - Implement message handlers
- Frontend: Create SignalR service
- Frontend: Board components - Subscribe to hub updates

**Acceptance Criteria:**
- ✅ Cursor presence works for multiple users
- ✅ Move updates broadcast to all clients
- ✅ Build: 0 errors

---

### PHASE 3C: Responsive UI & Mobile Support (Medium Priority)
**Status:** Future Enhancement

#### **Mobile-Responsive Design** (Medium) — Est. 8-10 hours
**Why:** Tool should work on tablets/mobile for on-the-go planning

**Changes:**
- Refactor grid layout for responsive breakpoints
- Touch-friendly drag-drop (larger hit targets)
- Mobile-optimized modals (full-screen on small screens)
- Adapt capacity view for narrow screens

**Files to Modify:**
- All component CSS files - Add responsive rules
- Board layout - Mobile-first grid rethink
- Touch event handlers for drag-drop

**Acceptance Criteria:**
- ✅ Functional on iPad (tablet size)
- ✅ Readable on mobile smaller than 375px
- ✅ Drag-drop works with touch events
- ✅ Build: 0 errors

---

### PHASE 3D: Story Refresh Confirmation Modal (Low Priority)
**Status:** Backlog

#### **Story Refresh Confirmation** (Optional Enhancement)
**Description:** When refreshing feature from Azure, if new user stories are found, prompt user to confirm before adding

**Implementation:**
- Backend: Return list of new stories discovered during refresh
- Frontend: Show confirmation modal with new stories  
- User can accept or decline adding new stories

**Status:** Parked for future enhancement (not blocking current phases)

---

### PHASE 4: Board Lock Endpoints (To be assigned)
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

## 📊 Dependency Chain

```
Completed (Phase 1):
  ✅ Global Exception Middleware
  ✅ Input Validation & Error Handling
       ↓
Completed (Phase 2):
  ✅ Board State Endpoints
  ✅ Board Finalization & Analysis
       ↓
Completed (Phase 3A):
  ✅ UI Component Refactoring (6 subcomponents)
  ✅ Dark-Mode Theme System
  ✅ Dev/Test Toggle Integration
       ↓
Upcoming (Phase 3B):
  → Real-time Collaboration (SignalR)
  → Mobile Responsive UI (Phase 3C)
  → Story Refresh Modal (Phase 3D)
  → Board Lock/Unlock (Phase 4)
```

---

## 📈 Success Metrics

- [x] ✅ Phase 1 complete: Global exception handling & input validation
- [x] ✅ Phase 2 complete: Board finalization with analysis features
- [x] ✅ Phase 3A complete: Component refactoring & dark-mode implementation
- [x] ✅ API returns consistent error responses
- [x] ✅ Frontend implements all finalization UI/UX
- [x] ✅ Board CSS reduced by 83% (1277 → 214 lines main)
- [x] ✅ All components use standalone: true & signals
- [x] ✅ No technical debt added
- [x] ✅ Backend: 0 errors | Frontend: 0 compilation errors
- [ ] Phase 3B: Real-time collaboration (SignalR)
- [ ] Phase 3C: Responsive mobile UI
- [ ] Phase 4: Board lock/unlock endpoints
- [ ] Code coverage > 80% for critical paths

---

## 📝 Completed Branches & PRs

- ✅ `chore/uiRefactoring` - Phase 3A: Board component refactoring, dark-mode, subcomponents (In Progress/Ready for PR)
- ✅ `board-finalization-feature` - Phase 2: Board finalization, analysis features, UI/UX (Completed previously)
- ✅ `boardSearchFiltering` - Phase 1: Board search with mandatory org/project (Completed previously)

---

## 📋 Files Modified in Phase 3A Sprint

### Frontend
- `Components/board/board.ts` - Cleaned imports, removed duplicate signals
- `Components/board/board.html` - Refactored to use 6 subcomponents
- `Components/board/board.css` - Consolidated from 1277 to 214 lines
- `Components/board/board-header/` - New standalone component (toggle, dev/test mode)
- `Components/board/team-bar/` - New standalone component (team management)
- `Components/board/capacity-row/` - New standalone component (capacity display & edit)
- `Components/board/sprint-header/` - New standalone component (column headers)
- `Components/board/feature-row/` - New standalone component (feature cards)
- `Components/board/board-modals/` - New standalone component (dialogs)

### Documentation
- `CHANGELOG.md` - Added Phase 3A entry with full details
- `ARCHITECTURE.md` - Added UI component architecture section
- `frontend/pi-planning-ui/README.md` - Added component architecture details
- `README.md` - Updated status to Phase 3A complete
- `ROADMAP_CURRENT.md` - Updated priorities and dependency chain

---

## 📝 Previous Phases Files

### Backend (Phase 2)
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
