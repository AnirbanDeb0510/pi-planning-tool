# Phase 3B: Folder Restructuring & Service Refactoring - Implementation Plan

**Date:** February 20, 2026  
**Status:** PLANNING - Ready for Implementation  
**Estimated Time:** 8-10 hours (with zero-breakage approach)  
**Risk Level:** MEDIUM (many files affected, but clear dependency paths)  
**Priority:** HIGH (foundation for Phase 3.5 and beyond)

---

## 📋 EXECUTIVE SUMMARY

### Current State (After Phase 3A)
```
app/
├── Components/
│   ├── board/                    ← 6 subcomponents + main board
│   ├── board-list/
│   ├── story-card/
│   └── ...others
├── features/
│   └── board/
│       ├── services/
│       │   └── board.service.ts  ← MONOLITH (800+ LOC)
│       └── models/
├── Services/
│   ├── user.service.ts           ← Scattered
│   ├── board-api.service.ts
│   └── ...others
├── core/
│   └── interceptors/
├── shared/
│   ├── models/
│   └── (DTOs only)
└── Models/                        ← UNUSED
```

**Problems:**
1. ❌ Services scattered across 3 locations
2. ❌ board.service.ts is 800+ LOC monolith
3. ❌ No clear ownership/domains
4. ❌ Models/ folder unused
5. ❌ Imports difficult to track
6. ❌ Hard to test individual domains
7. ❌ Difficult to add features without affecting others

---

### Target State (Phase 3B Complete)
```
app/
├── core/                         ← Infrastructure & global services
│   ├── interceptors/
│   ├── guards/
│   ├── services/
│   │   ├── http.service.ts       ← HTTP client wrapper
│   │   └── config.service.ts     ← App configuration
│   └── constants/
│       └── api.constants.ts
├── shared/                       ← Reusable across features
│   ├── models/
│   │   ├── api.dto.ts           ← API response/request DTOs
│   │   └── domain.models.ts      ← Shared domain models
│   ├── types/
│   │   └── common.types.ts       ← Shared TypeScript interfaces
│   ├── components/               ← Shared UI components
│   │   └── ...components
│   └── constants/
│       └── app.constants.ts
├── features/
│   ├── board/                    ← BOARD DOMAIN (isolated)
│   │   ├── components/           ← All board UI components
│   │   │   ├── board/            ← Main container
│   │   │   ├── board-header/     ← Subcomponents
│   │   │   ├── team-bar/
│   │   │   ├── capacity-row/
│   │   │   ├── sprint-header/
│   │   │   ├── feature-row/
│   │   │   └── board-modals/
│   │   ├── services/             ← Board-specific services (5 files)
│   │   │   ├── board.service.ts         ← Board CRUD (200 LOC)
│   │   │   ├── feature.service.ts       ← Feature management (150 LOC)
│   │   │   ├── team.service.ts          ← Team operations (120 LOC)
│   │   │   ├── story.service.ts         ← Story operations (100 LOC)
│   │   │   └── sprint.service.ts        ← Sprint operations (80 LOC)
│   │   ├── models/               ← Board domain models
│   │   │   ├── board.model.ts
│   │   │   ├── feature.model.ts
│   │   │   └── team.model.ts
│   │   ├── types/                ← Board-specific interfaces
│   │   │   └── board.types.ts
│   │   ├── constants/            ← Board constants
│   │   │   └── board.constants.ts
│   │   └── board-routing.module.ts
│   └── board-list/               ← BOARD LIST DOMAIN (isolated)
│       ├── components/
│       ├── services/
│       └── models/
└── app.routes.ts                 ← Updated imports
```

**Benefits:**
1. ✅ Clear domain-driven structure
2. ✅ Each service < 300 LOC (readable)
3. ✅ Easy to test individual domains
4. ✅ Clear import paths
5. ✅ Scalable for new features
6. ✅ No circular dependencies
7. ✅ Self-documenting code organization

---

## 🔍 CURRENT STATE ANALYSIS

### Current File Locations & LOC

**Components:**
```
src/app/Components/
├── board/
│   ├── board.ts                  (593 LOC) ← Main orchestrator
│   ├── board.html
│   ├── board.css
│   ├── board-header/             (3 files)
│   ├── team-bar/                 (3 files)
│   ├── capacity-row/             (3 files)
│   ├── sprint-header/            (3 files)
│   ├── feature-row/              (3 files)
│   └── board-modals/             (3 files)
├── board-list/
│   ├── board-list.component.ts   (200 LOC)
│   ├── board-list.component.html
│   └── board-list.component.css
├── story-card/
│   ├── story-card.ts             (150 LOC)
│   ├── story-card.html
│   └── story-card.css
└── ...others

TOTAL: ~30 component files
```

