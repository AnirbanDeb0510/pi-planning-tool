# Frontend UI Folder Structure Analysis & Restructuring Plan

**Date:** February 19, 2026  
**Status:** ANALYSIS COMPLETE  
**Scope:** Includes Phase 3 component modularization integration

---

## PART 1: CURRENT STATE ANALYSIS

### Current Folder Structure

```
src/app/
├─ Components/              ← Page-level components
│  ├─ board/
│  ├─ board-list/
│  ├─ create-board/
│  ├─ enter-your-name/
│  ├─ home/
│  └─ story-card/
├─ core/                    ← Cross-cutting concerns
│  ├─ config/
│  ├─ constants/
│  └─ services/
│     ├─ theme.service.ts
│     └─ http-client.service.ts
├─ features/                ← Feature domain
│  └─ board/
│     └─ services/
│        ├─ board.service.ts
│        ├─ board-api.service.ts
│        └─ board-api.interface.ts
├─ Models/                  ← UNUSED OLD MODELS ❌
│  ├─ sprint.model.ts
│  ├─ feature.model.ts
│  └─ story.model.ts
├─ Services/                ← ISOLATED SERVICE ⚠️
│  └─ user.service.ts
├─ shared/                  ← DTOs & shared types
│  └─ models/
│     ├─ board.dto.ts
│     └─ board-api.dto.ts
└─ app.ts, app.routes.ts, app.config.ts
```

---

## PART 2: PROBLEMS IDENTIFIED

### 🔴 CRITICAL ISSUES

#### 1. **Mixed Naming Conventions**
```
❌ Components/ (PascalCase, Noun plural)
❌ Models/ (PascalCase, Noun plural - BUT UNUSED!)
❌ Services/ (PascalCase, Noun plural - BUT ISOLATED!)
✓ shared/ (lowercase, Noun singular)
✓ core/ (lowercase, Noun singular)
✓ features/ (lowercase, Noun singular)
```

**Problem:**
- No consistency in naming convention
- Makes new developers confused about where things go
- Inconsistent with Angular style guide (prefer lower case singular/plural for folders)

---

#### 2. **Duplicate Model Locations**
```
Models/                  ← OLD, UNUSED
├─ sprint.model.ts
├─ feature.model.ts
└─ story.model.ts

shared/models/           ← NEW, ACTIVE
├─ board.dto.ts      (contains Sprint, Feature, Story inside)
├─ board-api.dto.ts
```

**Problem:**
- `Models/` folder completely unused
- Developers confused which to use
- No clear "source of truth" for data models
- DTO pattern not fully adopted

---

#### 3. **Service Organization Chaos**
```
Services/user.service.ts                    ← Random root service ⚠️
core/services/theme.service.ts             ← Infrastructure service
core/services/http-client.service.ts        ← Infrastructure service
features/board/services/board.service.ts    ← Feature service
features/board/services/board-api.service.ts ← API layer
```

**Problem:**
- No clear criteria for where services should live
- UserService in its own folder for no good reason
- No separation between:
  - Infrastructure services (theme, http)
  - Feature services (board logic)
  - API services (HTTP calls)
- Makes it hard to:
  - Understand what's shared vs feature-specific
  - Add new features (where do new services go?)
  - Refactor services without breaking things

---

#### 4. **ServiceAPI/Service Split is Confused**
```
board.service.ts (563 lines)
├─ Manages board state (signals)
├─ Handles team operations
├─ Handles feature operations
├─ Coordinates with board-api.service
├─ Does PAT management
├─ Does finalization logic
└─ Contains 30+ public methods ❌ TOO MANY RESPONSIBILITIES

board-api.service.ts (223 lines)
├─ BoardApiService
├─ FeatureApiService
├─ StoryApiService
├─ TeamApiService
├─ AzureApiService
└─ All in ONE file ❌ VIOLATION OF SRP
```

**Problem:**
- `board.service.ts` = 563 lines with mixed responsibilities
- `board-api.service.ts` = 5 API services mashed into one file
- No clear separation between:
  - **State management** (signals, board logic)
  - **API coordination** (HTTP calls)
  - **Domain operations** (where logic actually lives)
  - **Feature-specific services** (team ops, feature ops, story ops)
