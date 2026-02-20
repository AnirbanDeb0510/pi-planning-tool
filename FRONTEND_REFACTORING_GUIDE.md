# Frontend Structure Refactoring - Implementation Guide

**Scope:** Folder restructuring + Service reorganization + Phase 3 component modularization  
**Timeline:** 8-10 hours (1-2 days)  
**Complexity:** MEDIUM (many file moves + import updates)

---

## STEP 1: Understand Current vs Target Structure (20 min)

### Current State
```
app/
├─ Components/          ← Page + shared components mixed
├─ core/               ← Infrastructure partially here
├─ features/board/     ← Services only, components missing
├─ Models/             ← UNUSED
├─ Services/           ← Isolated user.service
└─ shared/             ← DTOs only
```

### Target State
```
app/
├─ core/               ← All infrastructure
├─ shared/             ← DTOs + types + reusable components
├─ features/
│  ├─ board/
│  │  ├─ components/   ← Page + subcomponents
│  │  ├─ services/     ← Individual domain services
│  │  ├─ models/       ← Feature-specific models
│  │  ├─ types/        ← Feature types
│  │  ├─ constants/
│  │  └─ guards/
│  └─ home/
└─ app.ts, app.routes.ts
```

---

## STEP 2: Create Target Folder Structure (30 min)

### 2.1 Create Core Infrastructure Folders

```bash
mkdir -p src/app/core/services
mkdir -p src/app/core/constants
mkdir -p src/app/core/guards
mkdir -p src/app/core/interceptors
mkdir -p src/app/core/models
```

### 2.2 Create Shared Layer Folders

```bash
mkdir -p src/app/shared/components/story-card
mkdir -p src/app/shared/components/modals
mkdir -p src/app/shared/components/buttons
mkdir -p src/app/shared/types
mkdir -p src/app/shared/pipes
mkdir -p src/app/shared/directives
mkdir -p src/app/shared/utils
mkdir -p src/app/shared/animations
```

### 2.3 Create Feature Folders

```bash
# Board feature structure
mkdir -p src/app/features/board/components/board
mkdir -p src/app/features/board/components/board/{board-header,team-bar,capacity-row,sprint-header,feature-row,board-modals}
mkdir -p src/app/features/board/components/board-list
mkdir -p src/app/features/board/components/create-board
mkdir -p src/app/features/board/services/board
mkdir -p src/app/features/board/services/feature
mkdir -p src/app/features/board/services/team
mkdir -p src/app/features/board/services/story
mkdir -p src/app/features/board/services/azure
mkdir -p src/app/features/board/services/calculations
mkdir -p src/app/features/board/models
mkdir -p src/app/features/board/types
mkdir -p src/app/features/board/constants
mkdir -p src/app/features/board/guards
mkdir -p src/app/features/board/adapters

# Home feature structure
mkdir -p src/app/features/home/components/home
mkdir -p src/app/features/home/services
mkdir -p src/app/features/home/types
```

### 2.4 Verify Folder Structure

```bash
find src/app/core -type d | sort
find src/app/shared -type d | sort
find src/app/features -type d | sort
```

---

## STEP 3: Create Placeholder Files (30 min)

### 3.1 Create Service Facade Shells

**File: src/app/features/board/services/board/board.facade.ts**
```typescript
import { Injectable, inject } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class BoardFacade {
  // This will coordinate all domain services
  // Implemented after other services created
}
```

### 3.2 Create API Service Shells

For each domain, create:
- `src/app/features/board/services/board/board.api.ts`
- `src/app/features/board/services/feature/feature.api.ts`
- `src/app/features/board/services/team/team.api.ts`
- `src/app/features/board/services/story/story.api.ts`
- `src/app/features/board/services/azure/azure.api.ts`

```typescript
import { Injectable, inject } from '@angular/core';
import { HttpClientService } from '../../../../../core/services/http-client.service';

@Injectable({ providedIn: 'root' })
export class BoardApiService {
  private http = inject(HttpClientService);
  
  // To be implemented
}
```

### 3.3 Create Service Shells

For each domain:
- `src/app/features/board/services/board/board.service.ts`
- `src/app/features/board/services/feature/feature.service.ts`
- `src/app/features/board/services/team/team.service.ts`
- `src/app/features/board/services/story/story.service.ts`
- `src/app/features/board/services/azure/azure.service.ts`

```typescript
import { Injectable, signal, inject } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class BoardService {
  // To be implemented
}
```

### 3.4 Create index.ts Files