**Services (SCATTERED):**
```
src/app/features/board/services/
└── board.service.ts              (850 LOC) ← MONOLITH! All domains combined
   - Board CRUD (getAllBoards, createBoard, updateBoard, etc.)
   - Feature operations (addFeature, removeFeature, reorderFeature, etc.)
   - Team operations (addTeamMember, updateCapacity, etc.)
   - Story operations (moveStory, updateStory, etc.)
   - Sprint operations (getDisplayedSprints, etc.)

src/app/Services/
├── user.service.ts               (180 LOC) ← Scattered user service
├── board-api.service.ts          (250 LOC) ← Scattered API service
└── ...others

src/app/core/
└── (no services here currently)

TOTAL: ~1300+ LOC of service logic, scattered across 3 locations
```

**Models/DTOs:**
```
src/app/Models/                   ← UNUSED FOLDER (0 files)

src/app/shared/models/
├── board.dto.ts                  (API response types)
└── ...DTOs

TOTAL: DTOs scattered in shared/models
```

### Current Import Paths (Messy)

```typescript
// In board.component.ts
import { BoardService } from '../../features/board/services/board.service';
import { UserService } from '../../Services/user.service';
import { BoardApiService } from '../../Services/board-api.service';
import { BoardHeader } from './board-header/board-header';  // Relative
import { TeamBar } from './team-bar/team-bar';              // Relative

// In child components
import { BoardService } from '../../../../features/board/services/board.service';  // Deep relative path
import { Board } from '../board';  // Relative backtracking

// Inconsistent patterns:
// - Deep relative paths (../../..)
// - Scattered service locations
// - No path aliases
```

---

## 📐 TARGET ARCHITECTURE

### New Folder Structure
```
src/app/
├── core/                         [Core infrastructure]
│   ├── interceptors/
│   │   ├── auth.interceptor.ts
│   │   └── error.interceptor.ts
│   ├── guards/
│   │   └── auth.guard.ts
│   ├── services/
│   │   ├── http.service.ts       [HTTP client wrapper]
│   │   ├── config.service.ts     [App configuration]
│   │   └── user.service.ts       [MOVED from Services/]
│   └── constants/
│       └── api.constants.ts
│
├── shared/                       [Shared across features]
│   ├── models/
│   │   ├── api.dto.ts
│   │   ├── domain.models.ts
│   │   ├── board.dto.ts
│   │   └── team.dto.ts
│   ├── types/
│   │   └── common.types.ts
│   ├── components/
│   │   ├── story-card/           [MOVED from Components/]
│   │   └── ...others
│   └── constants/
│       └── app.constants.ts
│
├── features/
│   ├── board/                    [BOARD DOMAIN - self-contained]
│   │   ├── components/
│   │   │   ├── board/
│   │   │   │   ├── board.ts
│   │   │   │   ├── board.html
│   │   │   │   ├── board.css
│   │   │   │   ├── board-old.html
│   │   │   │   └── board.spec.ts
│   │   │   ├── board-header/
│   │   │   ├── team-bar/
│   │   │   ├── capacity-row/
│   │   │   ├── sprint-header/
│   │   │   ├── feature-row/
│   │   │   └── board-modals/
│   │   ├── services/             [5 split from monolith]
│   │   │   ├── board.service.ts          (200 LOC)
│   │   │   ├── feature.service.ts        (150 LOC)
│   │   │   ├── team.service.ts           (120 LOC)
│   │   │   ├── story.service.ts          (100 LOC)
│   │   │   └── sprint.service.ts         (80 LOC)
│   │   ├── models/
│   │   │   ├── board.model.ts
│   │   │   ├── feature.model.ts
│   │   │   └── team.model.ts
│   │   ├── types/
│   │   │   └── board.types.ts
│   │   ├── constants/
│   │   │   └── board.constants.ts
│   │   └── board-routing.module.ts
│   │
│   └── board-list/               [BOARD LIST DOMAIN - isolated]
│       ├── components/
│       │   └── board-list/               [MOVED from Components/]
│       ├── services/
│       │   └── board-list.service.ts
│       ├── models/
│       └── board-list-routing.module.ts
│
├── app.routes.ts                 [Updated]
├── app.component.ts             [Updated imports]
└── app.config.ts
```