- Makes future development hard (where does Story Dependencies service go?)

---

#### 5. **Feature Module Not Fully Utilized**
```
features/board/
└─ services/              ← Services only, no components!
   └─ board*, board-api*

MISSING:
├─ models/ (domain models specific to board feature)
├─ resolvers/ (route resolvers)
├─ guards/ (route guards)
├─ interfaces/ (feature-specific contracts)
├─ adapters/ (data adapters)
└─ types/ (feature-specific types)
```

**Problem:**
- Feature folder exists but underutilized
- Components still in root `Components/` folder (not in `features/`)
- Models in `shared/` when some are feature-specific
- Feature module has no clearstructure

---

#### 6. **Component Folder Naming Inconsistency**
```
Components/board.ts                    ← Lowercase, .ts
Components/board-list/board-list.component.ts  ← kebab-case, .component.ts
Components/home/home.component.ts      ← kebab-case, .component.ts
```

**Problem:**
- Some use `.ts`, some use `.component.ts`
- When Phase 3 creates subcomponents, inconsistency makes it worse
- New developers won't know which naming convention to follow

---

#### 7. **No Clear Separation Between View & Logic**
```
board.ts = 928 lines
├─ Template binding (HTML logic)
├─ Modal state management
├─ Drag-drop handlers
├─ Calculations (calculations SHOULD be services)
├─ UI state (should be services)
└─ Service coordination
```

**Problem:**
- Calculations scattered in component
- Should be: Component → Service → Calculation
- Makes testing hard
- Makes reusability hard

---

### ⚠️ SECONDARY ISSUES

#### 8. **DTOs Mixed with Interfaces**
```
shared/models/board.dto.ts
├─ DTOs (data transfer objects - from backend)
├─ Response types
├─ Some used in templates directly ❌
└─ No strict typing

board-api.interface.ts
├─ Service interfaces for API services
├─ But no interfaces for feature services
└─ Inconsistent application of interfaces
```

---

#### 9. **No Shared Components Folder**
```
current:
Components/board/ (page component)
Components/story-card/ (shared component)  ← Mixed!
Components/board-list/ (page)
Components/enter-your-name/ (shared)

Should be:
pages/                    ← Page-level components
├─ board-page/
├─ board-list-page/
└─ ...
shared/components/      ← Reusable components
├─ story-card/
├─ modals/
├─ dialogs/
└─ buttons/
```

**Problem:**
- Page components and shared components mixed in one folder
- Hard to see what's reusable vs what's page-specific

---

#### 10. **Constants & Config Not Well Organized**
```
core/constants/api-endpoints.constants.ts
core/config/runtime-config.ts
```

Should include:
```
core/constants/
├─ api-endpoints.ts    ✓ Present
├─ app.constants.ts    ✗ Missing (app-level constants)
├─ feature.constants.ts ✗ Missing (board feature constants)
└─ error.constants.ts  ✗ Missing (error codes)
```

---

## PART 3: IMPACT OF CURRENT STRUCTURE

### Developer Experience

**New Developer Onboarding:**
- ❓ Where do I put a new service? (Services/ or core/services/ or features/?)
- ❓ Where do I put a shared component? (Components/ which should be for pages?)
- ❓ Where do I put feature-specific types? (shared/ or features/?)
- ❓ Models/ folder seems abandoned - why is it there?

**Adding New Feature:**
- Would need to create: new Components file
- Would need to create: new services (where?)
- DTOs would go to: shared/models
- No clear pattern = lots of guessing

**Phase 3 Integration:**
- When we create 6 new board subcomponents, they'll all go to Components/board/
- But should they??? Some are feature-specific, some are shared
- Creates another mess

---

### Code Quality

**Service Responsibilities** (VIOLATES SRP)
```
board.service.ts does:
- State management (signals)
- Team operations (add/edit/delete)
- Feature operations (import/refresh/delete)
- Story operations (move)
- Board operations (finalize/restore)
- PAT management
- Cache management
- Service coordination with 5 API services

= TOO MANY REASONS TO CHANGE!
```