**src/app/features/board/services/index.ts**
```typescript
export * from './board/board.service';
export * from './board/board.api';
export * from './feature/feature.service';
export * from './feature/feature.api';
export * from './team/team.service';
export * from './team/team.api';
export * from './story/story.service';
export * from './story/story.api';
export * from './azure/azure.service';
export * from './azure/azure.api';
```

---

## STEP 4: Copy and Refactor board-api.service.ts (2 hours)

### 4.1 Split board-api.service.ts

**From:** `src/app/features/board/services/board-api.service.ts` (223 lines)

**Extract BoardApiService to:** `src/app/features/board/services/board/board.api.ts`
```typescript
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpClientService } from '../../../../../core/services/http-client.service';
import { BOARD_API } from '../../../../../core/constants/api-endpoints.constants';
import { BoardResponseDto } from '../../../../../shared/models/board.dto';

@Injectable({ providedIn: 'root' })
export class BoardApiService {
  private http = inject(HttpClientService);

  getBoard(id: number): Observable<BoardResponseDto> {
    return this.http.get<BoardResponseDto>(BOARD_API.GET_BOARD(id));
  }

  createBoard(dto: any): Observable<any> {
    // ... copied from original
  }

  // ... etc - copy ONLY BoardApiService methods
}
```

**Extract FeatureApiService to:** `src/app/features/board/services/feature/feature.api.ts`
```typescript
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpClientService } from '../../../../../core/services/http-client.service';
import { FEATURE_API } from '../../../../../core/constants/api-endpoints.constants';
import { FeatureResponseDto } from '../../../../../shared/models/board.dto';

@Injectable({ providedIn: 'root' })
export class FeatureApiService {
  private http = inject(HttpClientService);

  importFeature(boardId: number, featureDto: any): Observable<FeatureResponseDto> {
    return this.http.post<FeatureResponseDto>(FEATURE_API.IMPORT(boardId), featureDto);
  }

  // ... etc - copy ONLY FeatureApiService methods
}
```

**Repeat for:**
- `TeamApiService` → `src/app/features/board/services/team/team.api.ts`
- `StoryApiService` → `src/app/features/board/services/story/story.api.ts`
- `AzureApiService` → `src/app/features/board/services/azure/azure.api.ts`

### 4.2 Verify Split is Complete

```bash
# Check each new file for correct content
wc -l src/app/features/board/services/*/[a-z]*.api.ts

# Rough breakdown:
# board-api.service.ts was 223 lines with 5 services
# board.api.ts should be ~50 lines
# feature.api.ts should be ~50 lines
# team.api.ts should be ~40 lines
# story.api.ts should be ~30 lines
# azure.api.ts should be ~20 lines
```

---

## STEP 5: Refactor board.service.ts (3 hours)

### 5.1 Analyze Current board.service.ts

**File:** `src/app/features/board/services/board.service.ts` (563 lines)

**Identify domains:**
```
Lines 1-100: State signals + board operations
Lines 100-165: Team operations (add/edit/delete/capacity)
Lines 165-250: Feature operations (import/refresh/delete/reorder)
Lines 250-300: Story operations (move)
Lines 300-350: PAT management
Lines 350-400: Finalization
Lines 400-563: Utilities
```

### 5.2 Keep Board Service (Refactored)

**File:** `src/app/features/board/services/board/board.service.ts`

Keep only:
- Board state signals (board, loading, error)
- Board operations (load, finalize, restore)
- PAT management (for now)
- Coordination with other services

```typescript
import { Injectable, signal, inject } from '@angular/core';
import { BoardResponseDto } from '../../../../../shared/models/board.dto';
import { BoardApiService } from './board.api';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class BoardService {
  private boardApi = inject(BoardApiService);

  // State signals
  private boardSignal = signal<BoardResponseDto | null>(null);
  private loadingSignal = signal<boolean>(false);
  private errorSignal = signal<string | null>(null);
  private patStorage = signal<{ pat: string; timestamp: number } | null>(null);

  // Public read-only
  public board = this.boardSignal.asReadonly();
  public loading = this.loadingSignal.asReadonly();
  public error = this.errorSignal.asReadonly();

  /**
   * Load board by ID
   */
  public loadBoard(id: number): void {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    this.boardApi.getBoard(id).subscribe({
      next: (board: BoardResponseDto) => {
        this.boardSignal.set(board);
        this.loadingSignal.set(false);
      },
      error: (err) => {
        this.errorSignal.set(err.message);
        this.loadingSignal.set(false);
      }
    });
  }

  /**
   * Get current board
   */
  public getBoard(): BoardResponseDto | null {
    return this.boardSignal();
  }

  // PAT mana management methods
  public storePat(pat: string): void { /* ... */ }
  public getStoredPat(): string | null { /* ... */ }
  public clearPat(): void { /* ... */ }

  // Finalization methods
  public async finalizeBoard(id: number): Promise<BoardResponseDto | null> { /* ... */ }
  public async restoreBoard(id: number): Promise<BoardResponseDto | null> { /* ... */ }
  public async getFinalizationWarnings(id: number): Promise<string[]> { /* ... */ }

  // Utility methods used by all services
  public clearError(): void { /* ... */ }
  public toggleDevTestToggle(): void { /* ... */ }
}
```