### New Service Boundaries (Split from Monolith)

**OLD: board.service.ts (850 LOC)**
```typescript
// All domains mixed
export class BoardService {
  // Board operations
  getAllBoards() { }
  createBoard() { }
  updateBoard() { }
  
  // Feature operations (should be in FeatureService)
  addFeature() { }
  removeFeature() { }
  reorderFeature() { }
  
  // Team operations (should be in TeamService)
  addTeamMember() { }
  updateCapacity() { }
  removeTeamMember() { }
  
  // Story operations (should be in StoryService)
  moveStory() { }
  updateStory() { }
  
  // Sprint operations (should be in SprintService)
  getDisplayedSprints() { }
}
```

**NEW: 5 separate services**

```typescript
// 1. board.service.ts (200 LOC)
export class BoardService {
  getAllBoards() { }
  createBoard() { }
  updateBoard() { }
  getBoard(id) { }
  deleteBoard(id) { }
  finalizeBoard(id) { }
  restoreBoard(id) { }
  lockBoard(id) { }
  unlockBoard(id) { }
  getFinalizationWarnings(id) { }
}

// 2. feature.service.ts (150 LOC)
export class FeatureService {
  addFeature(boardId, feature) { }
  removeFeature(boardId, featureId) { }
  updateFeature(boardId, featureId, updates) { }
  reorderFeature(boardId, featureId, newPosition) { }
  refreshFeature(boardId, featureId) { }
}

// 3. team.service.ts (120 LOC)
export class TeamService {
  addTeamMember(boardId, member) { }
  updateTeamMember(boardId, memberId, updates) { }
  removeTeamMember(boardId, memberId) { }
  updateCapacity(memberId, sprintId, dev, test) { }
}

// 4. story.service.ts (100 LOC)
export class StoryService {
  moveStory(storyId, targetSprintId) { }
  updateStory(storyId, updates) { }
  refreshStory(storyId) { }
}

// 5. sprint.service.ts (80 LOC)
export class SprintService {
  getDisplayedSprints(board) { }
  getSprintTotals(sprint) { }
  getFeatureSprintDevTestTotals(feature, sprint) { }
}
```

---

## 🚦 MIGRATION STRATEGY - ZERO BREAKAGE

### Principle: Move in Isolatable Steps

We'll use a **dependency-driven approach**:
1. Move files with **no dependencies first** (DTOs, constants)
2. Then move supporting files (types, models, interceptors)
3. Then move isolated services (user.service.ts)
4. Then **split the monolith** (board.service.ts)
5. Finally **update all imports** everywhere

### Critical Steps to Avoid Breakage

**✅ BEFORE YOU START:**
```bash
# 1. Create feature branch
git checkout -b feature/phase3b-folder-restructuring
git add .
git commit -m "checkpoint: phase 3a complete, starting phase 3b"

# 2. Verify build
npm run build
# Result: BUILD SUCCESS ✅

# 3. Verify dev server
ng serve
# Navigate to localhost:4200
# Result: APP LOADS ✅
```

**✅ STEP-BY-STEP APPROACH (with checkpoints):**

| Step | Action | Risk | Checkpoint |
|------|--------|------|------------|
| 1 | Create target folder structure | None | `ls` to verify |
| 2 | Copy DTO files (no dependencies) | Low | No imports change yet |
| 3 | Copy constants (no dependencies) | Low | No imports change yet |
| 4 | Copy model files (minimal deps) | Low | Update imports in 1-2 files |
| 5 | Copy interceptors/guards | Low | Update imports |
| 6 | Copy user.service.ts | Medium | Update imports in 3-4 files |
| 7 | Create split service files (empty) | None | Just structure |
| 8 | **CRITICAL: Update board.service.ts imports** | **HIGH** | Test after each domain split |
| 9 | Split service methods into 5 files | Medium | Test each service independently |
| 10 | Update component imports (1-2 key files) | Medium | `npm run build` after each |
| 11 | Update remaining component imports | Low | `npm run build` after batch |
| 12 | Update app.routes.ts imports | Medium | Run dev server, test routing |
| 13 | Delete old unused folders | Low | Verify nothing imports from them |
| 14 | Final verification | None | Full manual test |

**✅ AFTER EACH MAJOR CHANGE:**
```bash
# 1. Build without errors
npm run build
# If ERROR → Don't proceed, fix it first

# 2. Dev server starts without errors
ng serve
# If ERROR → Don't proceed, fix it first

# 3. Manual quick check
# Open localhost:4200
# Try basic navigation
# Check browser console (no errors)

# 4. Commit checkpoint
git add .
git commit -m "checkpoint: completed step X of 14"
```