**API Service Responsibilities** (VIOLATES SRP)
```
board-api.service.ts contains:
- BoardApiService
- FeatureApiService
- StoryApiService
- TeamApiService
- AzureApiService

If we add Story Dependencies:
- StoryRelationshipApiService ← where does it go?
```

---

## PART 4: PROPOSED STRUCTURE

### New Architecture

```
src/app/
├─ core/                                    ← Infrastructure
│  ├─ constants/
│  │  ├─ api-endpoints.ts
│  │  ├─ app.constants.ts
│  │  └─ error.constants.ts
│  ├─ guards/
│  ├─ interceptors/
│  ├─ services/
│  │  ├─ http-client.service.ts
│  │  ├─ theme.service.ts
│  │  ├─ user.service.ts              ← MOVED from root Services/
│  │  └─ error.service.ts              ← NEW
│  └─ models/
│     ├─ http-error.model.ts            ← NEW
│     └─ response-wrapper.model.ts      ← NEW
│
├─ shared/                                  ← Cross-feature sharing
│  ├─ types/
│  │  ├─ board.types.ts                 ← Types only (not DTOs)
│  │  └─ common.types.ts
│  ├─ models/
│  │  ├─ board.dto.ts                   ← RENAME: api.dto.ts
│  │  ├─ board-api.dto.ts               ← RENAME: payload.dto.ts
│  │  └─ board-summary.dto.ts           ← NEW (clearer separation)
│  ├─ components/                         ← NEW: Reusable UI
│  │  ├─ story-card/
│  │  ├─ modals/
│  │  ├─ buttons/
│  │  └─ dialogs/
│  ├─ pipes/                              ← NEW: Custom pipes
│  ├─ directives/                         ← NEW: Custom directives
│  ├─ animations/                         ← NEW: Reusable animations
│  └─ utils/                              ← NEW: Utility functions
│     ├─ array.utils.ts
│     ├─ date.utils.ts
│     └─ validation.utils.ts
│
├─ features/                                ← Feature modules
│  ├─ board/
│  │  ├─ components/                       ← NEW: Feature components
│  │  │  ├─ board.component/              ← MOVED from Components/board/
│  │  │  │  ├─ board.ts
│  │  │  │  ├─ board.html
│  │  │  │  ├─ board.css
│  │  │  │  ├─ board.spec.ts
│  │  │  │  ├─ board-header/              ← PHASE 3
│  │  │  │  ├─ team-bar/                  ← PHASE 3
│  │  │  │  ├─ capacity-row/              ← PHASE 3
│  │  │  │  ├─ sprint-header/             ← PHASE 3
│  │  │  │  ├─ feature-row/               ← PHASE 3
│  │  │  │  └─ board-modals/              ← PHASE 3
│  │  │  ├─ board-list.component/         ← MOVED from Components/board-list/
│  │  │  └─ create-board.component/       ← MOVED from Components/create-board/
│  │  │
│  │  ├─ services/                        ← NEW: Organized API services
│  │  │  ├─ board/                        ← NEW: Logical grouping
│  │  │  │  ├─ board.service.ts           ← State + orchestration
│  │  │  │  ├─ board.api.ts              ← API calls only
│  │  │  │  ├─ board.facade.ts           ← NEW: Simplified public API
│  │  │  │  └─ board.interface.ts        ← Contracts
│  │  │  ├─ feature/
│  │  │  │  ├─ feature.service.ts
│  │  │  │  ├─ feature.api.ts
│  │  │  │  ├─ feature.facade.ts         ← NEW
│  │  │  │  └─ feature.interface.ts
│  │  │  ├─ team/
│  │  │  │  ├─ team.service.ts
│  │  │  │  ├─ team.api.ts
│  │  │  │  ├─ team.facade.ts            ← NEW
│  │  │  │  └─ team.interface.ts
│  │  │  ├─ story/
│  │  │  │  ├─ story.service.ts
│  │  │  │  ├─ story.api.ts
│  │  │  │  └─ story.interface.ts
│  │  │  ├─ azure/
│  │  │  │  ├─ azure.service.ts
│  │  │  │  ├─ azure.api.ts
│  │  │  │  └─ azure.interface.ts
│  │  │  └─ index.ts                    ← PUBLIC API (what's exported)
│  │  │
│  │  ├─ models/                         ← NEW: Feature-specific models
│  │  │  ├─ board.model.ts              ← Business logic models
│  │  │  ├─ feature.model.ts
│  │  │  ├─ team-member.model.ts
│  │  │  └─ calculations/                ← NEW: Calculation functions
│  │  │     ├─ capacity.calculations.ts
│  │  │     ├─ load.calculations.ts
│  │  │     ├─ sprint.calculations.ts
│  │  │     └─ index.ts
│  │  │
│  │  ├─ guards/                         ← NEW: Feature guards
│  │  │  └─ board.guard.ts
│  │  │
│  │  ├─ adapters/                       ← NEW: Data adapters
│  │  │  └─ board.adapter.ts             ← Convert API → UI format
│  │  │
│  │  ├─ types/                          ← NEW: Feature types
│  │  │  ├─ board.types.ts
│  │  │  ├─ feature.types.ts
│  │  │  └─ enums.ts
│  │  │
│  │  ├─ constants/                      ← NEW: Feature constants
│  │  │  ├─ board.constants.ts
│  │  │  ├─ feature.constants.ts
│  │  │  └─ validation.constants.ts
│  │  │
│  │  └─ board.module.ts                 ← Optional: Feature module definition
│  │
│  ├─ home/
│  │  ├─ components/
│  │  │  └─ home.component/
│  │  ├─ services/
│  │  └─ types/
│  │
│  ├─ auth/  (if added later)
│  │  ├─ components/
│  │  ├─ services/
│  │  └─ guards/
│  │
│  └─ index.ts                           ← NEW: Feature layer public API
│
├─ app.routes.ts
├─ app.config.ts
├─ app.ts
└─ app.css
```