### 5.3 Create Team Service

**File:** `src/app/features/board/services/team/team.service.ts`

Extract from current board.service:
```typescript
import { Injectable, inject } from '@angular/core';
import { BoardService } from '../board/board.service';
import { TeamApiService } from './team.api';

@Injectable({ providedIn: 'root' })
export class TeamService {
  private boardService = inject(BoardService);
  private teamApi = inject(TeamApiService);

  public addTeamMember(name: string, role: 'dev' | 'test', devTestEnabled: boolean): void {
    // Extract this method from board.service
    // Call teamApi.addTeamMember()
    // Update boardService.board signal
  }

  public updateTeamMember(memberId: number, name: string, role: 'dev' | 'test', devTestEnabled: boolean): void {
    // Extract from board.service
  }

  public removeTeamMember(memberId: number): void {
    // Extract from board.service
  }

  public updateTeamMemberCapacity(memberId: number, sprintId: number, dev: number, test: number): void {
    // Extract from board.service
  }

  public getTeamMembers() {
    // Helper method
  }
}
```

### 5.4 Create Feature Service

**File:** `src/app/features/board/services/feature/feature.service.ts`

```typescript
import { Injectable, inject } from '@angular/core';
import { BoardService } from '../board/board.service';
import { FeatureApiService } from './feature.api';

@Injectable({ providedIn: 'root' })
export class FeatureService {
  private boardService = inject(BoardService);
  private featureApi = inject(FeatureApiService);

  public async importFeature(
    boardId: number,
    organization: string,
    project: string,
    featureId: string,
    pat: string
  ): Promise<void> {
    // Extract from board.service
  }

  public async refreshFeature(
    boardId: number,
    featureId: number,
    organization: string,
    project: string,
    pat: string
  ): Promise<void> {
    // Extract from board.service
  }

  public async deleteFeature(boardId: number, featureId: number): Promise<void> {
    // Extract from board.service
  }

  public async reorderFeatures(boardId: number, updates: any[]): Promise<void> {
    // Extract from board.service
  }
}
```

### 5.5 Create Story Service

**File:** `src/app/features/board/services/story/story.service.ts`

```typescript
import { Injectable, inject } from '@angular/core';
import { BoardService } from '../board/board.service';
import { StoryApiService } from './story.api';

@Injectable({ providedIn: 'root' })
export class StoryService {
  private boardService = inject(BoardService);
  private storyApi = inject(StoryApiService);

  public moveStory(storyId: number, fromSprintId: number, toSprintId: number): void {
    // Extract from board.service
  }
}
```

### 5.6 Create Azure Service

**File:** `src/app/features/board/services/azure/azure.service.ts`

```typescript
import { Injectable, inject } from '@angular/core';
import { AzureApiService } from './azure.api';

@Injectable({ providedIn: 'root' })
export class AzureService {
  private azureApi = inject(AzureApiService);

  public async validatePatForBoard(
    organization: string,
    project: string,
    sampleFeatureAzureId: string,
    pat: string
  ): Promise<boolean> {
    // Extract from board.service
  }

  public async getBoardPreview(boardId: number): Promise<any> {
    // Extract from board.service
  }
}
```

---

## STEP 6: Create Calculations Utilities (1 hour)

### 6.1 Extract Sprint Calculations

**File:** `src/app/features/board/services/calculations/sprint.calculations.ts`