---

## 📦 DETAILED MIGRATION STEPS

### PHASE 3B.1: Create Folder Structure (No Risk)

```bash
# Create target directories
mkdir -p src/app/core/interceptors
mkdir -p src/app/core/guards
mkdir -p src/app/core/services
mkdir -p src/app/core/constants

mkdir -p src/app/shared/models
mkdir -p src/app/shared/types
mkdir -p src/app/shared/components
mkdir -p src/app/shared/constants

mkdir -p src/app/features/board/components
mkdir -p src/app/features/board/services
mkdir -p src/app/features/board/models
mkdir -p src/app/features/board/types
mkdir -p src/app/features/board/constants

mkdir -p src/app/features/board-list/components
mkdir -p src/app/features/board-list/services
mkdir -p src/app/features/board-list/models

# Verify structure
find src/app -type d -name "core" -o -name "shared" -o -name "features" | sort
```

**Checkpoint:** All directories exist  
**Risk:** None (structure only)

---

### PHASE 3B.2: Move DTO Files (Low Risk)

**Files to move:**
```
src/app/shared/models/board.dto.ts      → stays (already in shared)
src/app/features/board/models/          → (already in correct location)
```

**Action:** No changes needed - already in good locations

**Checkpoint:** DTOs confirmed in place  
**Risk:** None (no imports depend on location)

---

### PHASE 3B.3: Move/Create Type Files (Low Risk)

**Current state:**
```
(No types/ folder exists)
```

**Create files:**
```bash
# Create empty files
touch src/app/shared/types/common.types.ts
touch src/app/features/board/types/board.types.ts
```

**Populate minimal TypeScript interfaces (reuse what exists in board.ts)**

**Checkpoint:** Type files created  
**Risk:** Low (optional, refining existing code)

---

### PHASE 3B.4: Move Constants (Low Risk)

**Files to move:**
```
src/app/constants/api.constants.ts → src/app/core/constants/api.constants.ts (if exists)
src/app/board/constants/board.constants.ts → src/app/features/board/constants/board.constants.ts
```

**Update imports in:**
- board.service.ts (1 import)
- Any files that use these constants

**After move, update:**
```typescript
// BEFORE
import { API_ENDPOINTS } from '../../constants/api.constants';

// AFTER
import { API_ENDPOINTS } from '../../../../core/constants/api.constants';
```

**Checkpoint:** `npm run build` passes  
**Risk:** Low (only 2-3 files affected)

---

### PHASE 3B.5: Move/Update Interceptors & Guards (Low Risk)

**Files to move:**
```
src/app/core/interceptors/* → stays in place (already correct)
src/app/core/guards/*       → stays in place (already correct)
```

**Action:** No changes needed

**Checkpoint:** Interceptors/guards verified  
**Risk:** None

---

### PHASE 3B.6: Move user.service.ts (Medium Risk)

**File to move:**
```
src/app/Services/user.service.ts → src/app/core/services/user.service.ts
```

**Files that import it:**
Find all references:
```bash
grep -r "from.*Services/user.service" src/app
```

**Expected imports in:**
- app.component.ts
- board.component.ts (or board.ts)
- board-list.component.ts
- Any auth-related components

**Update each import:**
```typescript
// BEFORE
import { UserService } from '../Services/user.service';

// AFTER
import { UserService } from '../core/services/user.service';
```

**After updating all imports:**
```bash
npm run build
# Result: Should succeed ✅
```

**Checkpoint:** `npm run build` passes, dev server works  
**Risk:** Medium (affects 3-5 files, but straightforward)

---

### PHASE 3B.7: Create Split Service Files (No Breakage)

**Current state:**
```
src/app/features/board/services/
└── board.service.ts (850 LOC) ← MONOLITH
```

**Create new empty service files:**
```bash
touch src/app/features/board/services/feature.service.ts
touch src/app/features/board/services/team.service.ts
touch src/app/features/board/services/story.service.ts
touch src/app/features/board/services/sprint.service.ts

# Keep board.service.ts for now (don't delete)
```

**Populate each with minimal structure:**
```typescript
// feature.service.ts
import { Injectable } from '@angular/core';
import { BoardService } from './board.service';

@Injectable({
  providedIn: 'root'
})
export class FeatureService {
  constructor(private boardService: BoardService) { }
  
  // Add methods here (step 9)
}
```