---

## PART 5: DETAILED CHANGES

### 📌 CRITICAL CHANGES

#### Change 1: Rename folder to lowercase (Angular style guide)
```
Components/   →  features/board/components/
Models/       →  DELETE (unused)
Services/     →  core/services/ (user.service moved here)
```

**Rationale:**
- Angular style guide recommends lowercase folder names
- Aligns with `core`, `shared`, `features` existing pattern
- Consistency across codebase

---

#### Change 2: Move components into features/
```
Components/board/              →  features/board/components/board/
Components/board-list/         →  features/board/components/board-list/
Components/create-board/       →  features/board/components/create-board/
Components/home/               →  features/home/components/home/
Components/enter-your-name/    →  features/auth/components/enter-your-name/ (or app/)
Components/story-card/         →  shared/components/story-card/
```

**Rationale:**
- Page components grouped by feature
- Reusable components go to `shared/components/`
- Clear separation: page vs shared

---

#### Change 3: Split board-api.service into individual services
```
board-api.service.ts (223 LOC, 5 services)

→ Split into:
  ├─ services/board/board.api.ts (BoardApiService only)
  ├─ services/feature/feature.api.ts (FeatureApiService only)
  ├─ services/team/team.api.ts (TeamApiService only)
  ├─ services/story/story.api.ts (StoryApiService only)
  └─ services/azure/azure.api.ts (AzureApiService only)
```

**Rationale:**
- Each API service in its own file (SRP)
- Easier to find and modify
- When adding Story Dependencies, clear where it goes
- Supports better testing (small, focused files)

---

#### Change 4: Refactor board.service into smaller services
```
board.service.ts (563 LOC, 30+ methods)

DON'T put everything in board.service!

→ Split into domain services:
  ├─ services/board/board.service.ts (state management only)
  ├─ services/feature/feature.service.ts (feature operations)
  ├─ services/team/team.service.ts (team operations)
  ├─ services/story/story.service.ts (story operations)
  └─ services/azure/azure.service.ts (Azure integration)

Each service:
├─ Manages domain-specific state
├─ Orchestrates with its API service
├─ Single responsibility
└─ Self-contained (easy to test/reuse)
```

**Rationale:**
- Single Responsibility Principle
- Board.service becomes coordinator, not monolith
- Adding Story Dependencies: add new service, not modify existing 563 LOC
- Much easier to test: each service is small

---