```typescript
import { FeatureResponseDto, UserStoryDto } from '../../../../../shared/models/board.dto';

export function getStoryTotalPoints(story: UserStoryDto): number {
  const hasDevOrTest = (story.devStoryPoints ?? 0) + (story.testStoryPoints ?? 0);
  const dev = story.devStoryPoints ?? 0;
  const test = story.testStoryPoints ?? 0;
  const base = story.storyPoints ?? 0;
  return hasDevOrTest > 0 ? dev + test : base;
}

export function calculateSprintTotals(
  features: FeatureResponseDto[],
  sprintId: number
): { dev: number; test: number; total: number } {
  let dev = 0, test = 0, total = 0;

  features.forEach(feature => {
    const sprintStories = feature.userStories.filter(s => s.sprintId === sprintId);
    sprintStories.forEach(story => {
      dev += story.devStoryPoints ?? 0;
      test += story.testStoryPoints ?? 0;
      total += getStoryTotalPoints(story);
    });
  });

  return { dev, test, total };
}

export function isSprintOverCapacity(
  features: FeatureResponseDto[],
  sprintId: number,
  type: 'dev' | 'test' | 'total',
  capacity: { dev: number; test: number }
): boolean {
  const load = calculateSprintTotals(features, sprintId);
  if (type === 'dev') return load.dev > capacity.dev;
  if (type === 'test') return load.test > capacity.test;
  return load.total > capacity.dev + capacity.test;
}
```

### 6.2 Extract Feature Calculations

**File:** `src/app/features/board/services/calculations/feature.calculations.ts`

```typescript
import { FeatureResponseDto } from '../../../../../shared/models/board.dto';
import { getStoryTotalPoints } from './sprint.calculations';

export function calculateFeatureTotal(feature: FeatureResponseDto): number {
  return feature.userStories.reduce((sum, s) => sum + getStoryTotalPoints(s), 0);
}

export function calculateFeatureSprintTotals(
  feature: FeatureResponseDto,
  sprintId: number
): { dev: number; test: number; total: number } {
  const sprintStories = feature.userStories.filter(s => s.sprintId === sprintId);

  let dev = 0, test = 0, total = 0;
  sprintStories.forEach(story => {
    dev += story.devStoryPoints ?? 0;
    test += story.testStoryPoints ?? 0;
    total += getStoryTotalPoints(story);
  });

  return { dev, test, total };
}
```

### 6.3 Extract Team Calculations

**File:** `src/app/features/board/services/calculations/team.calculations.ts`

```typescript
import { TeamMemberResponseDto, BoardResponseDto } from '../../../../../shared/models/board.dto';

export function calculateSprintCapacityTotals(
  teamMembers: TeamMemberResponseDto[],
  sprintId: number
): { dev: number; test: number; total: number } {
  let dev = 0, test = 0;

  teamMembers.forEach(member => {
    const entry = member.sprintCapacities.find(cap => cap.sprintId === sprintId);
    if (entry) {
      dev += entry.capacityDev ?? 0;
      test += entry.capacityTest ?? 0;
    }
  });

  return { dev, test, total: dev + test };
}
```

### 6.4 Create Calculations Index

**File:** `src/app/features/board/services/calculations/index.ts`

```typescript
export * from './sprint.calculations';
export * from './feature.calculations';
export * from './team.calculations';
```

---

## STEP 7: Move Components (2 hours)

### 7.1 Move Board Component

```bash
# Copy entire board folder with all Phase 3 subcomponents
cp -r src/app/Components/board/* src/app/features/board/components/board/

# Verify all files copied:
# ├─ board.ts
# ├─ board.html
# ├─ board.css
# ├─ board-header/
# ├─ team-bar/
# ├─ capacity-row/
# ├─ sprint-header/
# ├─ feature-row/
# └─ board-modals/
```

### 7.2 Move Other Page Components

```bash
cp -r src/app/Components/board-list/* src/app/features/board/components/board-list/
cp -r src/app/Components/create-board/* src/app/features/board/components/create-board/
cp -r src/app/Components/home/* src/app/features/home/components/home/
```

### 7.3 Move Story Card to Shared

```bash
cp -r src/app/Components/story-card/* src/app/shared/components/story-card/
```

### 7.4 Move UserService to Core

```bash
cp src/app/Services/user.service.ts src/app/core/services/user.service.ts
```

---

## STEP 8: Update All Import Paths (2 hours)

### 8.1 Update app.routes.ts

**BEFORE:**
```typescript
import { Board } from './Components/board/board';
import { HomeComponent } from './Components/home/home.component';
import { CreateBoardComponent } from './Components/create-board/create-board.component';
import { BoardListComponent } from './Components/board-list/board-list.component';
import { EnterYourName } from './Components/enter-your-name/enter-your-name';
```