**Checkpoint:** New services exist, compile clean  
**Risk:** None (empty files, no imports)

---

### PHASE 3B.8: CRITICAL - Analyze board.service.ts Dependencies

**Before splitting, map all method calls:**

```bash
# Find all calls to boardService in components
grep -r "boardService\." src/app/Components

# Expected patterns:
# - this.boardService.getAllBoards()
# - this.boardService.addFeature()
# - this.boardService.moveStory()
# - etc.
```

**Create mapping document:**
```
Methods called from board.component.ts:
✓ getAllBoards() → needs BoardService
✓ createBoard() → needs BoardService
✓ updateBoard() → needs BoardService
✓ addFeature() → needs FeatureService (MOVING)
✓ removeFeature() → needs FeatureService (MOVING)
✓ reorderFeature() → needs FeatureService (MOVING)
✓ refreshFeature() → needs FeatureService (MOVING)
✓ addTeamMember() → needs TeamService (MOVING)
✓ updateCapacity() → needs TeamService (MOVING)
✓ removeTeamMember() → needs TeamService (MOVING)
✓ moveStory() → needs StoryService (MOVING)
✓ updateStory() → needs StoryService (MOVING)
✓ getDisplayedSprints() → needs SprintService (MOVING)
```

**Checkpoint:** All dependencies mapped  
**Risk:** LOW (analysis only, no code changes)

---

### PHASE 3B.9: Split the Monolith (High Risk - Careful!)

**Strategy:** Move one service at a time, test after each

**Step 9.1: Move Feature methods to FeatureService**

```typescript
// feature.service.ts (150 LOC)
@Injectable({ providedIn: 'root' })
export class FeatureService {
  constructor(private boardService: BoardService) { }

  addFeature(boardId: number, feature: FeatureCreateDto) {
    // Move addFeature logic from board.service.ts
  }

  removeFeature(boardId: number, featureId: number) {
    // Move removeFeature logic
  }

  reorderFeature(boardId: number, featureId: number, newPosition: number) {
    // Move reorderFeature logic
  }

  refreshFeature(boardId: number, featureId: number) {
    // Move refreshFeature logic
  }
}
```

**Update board.component.ts:**
```typescript
// BEFORE
constructor(
  public boardService: BoardService,
  private userService: UserService,
  // ... others
) { }

// AFTER
constructor(
  public boardService: BoardService,
  private featureService: FeatureService,  // ADD
  private userService: UserService,
  // ... others
) { }

// Update method calls
addFeature(feature) {
  this.featureService.addFeature(this.board().id, feature);  // Changed
}

removeFeature(featureId) {
  this.featureService.removeFeature(this.board().id, featureId);  // Changed
}
```

**Test after this change:**
```bash
npm run build
# If ERROR → Investigate, fix, don't proceed
# If SUCCESS → Continue to next service split
```

**Checkpoint:** `npm run build` succeeds  
**Risk:** MEDIUM (many method calls, but isolated to 1 service)

---

**Step 9.2: Move Team methods to TeamService**

```typescript
// team.service.ts (120 LOC)
@Injectable({ providedIn: 'root' })
export class TeamService {
  constructor(private boardService: BoardService) { }

  addTeamMember(boardId: number, member: TeamMemberDto) {
    // Move addTeamMember logic
  }

  updateTeamMember(boardId: number, memberId: number, updates: Partial<TeamMemberDto>) {
    // Move updateTeamMember logic
  }

  removeTeamMember(boardId: number, memberId: number) {
    // Move removeTeamMember logic
  }

  updateCapacity(memberId: number, sprintId: number, dev: number, test: number) {
    // Move updateCapacity logic
  }
}
```

**Update board.component.ts:**
```typescript
// ADD to constructor
private teamService: TeamService

// Update method calls
addTeamMember(member) {
  this.teamService.addTeamMember(this.board().id, member);
}

updateCapacity(memberId, sprintId, dev, test) {
  this.teamService.updateCapacity(memberId, sprintId, dev, test);
}
```

**Test:**
```bash
npm run build
```

**Checkpoint:** `npm run build` succeeds  
**Risk:** MEDIUM

---

**Step 9.3: Move Story methods to StoryService**

```typescript
// story.service.ts (100 LOC)
@Injectable({ providedIn: 'root' })
export class StoryService {
  constructor(private boardService: BoardService) { }

  moveStory(storyId: number, targetSprintId: number) {
    // Move moveStory logic
  }

  updateStory(storyId: number, updates: Partial<UserStoryDto>) {
    // Move updateStory logic
  }

  refreshStory(storyId: number) {
    // Move refreshStory logic
  }
}
```