#### Change 5: Create Facade Services (optional but recommended)
```
board.facade.ts
├─ Simplifies component interaction
├─ Hides complexity of coordinating multiple services
├─ Something like:

  class BoardFacade {
    // Components only call these:
    loadBoard(id)
    finalizeBoard()
    addTeamMember()
    moveStory()
    
    // Internally coordinates:
    ├─ board.service
    ├─ feature.service
    ├─ team.service
    └─ story.service
  }
```

**Rationale:**
- Components have simpler API to work with
- Hide service layer complexity
- Easier to refactor internals without affecting components

---

#### Change 6: Extract Calculations into Utility Services
```
BEFORE:
board.ts (component) contains:
├─ getSprintTotals() - calculation
├─ isSprintOverCapacity() - calculation
├─ getFeatureTotal() - calculation
└─ mixed with UI logic

AFTER:
services/calculations/
├─ sprint.calculations.ts
│  ├─ calculateSprintTotal()
│  └─ isSprintOverCapacity()
├─ feature.calculations.ts
│  └─ calculateFeatureTotal()
└─ team.calculations.ts
   └─ calculateTeamCapacity()

Called from services, not components!
```

**Rationale:**
- Testable independently
- Reusable across components
- Clear separation of concerns

---

#### Change 7: Create proper DTOs vs Types separation
```
BEFORE:
shared/models/board.dto.ts
├─ Types used in template (Response DTOs)
├─ Types used internally (Models)
└─ API Payloads (Request DTOs)
All mixed together!

AFTER:
shared/models/
├─ api.dto.ts (from backend - response objects)
├─ payload.dto.ts (to backend - request objects)

shared/types/
├─ board.types.ts (UI-specific types)
├─ feature.types.ts (Feature-specific types)

features/board/models/
├─ board.model.ts (Business logic models)
├─ team-member.model.ts (Domain models)
└─ ...
```

**Rationale:**
- Clear classification: what's from API vs UI vs business logic
- Easier to version OpenAPI contracts
- Types stay close to where used

---

### 🟡 SUPPORTING CHANGES

#### Change 8: Rename component files consistently
```
BEFORE:
├─ board.ts
├─ home.component.ts
├─ board-list.component.ts

AFTER:
├─ board.component.ts         ← Consistent naming
├─ home.component.ts          ← Already good
├─ board-list.component.ts    ← Already good
```

---

#### Change 9: Create index.ts files for cleaner imports
```
BEFORE:
import { BoardService } from '../../../features/board/services/board.service.ts'
import { BoardApiService } from '../../../features/board/services/board-api.service.ts'

AFTER:
import { BoardService, FeatureService, TeamService } 
  from '../../../features/board/services'

// Via index.ts that exports all:
export { BoardService } from './board/board.service'
export { FeatureService } from './feature/feature.service'
```

---

#### Change 10: Move UserService to core/services
```
BEFORE:
Services/user.service.ts  ← Isolated oddly

AFTER:
core/services/user.service.ts  ← Core infrastructure service
```

---

## PART 6: MIGRATION STEPS

### Phase 3A: Setup New Structure (1 hour)

1. Create new folder structure
2. Create all the new directories before moving files
3. Don't delete anything yet (commit checkpoint)

### Phase 3B: Move Features (2 hours)

1. Move board components to `features/board/components/board/`
2. Move board-list to `features/board/components/board-list/`
3. Move create-board to `features/board/components/create-board/`
4. Move home to `features/home/`
5. Move story-card to `shared/components/story-card/`
6. Update all import paths

### Phase 3C: Refactor Services (3 hours)

1. Split board-api.service into separate files:
   - `services/board/board.api.ts`
   - `services/feature/feature.api.ts`
   - `services/team/team.api.ts`
   - `services/story/story.api.ts`
   - `services/azure/azure.api.ts`

2. Keep board.service, but move team/feature/story logic:
   - Start with team operations → team.service.ts
   - Then feature operations → feature.service.ts
   - Then story operations → story.service.ts
   - Leave board.service focused on board state

3. Create facade if needed

4. Update all imports in components

### Phase 3D: Extract Calculations (1 hour)

1. Create `services/calculations/`
2. Move calculations from board.ts to utility functions
3. Update board.ts to call services

### Phase 3E: Update DTOs (30 min)