**AFTER:**
```typescript
import { BoardComponent } from './features/board/components/board/board.component';
import { HomeComponent } from './features/home/components/home/home.component';
import { CreateBoardComponent } from './features/board/components/create-board/create-board.component';
import { BoardListComponent } from './features/board/components/board-list/board-list.component';
import { EnterYourName } from './features/board/components/enter-your-name/enter-your-name';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'boards', component: BoardListComponent },
  { path: 'boards/new', component: CreateBoardComponent },
  { path: 'boards/:id', component: BoardComponent },
  { path: 'name', component: EnterYourName },
  { path: 'board', redirectTo: 'boards/1', pathMatch: 'full' },
  { path: '**', redirectTo: '' }
];
```

### 8.2 Update app.ts

**BEFORE:**
```typescript
import { BoardService } from './features/board/services/board.service';
import { UserService } from './Services/user.service';
```

**AFTER:**
```typescript
import { BoardService } from './features/board/services/board/board.service';
import { UserService } from './core/services/user.service';
```

### 8.3 Update board.component.ts

**BEFORE:**
```typescript
import { UserService } from '../../Services/user.service';
import { BoardService } from '../../features/board/services/board.service';
import {
  SprintDto,
  FeatureResponseDto,
  UserStoryDto,
  TeamMemberResponseDto,
} from '../../shared/models/board.dto';
```

**AFTER:**
```typescript
import { UserService } from '../../../core/services/user.service';
import { 
  BoardService, 
  FeatureService, 
  TeamService, 
  StoryService 
} from '../../../features/board/services';
import {
  SprintDto,
  FeatureResponseDto,
  UserStoryDto,
  TeamMemberResponseDto,
} from '../../../shared/models/board.dto';
```

### 8.4 Update Subcomponent Imports

**In team-bar.component.ts:**
```typescript
import { Board } from '../../board.component';
```

**In feature-row.component.ts:**
```typescript
import { Board } from '../../board.component';
```

### 8.5 Update Story Card Imports

**In board.component.ts:**
```typescript
// BEFORE:
import { StoryCard } from '../story-card/story-card';

// AFTER:
import { StoryCard } from '../../../../shared/components/story-card/story-card';
```

### 8.6 Use Grep to Find All Imports to Update

```bash
grep -r "from.*Components/" src/app --include="*.ts" | head -20
grep -r "from.*Services/" src/app --include="*.ts" | head -20
grep -r "from.*Models/" src/app --include="*.ts" | head -20
```

Then replace each occurrence.

---

## STEP 9: Update Component Names (1 hour)

### 9.1 Rename Component Files

```bash
# Rename board.ts to board.component.ts
mv src/app/features/board/components/board/board.ts \
   src/app/features/board/components/board/board.component.ts

# Update @Component selector consistency
# In board.component.ts: selector: 'app-board' ✓ (already correct)
```

### 9.2 Verify All Files

```bash
find src/app/features -name "*.component.ts" | wc -l
# Should have consistent naming across all components
```

---

## STEP 10: Delete Old Folders (30 min)

### 10.1 Verify Nothing Left to Copy

```bash
# Double-check nothing is in old locations
ls -la src/app/Components/
ls -la src/app/Services/
ls -la src/app/Models/
```

### 10.2 Delete Old Folders

```bash
rm -rf src/app/Components/
rm -rf src/app/Services/
rm -rf src/app/Models/
```

### 10.3 Rename old board-api.service.ts (Archive)

```bash
# Keep copy for reference during refactoring
mv src/app/features/board/services/board-api.service.ts \
   src/app/features/board/services/board-api.service.ts.archived
```

---

## STEP 11: Update Shared Models (30 min)

### 11.1 Rename DTOs

```bash
# Rename to clarify purpose
mv src/app/shared/models/board.dto.ts \
   src/app/shared/models/api.dto.ts

mv src/app/shared/models/board-api.dto.ts \
   src/app/shared/models/payload.dto.ts
```

### 11.2 Update Imports of Renamed DTOs

```bash
grep -r "from.*board.dto" src/app --include="*.ts"
# Replace all with:
# from '../models/api.dto'

grep -r "from.*board-api.dto" src/app --include="*.ts"
# Replace all with:
# from '../models/payload.dto'
```

---

## STEP 12: Build and Verify (1 hour)

### 12.1 Build Frontend

```bash
cd frontend/pi-planning-ui
npm run build 2>&1 | tail -20
```

**Expected:**
```
✓ Application bundle generation complete. [X seconds]
✓ 0 errors
```