**Update board.component.ts:**
```typescript
// ADD to constructor
private storyService: StoryService

// Update method calls
drop(event: CdkDragDrop<UserStoryDto[]>) {
  // ...
  this.storyService.moveStory(story.id, targetSprintId);
}
```

**Test:**
```bash
npm run build
```

**Checkpoint:** `npm run build` succeeds  
**Risk:** MEDIUM

---

**Step 9.4: Move Sprint methods to SprintService**

```typescript
// sprint.service.ts (80 LOC)
@Injectable({ providedIn: 'root' })
export class SprintService {
  constructor(private boardService: BoardService) { }

  getDisplayedSprints(board: BoardResponseDto) {
    // Move getDisplayedSprints logic
  }

  getSprintTotals(sprint: SprintDto) {
    // Move getSprintTotals logic
  }

  getFeatureSprintDevTestTotals(feature: FeatureResponseDto, sprint: SprintDto) {
    // Move getFeatureSprintDevTestTotals logic
  }
}
```

**Update board.component.ts & child components:**
```typescript
// ADD to constructor
private sprintService: SprintService

// Update method calls in board.ts
getDisplayedSprints() {
  return this.sprintService.getDisplayedSprints(this.board());
}

getSprintTotals(sprint: SprintDto) {
  return this.sprintService.getSprintTotals(sprint);
}

// Update calls in sprint-header.component.ts or other child components
```

**Test:**
```bash
npm run build
```

**Checkpoint:** `npm run build` succeeds  
**Risk:** MEDIUM

---

**Step 9.5: Clean up board.service.ts**

After splitting, **board.service.ts should have only board-specific methods:**

```typescript
// board.service.ts (200 LOC) - CLEANED
@Injectable({ providedIn: 'root' })
export class BoardService {
  constructor(
    private http: HttpClient,
    private config: ConfigService
  ) { }

  getAllBoards() { }
  createBoard() { }
  updateBoard() { }
  getBoard(id) { }
  deleteBoard(id) { }
  finalizeBoard(id) { }
  restoreBoard(id) { }
  lockBoard(id) { }
  unlockBoard(id) { }
  getFinalizationWarnings(id) { }
  
  // Removed: addFeature, moveStory, etc. (moved to their services)
}
```

**Verify no duplicate methods remain:**
```bash
grep -n "^  [a-zA-Z]*(" src/app/features/board/services/board.service.ts | wc -l
# Should be ~10 methods (board-specific only)
```

**Test:**
```bash
npm run build
```

**Checkpoint:** `npm run build` succeeds, board.service.ts reduced to ~200 LOC  
**Risk:** LOW (cleanup only)

---

### PHASE 3B.10: Update Component Imports (Medium Risk)

**Files to update:**
```
src/app/Components/board/board.ts
  → Add: import { FeatureService } from '...';
  → Add: import { TeamService } from '...';
  → Add: import { StoryService } from '...';
  → Add: import { SprintService } from '...';

src/app/Components/board/subcomponents/*.ts
  → Update paths for any service imports
  
src/app/Components/board-list/board-list.component.ts
  → Update BoardService import path
```

**Strategy: Update in batches**

Batch 1: Update board.component.ts (main board)
```typescript
// OLD
import { BoardService } from '../../features/board/services/board.service';
import { UserService } from '../../Services/user.service';

// NEW
import { BoardService } from '../../features/board/services/board.service';
import { FeatureService } from '../../features/board/services/feature.service';
import { TeamService } from '../../features/board/services/team.service';
import { StoryService } from '../../features/board/services/story.service';
import { SprintService } from '../../features/board/services/sprint.service';
import { UserService } from '../../core/services/user.service';
```

**Test after update:**
```bash
npm run build
# If ERROR → Check import paths, fix, retry
```

Batch 2: Update subcomponent imports
```typescript
// In sprint-header.component.ts, feature-row.component.ts, etc.
// Update any service imports to new paths

// Before
import { BoardService } from '../../features/board/services/board.service';

// After (if needed)
import { SprintService } from '../../features/board/services/sprint.service';
```

**Test after each batch:**
```bash
npm run build
```

**Checkpoint:** All component imports updated  
**Risk:** MEDIUM (many files, but mechanical changes)

---