1. Rename board.dto.ts → api.dto.ts
2. Rename board-api.dto.ts → payload.dto.ts
3. Create board.types.ts in shared/types/
4. Move UI-specific types there

### Phase 3F: Cleanup (30 min)

1. Delete old Models/ folder
2. Delete old Services/ folder (user.service moved)
3. Delete old Components/ folder (moved to features)
4. Update app.routes.ts with new paths
5. Test build: 0 errors

---

## PART 7: INTEGRATION WITH PHASE 3 COMPONENT MODULARIZATION

### How New Structure Supports Phase 3

**BEFORE (confusing):**
```
Components/board/
├─ board.ts (928 LOC main)
├─ board.html
├─ board.css
└─ ... phase 3 subcomponents go here too?
  ├─ board-header/
  ├─ team-bar/
  └─ All mixed with page-level component!
```

**AFTER (clear):**
```
features/board/components/board/
├─ board.component.ts (main, orchestrator)
├─ board.component.html
├─ board.component.css
├─ board-header/              ← Phase 3: Sub-component
├─ team-bar/                  ← Phase 3: Sub-component
├─ capacity-row/              ← Phase 3: Sub-component
├─ sprint-header/             ← Phase 3: Sub-component
├─ feature-row/               ← Phase 3: Sub-component
└─ board-modals/              ← Phase 3: Sub-component

All clearly organized under parent component!
```

**Service Structure for Phase 3:**
```
When adding Story Dependencies (Phase 3.5):

services/story-dependency/
├─ story-dependency.service.ts
├─ story-dependency.api.ts
├─ story-dependency.types.ts  ← Feature types
└─ index.ts
```

---

## PART 8: KEY FILES TO RENAME/MOVE

### Files to Move

```
Components/board/ 
  → features/board/components/board/
  UPDATE: board.ts imports

Components/board-list/
  → features/board/components/board-list/
  UPDATE: imports in app.routes.ts

Components/create-board/
  → features/board/components/create-board/

Components/home/
  → features/home/components/home/
  UPDATE: imports in app.routes.ts

Components/enter-your-name/
  → features/auth/components/enter-your-name/
  (or keep in features/board/components if not separating auth)

Components/story-card/
  → shared/components/story-card/
  UPDATE: imports in board component

Services/user.service.ts
  → core/services/user.service.ts

Models/sprint.model.ts
  → DELETE (not used, use DTOs instead)

Models/feature.model.ts
  → DELETE

Models/story.model.ts
  → DELETE
```

### Files to Split

```
features/board/services/board-api.service.ts (223 lines, 5 services)

Split to:
├─ services/board/board.api.ts
├─ services/feature/feature.api.ts
├─ services/team/team.api.ts
├─ services/story/story.api.ts
└─ services/azure/azure.api.ts
```

### Files to Refactor

```
features/board/services/board.service.ts (563 lines)

Extract to:
├─ services/board/board.service.ts (state + orchestration)
├─ services/team/team.service.ts (team ops)
├─ services/feature/feature.service.ts (feature ops)
├─ services/story/story.service.ts (story ops)
└─ services/calculations/ (utility functions)
```

### Files to Rename

```
shared/models/board.dto.ts → shared/models/api.dto.ts
shared/models/board-api.dto.ts → shared/models/payload.dto.ts
```

---

## PART 9: IMPORT PATH UPDATES

### Critical Import Updates

**In board.component.ts (main):**
```typescript
// BEFORE:
import { BoardService } from '../../../features/board/services/board.service';
import { UserService } from '../../../Services/user.service';

// AFTER:
import { BoardService, FeatureService, TeamService, StoryService } 
  from '../features/board/services';
import { UserService } from '../../../core/services/user.service';
```

**In app.routes.ts:**
```typescript
// BEFORE:
import { Board } from './Components/board/board';
import { HomeComponent } from './Components/home/home.component';

// AFTER:
import { BoardComponent } from './features/board/components/board/board.component';
import { HomeComponent } from './features/home/components/home/home.component';
```

**In app.ts:**
```typescript
// BEFORE:
import { BoardService } from './features/board/services/board.service';

// AFTER:
import { BoardFacade } from './features/board/services/board.facade'; // or keep as is
```

---

## PART 10: FOLDER STRUCTURE SUMMARY