### 12.2 Check for Compilation Errors

If errors:
```bash
npm run build 2>&1 | grep -A 3 "error"
```

Fix import paths systematically.

### 12.3 Type Checking (Optional)

```bash
npm run lint
# Check for any linter issues
```

### 12.4 Run Dev Server

```bash
npm start
# Or ng serve

# Verify no console errors
# Verify page loads
# Verify board loads
```

### 12.5 Manual Testing

- [ ] Navigate to home page
- [ ] Navigate to boards list
- [ ] Create new board
- [ ] Open board (verify PAT modal, etc.)
- [ ] Add team member
- [ ] Edit capacity
- [ ] Drag story between sprints
- [ ] Reorder features
- [ ] Finalize board
- [ ] Restore board
- [ ] Toggle Dev/Test
- [ ] Toggle theme (dark/light)

---

## STEP 13: Commit Progress (1 hour)

### 13.1 Create Git Commits

```bash
# Stage folder restructuring
git add src/app/features/board/components/
git add src/app/features/board/services/
git add src/app/features/home/
git add src/app/shared/components/
git add src/app/core/services/user.service.ts
git commit -m "refactor: reorganize folder structure - part 1: move files"

# Stage import updates
git add src/app/app.routes.ts
git add src/app/app.ts
git add src/app/features/board/components/board/board.component.ts
git commit -m "refactor: update import paths after folder restructuring"

# Stage DTO renames
git add src/app/shared/models/
git commit -m "refactor: rename DTOs for clarity (api.dto, payload.dto)"

# Stage old folder deletion
git add -u src/app/
git commit -m "refactor: delete old folder structure (Components/, Services/, Models/)"
```

### 13.2 Verify Build Passes

```bash
npm run build 2>&1 | tail -5
# Should show: ✓ Application bundle generation complete [X seconds]
```

---

## STEP 14: Integration with Phase 3 (Already Done)

### Current State After Steps 1-13

```
features/board/components/board/
├─ board.component.ts (main - 928 lines currently)
├─ board.component.html
├─ board.component.css
├─ board-header/     ← From Phase 3
├─ team-bar/         ← From Phase 3
├─ capacity-row/     ← From Phase 3
├─ sprint-header/    ← From Phase 3
├─ feature-row/      ← From Phase 3
└─ board-modals/     ← From Phase 3
```

### Next Steps for Phase 3

If not yet done:
1. Follow Phase 3 modularization checklist
2. Component imports updated to use new structure
3. All services use new service layer structure

---

## STEP 15: Final Verification Checklist

### Folder Structure
- ✓ No `Components/` folder in app/
- ✓ No `Services/` folder in app/
- ✓ No `Models/` folder in app/
- ✓ All components in `features/*/components/`
- ✓ All shared components in `shared/components/`
- ✓ All services in `features/*/services/` or `core/services/`

### Services
- ✓ No single service > 600 LOC
- ✓ Each API service in separate file
- ✓ One responsibility per service
- ✓ index.ts files for clean imports

### Components
- ✓ All file names use `*.component.ts` pattern
- ✓ Page components in features/
- ✓ Shared components in shared/
- ✓ Clear hierarchy

### Imports
- ✓ All paths updated
- ✓ No broken imports
- ✓ No deep relative paths (using index.ts)
- ✓ Build: 0 errors

### Testing
- ✓ All pages load
- ✓ All operations work
- ✓ No console errors
- ✓ Dark/light theme works
- ✓ Dev server clean start

---

## ROLLBACK PLAN

If significant issues arise:

```bash
# Check git log to find last good commit
git log --oneline | head -20

# Revert to previous checkpoint
git reset --hard <commit-hash>

# Or revert specific file
git checkout HEAD~1 -- src/app/app.routes.ts
```

---

## SUCCESS METRICS

✅ **Folder Structure:**
- No PascalCase folders (except features/board for module)
- Clear separation: core, shared, features
- Predictable locations for new files

✅ **Services:**
- No monolith services (all < 500 LOC)
- Clear domain separation
- Each file has one responsibility

✅ **Components:**
- Consistent naming conventions
- Clear page vs shared distinction
- Ready for Phase 3 modularization

✅ **Build:**
- npm run build: 0 errors
- No import errors
- No type errors

✅ **Functionality:**
- All features work identically
- No regressions
- Performance same or better

✅ **Team:**
- Faster onboarding for new developers
- Clear mental model of structure
- Easy to add new features