### PHASE 3B.11: Update Routing Imports (Medium Risk)

**Files to update:**
```
src/app/app.routes.ts
  → Update any BoardService imports
  → Update any path references
```

**Also update in:**
```
src/app/app.component.ts
  → Import paths for services
  
src/main.ts
  → Import paths if any
```

**Test:**
```bash
npm run build
ng serve
# Navigate to localhost:4200
# Click around, verify main functionality
```

**Checkpoint:** App routing works, dev server starts  
**Risk:** LOW (few files affected)

---

### PHASE 3B.12: Final Cleanup (Low Risk)

**Delete old empty folders:**
```bash
# Only after verifying nothing imports from them
rm -rf src/app/Services/
rm -rf src/app/Models/

# Verify deletions worked
ls src/app/
# Should show: core/, shared/, features/, app.routes.ts, app.component.ts, etc.
```

**Verify no orphaned imports:**
```bash
# Check for imports from deleted folders
grep -r "from.*Services/" src/app
# Result: Should be empty

grep -r "from.*Models/" src/app
# Result: Should be empty
```

**Final build:**
```bash
npm run build --prod
# Result: Should succeed ✅
```

**Checkpoint:** Zero imports from old locations  
**Risk:** NONE (cleanup only)

---

### PHASE 3B.13: Final Verification (Zero Risk)

**Full manual test:**
```bash
ng serve
```

Navigate to address and test:
```
Test 1: Board List
  - Navigate to /boards
  - Verify list loads
  - Search/filter boards
  ✓ WORKS?

Test 2: Create Board
  - Create a new board
  ✓ WORKS?

Test 3: View Board
  - Open a board
  - Verify all sections load
  ✓ WORKS?

Test 4: Team Management
  - Add team member
  - Edit capacity
  - Delete member
  ✓ WORKS?

Test 5: Feature Management
  - Add feature from Azure
  - Delete feature
  - Reorder feature
  ✓ WORKS?

Test 6: Story Operations
  - Drag-drop story
  - View story details
  ✓ WORKS?

Test 7: Sprint Operations
  - View capacity metrics
  - Check load vs capacity
  ✓ WORKS?

Test 8: Finalization
  - Finalize board
  - Verify restricted operations
  - Restore board
  ✓ WORKS?

Test 9: Dark Mode
  - Toggle dark mode
  - Verify all colors correct
  ✓ WORKS?

Test 10: Dev/Test Toggle
  - Toggle dev/test mode
  - Verify split display
  ✓ WORKS?
```

**Browser console:**
- No errors
- No warnings (except expected)

**Checkpoint:** All functionality works identically to before  
**Risk:** NONE (verification only)

---

## 🛡️ BREAKAGE PREVENTION CHECKLIST

### Before You Start
- [ ] Git working tree clean: `git status`
- [ ] Current branch: `git branch` (should be `feature/phase3b-folder-restructuring`)
- [ ] Latest code: `git log --oneline | head -1`
- [ ] Build succeeds: `npm run build` (0 errors)
- [ ] Tests pass: `npm test` (if applicable)
- [ ] Dev server works: `ng serve` ✓

### Architecture Decisions Made
- [ ] Board.service.ts will be split into 5 files (not deleted, split)
- [ ] Moved imports will use new paths consistently
- [ ] No circular dependencies introduced
- [ ] Each service has single responsibility