### New Root Structure

```
src/
├─ app/
│  ├─ core/                              ← Infrastructure
│  ├─ shared/                            ← Cross-cutting
│  ├─ features/
│  │  ├─ board/                          ← Feature module
│  │  │  ├─ components/
│  │  │  ├─ services/
│  │  │  ├─ models/
│  │  │  ├─ types/
│  │  │  ├─ constants/
│  │  │  └─ guards/
│  │  ├─ home/
│  │  └─ auth/                           ← Future feature
│  ├─ app.ts
│  └─ app.routes.ts
├─ assets/
├─ styles/                               ← Global CSS?
├─ environments/
└─ main.ts
```

### Benefits of New Structure

1. ✅ **Consistency:** All folders lowercasecase, singular/plural consistent
2. ✅ **Clarity:** Clear where things go
3. ✅ **Scalability:** Adding new features is straightforward
4. ✅ **Maintainability:** Each folder has one responsibility
5. ✅ **Testability:** Smaller, focused files easier to test
6. ✅ **Team speed:** New developers onboard faster
7. ✅ **Phase 3 ready:** Component hierarchy clear
8. ✅ **Future-proof:** Story Dependencies will fit naturally

---

## PART 11: IMPLEMENTATION TIMELINE

### Combined with Phase 3 Modularization

```
PHASE 3A: Component Refactoring (3 hours)
├─ Create Phase 3 subcomponents (board-header, team-bar, etc.)
└─ Update board.component imports

PHASE 3B: Folder Restructuring (4 hours) 
├─ Create new folder structure
├─ Move files to new locations
├─ Update all import paths
└─ Update app.routes.ts

PHASE 3C: Service Refactoring (3 hours)
├─ Split board-api.service into individual services
├─ Extract team/feature/story logic from board.service
└─ Create facade if needed

PHASE 3D: Final Polish (1 hour)
├─ Extract calculations to utils
├─ Clean up DTOs
├─ Final build verification

TOTAL: 11 hours (can be compressed to 1-2 days)
```

**Recommendation:** Do Phase 3A first (component modularization), THEN Phase 3B+ (folder restructuring).
This way:
- Board component gets split first (easier to move after)
- New subcomponents follow clean structure from day 1
- Services reorganization after components settled

---

## PART 12: ACCEPTANCE CRITERIA

### Folder Structure

- ✓ All folders lowercase or PascalCase (consistent)
- ✓ Models/ folder deleted (unused)
- ✓ Services/ folder deleted (merged into core/services)
- ✓ Components/ folder restructured into features/
- ✓ New paths: features/*, core/*, shared/* only

### Services

- ✓ One API service per file (not 5 services in 1 file)
- ✓ Domain services organized by feature
- ✓ No service with 500+ LOC
- ✓ Each service has single responsibility

### Components

- ✓ Page components in features/*/components/
- ✓ Shared components in shared/components/
- ✓ Clear hierarchy (no mixing page + shared)
- ✓ Consistent naming (*.component.ts)

### Types & Models

- ✓ DTOs in shared/models/
- ✓ Types in shared/types/
- ✓ Feature models in features/*/models/
- ✓ Clear separation: DTO vs Type vs Model

### Imports

- ✓ index.ts files for clean public APIs
- ✓ No deep relative imports (use index.ts)
- ✓ All imports follow new structure
- ✓ 0 compilation errors

### Build

- ✓ npm run build: 0 errors
- ✓ Same bundle size (or smaller)
- ✓ No regressions in functionality
- ✓ Dev server starts cleanly

---

## CONCLUSION

The current structure is confusing because:
1. **Mixed naming conventions** (Components/ vs core/ vs features/)
2. **Duplicate models** (Models/ unused, DTOs in shared/)
3. **Service chaos** (Services/ + core/services/ + features/board/services/)
4. **Monolith services** (563 LOC board.service, 5 APIs in one file)
5. **Components mixed** (page + shared in same folder)

**Proposed solution:**
- Clean, consistent structure
- Aligns with Angular best practices
- Ready for Phase 3 modularization
- Scalable for future features
- Clear mental model for team

**Recommendation:** Implement alongside Phase 3 component modularization for maximum impact.