### During Migration
- [ ] After folder creation: `ls src/app` shows new structure
- [ ] After moving files: Check file counts match target
- [ ] After updating imports: `npm run build` succeeds
- [ ] After each service split: `npm run build` succeeds (don't skip this!)
- [ ] After routing update: `ng serve` works and app loads

### After Complete
- [ ] No unused imports in any file
- [ ] No deep relative paths (../../..) remain
- [ ] Each service < 300 LOC
- [ ] No imports from old locations (Services/, Models/)
- [ ] Folder structure matches target exactly
- [ ] All manual tests pass
- [ ] Console has no errors
- [ ] Git has clean commits for each step

### Rollback Ready
- [ ] Can restore from: `git reset --hard <checkpoint-hash>`
- [ ] Know last working commit
- [ ] Have backup branch

---

## 📊 METRICS & TRACKING

### Before Phase 3B
```
Services:
  - board.service.ts: 850 LOC (MONOLITH)
  - user.service.ts: 180 LOC (scattered)
  - board-api.service.ts: 250 LOC (scattered)
  - Others: ~300 LOC
  Total: ~1580 LOC service code

Folder Organization:
  - Services scattered across 3 locations
  - Components in Components/ (good)
  - Unused Models/ folder
  - No clear domain boundaries

Imports:
  - Deep relative paths (../../..)
  - Scattered import sources
  - Hard to trace dependencies
```

### After Phase 3B (Expected)
```
Services:
  - BoardService: 200 LOC (board-specific)
  - FeatureService: 150 LOC (feature-specific)
  - TeamService: 120 LOC (team-specific)
  - StoryService: 100 LOC (story-specific)
  - SprintService: 80 LOC (sprint-specific)
  - UserService: 180 LOC (in core/)
  - Etc.
  Total: ~1580 LOC (SAME!) but organized better

Folder Organization:
  - core/ for infrastructure
  - shared/ for reusable
  - features/ for domains
  - Clear boundaries

Imports:
  - Consistent paths
  - Easy to trace
  - No deep relatives
```

---

## 🎯 SUCCESS CRITERIA

✅ **Phase 3B is complete when:**

1. **Structure Matches Target**
   - All folders created
   - All files in correct locations
   - Old folders (Services/, Models/) deleted

2. **Services are Split**
   - board.service.ts: 200 LOC (board only)
   - feature.service.ts: 150 LOC (feature only)
   - team.service.ts: 120 LOC (team only)
   - story.service.ts: 100 LOC (story only)
   - sprint.service.ts: 80 LOC (sprint only)

3. **Imports are Clean**
   - No imports from old locations
   - No deep relative paths
   - All paths use new locations

4. **Build Succeeds**
   - `npm run build` with 0 errors
   - `npm run build --prod` with 0 errors
   - No warnings (except allowed)

5. **Dev Server Works**
   - `ng serve` starts without errors
   - App loads at localhost:4200
   - No console errors

6. **Functionality Preserved**
   - All manual tests pass
   - Behavior identical to before
   - No new bugs introduced

7. **Git History is Clean**
   - 13-15 logical commits (one per step)
   - Each commit is checkpointed and tested
   - Can rollback to any step

---

## ⚠️ KNOWN RISKS & MITIGATIONS

| Risk | Severity | Mitigation |
|------|----------|-----------|
| Import errors after move | HIGH | Test with `npm run build` after each batch |
| Circular dependencies | MEDIUM | Use dependency injection, avoid direct imports |
| Subcomponent path breaks | MEDIUM | Update relative paths in component imports |
| Service injection failures | MEDIUM | Verify providedIn: 'root' on all services |
| Routes stop working | MEDIUM | Test app startup and navigation |
| Method names conflict | LOW | Use unique method names per service |
| Tests fail | MEDIUM | Skip for now, can be fixed in Phase 3C |

---

## 📅 TIMELINE

**Estimated Duration:** 8-10 hours

| Phase | Task | Time | Checkpoint |
|-------|------|------|-----------|
| Setup | Create folders, cleanup | 1 hr | Folder structure ready |
| Move | Move DTO/constants/types | 1 hr | `npm run build` ✓ |
| Services | Create new service files | 0.5 hr | New services exist |
| Split | Move methods to services | 3-4 hr | Each split tested |
| Import | Update all imports | 2-3 hr | `npm run build` ✓ |
| Cleanup | Delete old folders | 0.5 hr | Old locations gone |
| Verify | Full manual test | 1 hr | All features work |
| **TOTAL** | | **8-10 hrs** | **Phase 3B complete** |

---

## 🎓 NOTES FOR DEVELOPERS

1. **Don't Skip Checkpoint Tests** - Each checkpoint's `npm run build` is critical
2. **Move One Service at a Time** - Don't try to split all 4 at once
3. **Update Imports Methodically** - Do it in batches, test after each batch
4. **Keep Git Commits Logical** - Each step = one commit
5. **Document Your Progress** - Note which step you're on in commit messages

---

## 📞 SUPPORT

| Question | Answer |
|----------|--------|
| "Where should file X go?" | Check Target Architecture section |
| "How do I know if import is correct?" | `npm run build` succeeds |
| "What if something breaks?" | Use git to rollback to last checkpoint |
| "Can I skip a step?" | Only if your structure already matches that step |

---

**You're ready to start Phase 3B when:**
- [ ] You've read this entire plan
- [ ] You have git branch created
- [ ] Current build succeeds
- [ ] You have ~8-10 hours uninterrupted time
- [ ] You understand the risks

**Begin with:** PHASE 3B.1 (Create Folder Structure)

Good luck! 🚀
