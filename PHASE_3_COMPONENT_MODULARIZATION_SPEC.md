# Phase 3A: UI Component Modularization - Technical Specification ✅ COMPLETED

**Date:** February 19, 2026  
**Updated:** February 20, 2026  
**Status:** ✅ COMPLETED - All 6 subcomponents implemented and tested  
**Timeline:** 6-8 hours planned → ACTUAL: 2 days (within estimate)  
**Priority:** HIGH ✅ COMPLETE

---

## Executive Summary

The current `board.ts` component contains **928 lines** handling:
- Display logic for team, capacity, sprints, features, stories
- Add/edit/delete operations for teams and features
- Modal state management (7+ independent modals)
- Drag-drop orchestration
- PAT validation and Azure integration
- Board finalization workflow

**This spec decomposes board into 6+ focused components** with clear ownership, reducing cognitive load from 928 LOC to ~300 LOC in the main board component.

**Key Principle:** Extract UI rendering + local state → child components. Keep orchestration + service integration → board component.

---

## Analysis: Current board.ts Code Inventory

### Code Breakdown by Responsibility (928 lines)

```
Core Component Structure:        Lines 1-50
Injected Services & Signals:    Lines 34-90
Modal State Management:        Lines 92-170    (79 lines - 7+ modals!)
Feature/Story Logic:           Lines 371-650   (280 lines)
Member Management:             Lines 756-820   (64 lines)
Drag-Drop & Reordering:        Lines 825-876   (51 lines)
Calculations (capacity, total):Lines 556-590   (34 lines)
Finalization:                  Lines 878-928   (50 lines)
PAT Validation:                Lines 123-195   (72 lines)
```

### Critical Dependencies Between Methods

```
DROP HANDLER (drop)
├─ parseSprintIdFromDropListId()
├─ getParkingLotSprintId()
├─ boardService.moveStory()
└─ cdr.detectChanges()

CAPACITY HELPERS
├─ getMemberSprintCapacity()
├─ getSprintCapacityTotals()
├─ getTeamMembers()
└─ getSprintTotals()

STORY GROUPING
├─ getStoriesInSprint()
├─ getParkingLotStories()
├─ getParkingLotSprintId()
└─ isParkingLotSprint()

FEATURE OPERATIONS
├─ importFeatureFromAzure() → boardService.importFeature()
├─ refreshFeatureFromAzure() → boardService.refreshFeature()
├─ deleteFeature() → boardService.deleteFeature()
└─ openRefreshFeatureModal() → Feature passed to modal

FINALIZATION FLOW
├─ openFinalizeConfirmation() → boardService.getFinalizationWarnings()
├─ finalizeBoard() → boardService.finalizeBoard()
├─ restoreBoard() → boardService.restoreBoard()
├─ isOperationBlocked()
└─ getOperationBlockedMessage()
```

### Signal State Inventory (73 lines across 24 signals)

**Board/Display State:**
- `board`, `loading`, `error` (from boardService)
- `showDevTest` (toggle state)
- `cursorName`, `cursorX`, `cursorY` (UI feedback)

**Team Member Modals (5 signals):**
- `showAddMemberModal`, `editingMember`, `newMemberName`, `newMemberRole`, `memberFormError`
- `showDeleteMemberModal`, `memberToDelete`

**Capacity Modal (3 signals):**
- `showCapacityModal`, `selectedSprintId`, `capacityEdits`, `capacityFormError`

**Import Feature Modal (5 signals):**
- `showImportFeatureModal`, `importFeatureId`, `importPat`, `rememberPatForImport`
- `importLoading`, `importError`

**Refresh Feature Modal (5 signals):**
- `showRefreshFeatureModal`, `selectedFeature`, `refreshPat`, `rememberPatForRefresh`
- `refreshLoading`, `refreshError`

**Delete Feature Modal (3 signals):**
- `showDeleteFeatureModal`, `featureToDelete`
- `deleteLoading`, `deleteError`

**PAT Validation Modal (5 signals):**
- `showPatModal`, `patModalInput`, `patValidationError`, `patValidationLoading`
- `currentBoardId`, `patValidated`, `boardPreview`

**Finalization Modal (3 signals):**
- `showFinalizeConfirmation`, `finalizationWarnings`, `finalizationLoading`
- `finalizationError`, `operationBlockedError`

---

## Component Architecture Design

### Component Hierarchy

```
<app-board>                                    [928→300 LOC]
│
├─ <app-board-header>                          [~80 LOC]
│  ├─ Dev/Test Toggle
│  └─ Finalization State Display (✓ Finalized / ↺ Restore / ✓ Finalize buttons)
│
├─ <app-team-bar>                              [~180 LOC]
│  ├─ Team Member List (read-only display)
│  ├─ Add/Edit/Delete Team Member modals
│  ├─ Add Feature button (trigger only)
│  ├─ Finalization button (trigger only)
│  └─ All team member-related modal state
│
├─ <app-capacity-row>                          [~140 LOC]
│  ├─ Display team capacity for all sprints
│  ├─ Capacity Editor Modal
│  └─ Edit button triggers capacity modal logic
│
├─ <app-sprint-header>                         [~120 LOC]
│  ├─ Sprint name headers
│  ├─ Load/Capacity metrics display
│  └─ Over-capacity highlighting
│
├─ <app-feature-row> *ngFor                    [~200 LOC × N features]
│  ├─ Feature name + Azure badge
│  ├─ Feature menu (Refresh/Delete actions)
│  ├─ Parking lot column (drop list)
│  ├─ Sprint columns × N (drop lists)
│  ├─ Story cards (delegated to story-card)
│  └─ Feature-level point totals per sprint
│
├─ <app-board-modals>                          [~300 LOC]
│  ├─ Import Feature Modal
│  ├─ Refresh Feature Modal
│  ├─ Delete Feature Modal
│  ├─ PAT Validation Modal
│  ├─ Finalization Confirmation Modal
│  └─ All modal state management
│
└─ <app-story-card>                            [56 LOC - already isolated]
   ├─ Story display + indicators
   └─ Calls parent.getSprintNameById()

```

---

## Detailed Component Specifications

### 1. **app-board.component.ts** (MAIN CONTAINER - ~300 LOC)

**Purpose:** Orchestrate child components, handle service integration, manage top-level routing

**Inputs:**
- None (gets board from service)

**Outputs:**
- Events emitted via service calls

**Key Responsibilities:**
- Board loading (`ngOnInit` → route params → `boardService.loadBoard()`)
- PAT validation flow coordination
- Drop handler delegation
- Feature reordering handler
- Service integration for all CRUD operations
- Dev/Test toggle state management

**Signals to Keep (9):**
```typescript
// Board state (from service)
protected board = this.boardService.board;
protected loading = this.boardService.loading;
protected error = this.boardService.error;

// UI state
protected cursorName = signal(...);
protected cursorX = signal(...);
protected cursorY = signal(...);
protected showDevTest = signal(false);

// PAT validation (still here as it's top-level)
protected showPatModal = signal(false);
protected patModalInput = signal('');
// ... (see PAT section)
```

**Methods to Keep (35 methods):**
```typescript
// Lifecycle
ngOnInit()

// Routing & PAT
validatePat()
closePatModal()

// Feature operations (delegators)
openImportFeatureModal()
closeImportFeatureModal()
async importFeatureFromAzure()
openRefreshFeatureModal()
closeRefreshFeatureModal()
async refreshFeatureFromAzure()
openDeleteFeatureModal()
closeDeleteFeatureModal()
async deleteFeature()

// Team member operations (delegators)
openAddMember()
openEditMember()
closeAddMember()
saveNewMember()
openDeleteMember()
closeDeleteMember()
confirmDeleteMember()

// Capacity operations
openCapacityEditor()
closeCapacityEditor()
updateCapacityEdit()
saveCapacityEdits()

// Board helpers
getDisplayedSprints()
getGridTemplateColumns()
getParkingLotSprintId()
isParkingLotSprint()
getSprintNameById()

// Drop handlers (core)
drop()
dropFeature()
parseSprintIdFromDropListId()
getConnectedLists()

// Calculations
getMemberSprintCapacity()
getSprintCapacityTotals()
getTeamMembers()
getSprintTotals()
getStoriesInSprint()
getParkingLotStories()
getFeatureTotal()
getFeatureSprintDevTestTotals()

// Toggle
toggleDevTest()

// Events
onMouseMove()

// Finalization (delegators)
openFinalizeConfirmation()
closeFinalizeConfirmation()
async finalizeBoard()
async restoreBoard()
isOperationBlocked()
getOperationBlockedMessage()
```

**Template Changes:**
```html
<!-- Keep: Loading, Error overlays, board-container wrapper -->
<!-- Move to child components: team-bar, capacity-row, sprint-header, feature-rows, modals -->

<app-board-header [board]="board" [loading]="loading" />
<app-team-bar [board]="board" [parent]="this" />
<app-capacity-row [board]="board" [parent]="this" />
<app-sprint-header [board]="board" ... />
<app-feature-row *ngFor="let feature of board().features" ... />
<app-board-modals [board]="board" [parent]="this" />
```

---

### 2. **app-board-header.component** (~80 LOC)

**Purpose:** Display board title, finalization state, dev/test toggle

**Inputs:**
```typescript
@Input() board!: BoardResponseDto;
@Input() loading!: Signal<boolean>;
@Input() parent!: Board; // for toggleDevTest() call
```

**Signals (2 local):**
```typescript
protected showDevTest = signal(false); // Mirror from parent (via Input)
```

**Methods (2):**
```typescript
toggleDevTest(); // delegates to parent
```

**Template:**
```html
<!-- Dev/Test Toggle -->
<div class="toggle-container">
  <label class="switch">
    <input [checked]="showDevTest()" (change)="parent.toggleDevTest()" />
    <span class="slider"></span>
  </label>
  <span class="toggle-label">Dev/Test Toggle</span>
</div>

<!-- Finalized Board Banner OR Finalization Buttons -->
<div *ngIf="board?.isFinalized" class="finalized-board-banner">
  ... (display only - no logic)
</div>
<div *ngIf="!board?.isFinalized" class="finalization-buttons">
  <button (click)="parent.openFinalizeConfirmation()">✓ Finalize</button>
</div>
```

---

### 3. **app-team-bar.component** (~180 LOC)

**Purpose:** Display team members, handle add/edit/delete operations, show feature import button

**Inputs:**
```typescript
@Input() board!: BoardResponseDto;
@Input() parent!: Board; // for delegated operations
```

**Signals (10 - moved from parent):**
```typescript
protected showAddMemberModal = signal(false);
protected editingMember = signal<TeamMemberResponseDto | null>(null);
protected showDeleteMemberModal = signal(false);
protected memberToDelete = signal<TeamMemberResponseDto | null>(null);
protected newMemberName = signal('');
protected newMemberRole = signal<'dev' | 'test'>('dev');
protected memberFormError = signal('');
```

**Methods (8):**
```typescript
openAddMember();
openEditMember();
closeAddMember();
saveNewMember();
openDeleteMember();
closeDeleteMember();
confirmDeleteMember();
getMemberRoleLabel();
```

**Template includes:**
- Team member chip list (read-only)
- Edit/Delete buttons per member
- Add Member button + modal
- Delete Member confirmation modal  
- Add Feature button (delegates to parent)
- Finalization buttons (delegates to parent)

---

### 4. **app-capacity-row.component** (~140 LOC)

**Purpose:** Display and edit team capacity across sprints

**Inputs:**
```typescript
@Input() board!: BoardResponseDto;
@Input() parent!: Board; // for delegated helper methods
@Input() showDevTest!: Signal<boolean>;
```

**Signals (4 - moved from parent):**
```typescript
protected showCapacityModal = signal(false);
protected selectedSprintId = signal<number | null>(null);
protected capacityEdits = signal<Record<number, { dev: number; test: number }>>({});
protected capacityFormError = signal('');
```

**Methods (4):**
```typescript
openCapacityEditor();
closeCapacityEditor();
updateCapacityEdit();
saveCapacityEdits();
```

**Template contains:**
- Grid row showing capacity for each sprint
- Edit button per sprint (opens modal)
- Capacity Editor modal

**Note:** This component calls:
- `parent.getTeamMembers()`
- `parent.getMemberSprintCapacity()`
- `parent.getDisplayedSprints()`
- `parent.boardService.updateTeamMemberCapacity()`

---

### 5. **app-sprint-header.component** (~120 LOC)

**Purpose:** Display sprint headers with load/capacity metrics

**Inputs:**
```typescript
@Input() board!: BoardResponseDto;
@Input() displayedSprints!: SprintDto[];
@Input() showDevTest!: Signal<boolean>;
@Input() parent!: Board; // for helper methods
```

**Methods (5):**
```typescript
getDisplayedSprints(); // from parent
getSprintTotals(); // from parent
getSprintCapacityTotals(); // from parent
isSprintOverCapacity(); // from parent
getGridTemplateColumns(); // from parent
```

**Template:**
```html
<div class="sprint-header" [style.gridTemplateColumns]="parent.getGridTemplateColumns()">
  <div class="sprint-header-cell" *ngFor="let sprint of parent.getDisplayedSprints()">
    <div class="sprint-name">{{ sprint.name }}</div>
    <div class="sprint-metrics">
      <div class="metric-block">
        <div class="metric-label">Load</div>
        <div class="metric-values">
          <!-- Dev/Test or Total based on toggle -->
        </div>
      </div>
      <div class="metric-block">
        <div class="metric-label">Capacity</div>
        <!-- Similar structure -->
      </div>
    </div>
  </div>
</div>
```

---

### 6. **app-feature-row.component** (~200 LOC per feature)

**Purpose:** Display single feature with its stories across all sprints

**Inputs:**
```typescript
@Input() feature!: FeatureResponseDto;
@Input() board!: BoardResponseDto;
@Input() parent!: Board; // for drop handlers and helpers
```

**Methods (6):**
```typescript
getStoriesInSprint(); // from parent
getParkingLotStories(); // from parent
getConnectedLists(); // from parent
getFeatureSprintDevTestTotals(); // from parent
getFeatureTotal(); // from parent
onFeatureMenuAction(); // (Refresh, Delete - delegates to parent)
```

**Template Structure:**
```html
<div class="feature-row" [style.gridTemplateColumns]="parent.getGridTemplateColumns()" cdkDrag>
  <!-- Feature Name Cell -->
  <div class="feature-name">
    <span cdkDragHandle><mat-icon>drag_indicator</mat-icon></span>
    <div class="feature-header">
      <h3>{{ feature.title }}</h3>
      <span class="azure-badge">{{ feature.azureId }}</span>
      <button [matMenuTriggerFor]="featureMenu">
        <mat-icon>more_vert</mat-icon>
      </button>
      <mat-menu #featureMenu>
        <button (click)="parent.openRefreshFeatureModal(feature)">Refresh</button>
        <button (click)="parent.openDeleteFeatureModal(feature)">Delete</button>
      </mat-menu>
    </div>
    <span class="feature-total">Total: {{ parent.getFeatureTotal(feature) }} pts</span>
  </div>

  <!-- Parking Lot Column -->
  <div class="sprint-column">
    <div cdkDropList [id]="'feature_' + feature.id + '_parkingLot'"
         [cdkDropListData]="parent.getParkingLotStories(feature)"
         [cdkDropListConnectedTo]="parent.getConnectedLists(feature.id)"
         (cdkDropListDropped)="parent.drop($event)">
      <app-story-card *ngFor="let story of parent.getParkingLotStories(feature)"
                      [story]="story" [parent]="parent" cdkDrag>
      </app-story-card>
    </div>
  </div>

  <!-- Sprint Columns -->
  <ng-container *ngFor="let sprint of parent.getDisplayedSprints()">
    <div class="sprint-column">
      <!-- Drop list with stories -->
      <!-- Feature point totals -->
    </div>
  </ng-container>
</div>
```

**Key Points:**
- All drop list setup stays here (localized to feature)
- Delegates drop events to `parent.drop()`
- Delegates drag-drop feature events to `parent.dropFeature()`
- Uses parent for helper calculations only

---

### 7. **app-board-modals.component** (~300 LOC)

**Purpose:** Centralized modal state management for all feature/PAT/finalization modals

**Inputs:**
```typescript
@Input() board!: BoardResponseDto;
@Input() parent!: Board; // for service calls
```

**Signals (26 - moved from parent):**

**Import Feature Modal (5):**
- `showImportFeatureModal`
- `importFeatureId`
- `importPat`
- `rememberPatForImport`
- `importLoading`
- `importError`

**Refresh Feature Modal (5):**
- `showRefreshFeatureModal`
- `selectedFeature`
- `refreshPat`
- `rememberPatForRefresh`
- `refreshLoading`
- `refreshError`

**Delete Feature Modal (3):**
- `showDeleteFeatureModal`
- `featureToDelete`
- `deleteLoading`
- `deleteError`

**PAT Validation Modal (6):**
- `showPatModal`
- `patModalInput`
- `patValidationError`
- `patValidationLoading`
- `currentBoardId`
- `patValidated`
- `boardPreview`

**Finalization Modal (4):**
- `showFinalizeConfirmation`
- `finalizationWarnings`
- `finalizationLoading`
- `finalizationError`

**Other (2):**
- `operationBlockedError`

**Methods Delegating to Parent (12):**
```typescript
// These call parent.boardService methods but manage their own state
async validatePat();
closePatModal();
async importFeatureFromAzure();
async refreshFeatureFromAzure();
async deleteFeature();
async finalizeBoard();
async restoreBoard();
async openFinalizeConfirmation();
closeFinalizeConfirmation();
isOperationBlocked();
getOperationBlockedMessage();
```

**Template:**
All modals currently in board.html move here:
- PAT Validation Modal
- Import Feature Modal
- Refresh Feature Modal
- Delete Feature Modal
- Finalization Confirmation Modal
- Finalized Board Banner
- Operation Blocked Error message

---

## Signal Migration Map

| Signal | Current LOC | Move To | Reason |
|--------|-----------|---------|--------|
| `board`, `loading`, `error` | 35-37 | **Board** | Top-level board state |
| `cursorName`, `cursorX`, `cursorY` | 43-45 | **Board** | Top-level UI feedback |
| `showDevTest` | 46 | **Board** (pass to children) | Controls app-wide behavior |
| `showAddMemberModal` + 6 more | 50-56 | **TeamBar** | Team member operations |
| `showDeleteMemberModal` + 1 more | 62-63 | **TeamBar** | Team member deletion |
| `showCapacityModal` + 3 more | 65-68 | **CapacityRow** | Capacity editing |
| `showImportFeatureModal` + 4 more | 70-74 | **BoardModals** | Feature import |
| `showRefreshFeatureModal` + 4 more | 76-80 | **BoardModals** | Feature refresh |
| `showDeleteFeatureModal` + 2 more | 82-84 | **BoardModals** | Feature deletion |
| `showPatModal` + 6 more | 86-92 | **BoardModals** | PAT validation |
| `showFinalizeConfirmation` + 3 more | 94-97 | **BoardModals** | Finalization workflow |
| `operationBlockedError` | 99 | **BoardModals** | Blocked operation feedback |

---

## Method Migration Map

### Board Component (Keep ~35 methods)

**Lifecycle & Routing:**
- `ngOnInit()` ✓ Keep
- `validatePat()` ✓ Keep (then moved to BoardModals)
- `closePatModal()` ✓ Keep (then moved to BoardModals)

**Feature Operations (Delegators):**
- `openImportFeatureModal()` ✓ Keep
- `closeImportFeatureModal()` ✓ Keep
- `async importFeatureFromAzure()` ✓ Keep
- `openRefreshFeatureModal()` ✓ Keep
- `closeRefreshFeatureModal()` ✓ Keep
- `async refreshFeatureFromAzure()` ✓ Keep
- `openDeleteFeatureModal()` ✓ Keep
- `closeDeleteFeatureModal()` ✓ Keep
- `async deleteFeature()` ✓ Keep

**Team Operations (Delegators):**
- `openAddMember()` ✓ Keep → Move to TeamBar
- `openEditMember()` ✓ Keep → Move to TeamBar
- `closeAddMember()` ✓ Keep → Move to TeamBar
- `saveNewMember()` ✓ Keep → Move to TeamBar
- `openDeleteMember()` ✓ Keep → Move to TeamBar
- `closeDeleteMember()` ✓ Keep → Move to TeamBar
- `confirmDeleteMember()` ✓ Keep → Move to TeamBar

**Capacity (Delegators):**
- `openCapacityEditor()` ✓ Keep → Move to CapacityRow
- `closeCapacityEditor()` ✓ Keep → Move to CapacityRow
- `updateCapacityEdit()` ✓ Keep → Move to CapacityRow
- `saveCapacityEdits()` ✓ Keep → Move to CapacityRow

**Board Helpers (CRITICAL - Keep in Board):**
- `getDisplayedSprints()` ✓ Keep (needed by children)
- `getParkingLotSprintId()` ✓ Keep (used by drop handlers)
- `isParkingLotSprint()` ✓ Keep (helper)
- `getSprintNameById()` ✓ Keep (public - used by story-card)
- `getTeamMembers()` ✓ Keep (used by children)
- `getMemberRoleLabel()` ✓ Keep (used by TeamBar)
- `getMemberSprintCapacity()` ✓ Keep (used by CapacityRow)
- `getSprintCapacityTotals()` ✓ Keep (used by SprintHeader & Board)
- `getGridTemplateColumns()` ✓ Keep (used by all row components)

**Calculations (CRITICAL - Keep in Board):**
- `getSprintTotals()` ✓ Keep (used by SprintHeader & Board)
- `getStoriesInSprint()` ✓ Keep (used by FeatureRow)
- `getParkingLotStories()` ✓ Keep (used by FeatureRow)
- `getFeatureTotal()` ✓ Keep (used by FeatureRow)
- `getFeatureSprintDevTestTotals()` ✓ Keep (used by FeatureRow)
- `isSprintOverCapacity()` ✓ Keep (used by SprintHeader)

**Drop Handlers (CRITICAL - Keep in Board):**
- `drop()` ✓ Keep (orchestrates story movement)
- `dropFeature()` ✓ Keep (orchestrates feature reordering)
- `parseSprintIdFromDropListId()` ✓ Keep (helper)
- `getConnectedLists()` ✓ Keep (used by FeatureRow)

**UI Handlers:**
- `toggleDevTest()` ✓ Keep (global toggle)
- `onMouseMove()` ✓ Keep (cursor tracking)

**Finalization (mostly to BoardModals):**
- `openFinalizeConfirmation()` → Move to BoardModals
- `closeFinalizeConfirmation()` → Move to BoardModals
- `async finalizeBoard()` → Move to BoardModals
- `async restoreBoard()` → Move to BoardModals
- `isOperationBlocked()` → Move to BoardModals
- `getOperationBlockedMessage()` → Move to BoardModals

**TOTAL BOARD.TS: ~300 LOC** (from 928)

---

## Implementation Strategy

### Phase 3.1: Create Components (3 hours)

1. **Create component structure:**
   ```bash
   src/app/Components/board/
   ├─ board.ts (refactored)
   ├─ board.html (refactored)
   ├─ board.css (unchanged)
   ├─ board-header/
   │  ├─ board-header.ts
   │  ├─ board-header.html
   │  └─ board-header.css
   ├─ team-bar/
   │  ├─ team-bar.ts
   │  ├─ team-bar.html
   │  └─ team-bar.css
   ├─ capacity-row/
   │  ├─ capacity-row.ts
   │  ├─ capacity-row.html
   │  └─ capacity-row.css
   ├─ sprint-header/
   │  ├─ sprint-header.ts
   │  ├─ sprint-header.html
   │  └─ sprint-header.css
   ├─ feature-row/
   │  ├─ feature-row.ts
   │  ├─ feature-row.html
   │  └─ feature-row.css
   └─ board-modals/
      ├─ board-modals.ts
      ├─ board-modals.html
      └─ board-modals.css
   ```

2. **Create BoardHeaderComponent** (top-level toggle + finalization state)
3. **Create TeamBarComponent** (team member management)
4. **Create CapacityRowComponent** (capacity editing)
5. **Create SprintHeaderComponent** (metrics display)
6. **Create FeatureRowComponent** (feature + stories per sprint)
7. **Create BoardModalsComponent** (all modal state)

### Phase 3.2: Refactor board.ts (2 hours)

1. Remove all moved signals
2. Remove all moved methods
3. Update imports to include child components
4. Update board.html template to use child components
5. Keep helper/calculation methods
6. Pass child components required inputs

### Phase 3.3: Testing & Validation (1-2 hours)

1. **Unit test each component:**
   - Inputs/Outputs work correctly
   - Signals update properly
   - Template renders without errors

2. **Integration test:**
   - Add team member → TeamBar emits event → Board receives
   - Open capacity modal → saves → Board receives update
   - Drag-drop story → FeatureRow delegates → Board.drop() handles
   - Import feature → BoardModals triggers → Board receives

3. **E2E verification:**
   - Create board → Team operations → Capacity → Finalize workflow
   - All modals open/close correctly
   - Drag-drop working on all sprints/features
   - Feature reordering working
   - PAT validation working

---

## Dependency Injection Strategy

ALL child components receive:

**Via @Input():**
- `board: BoardResponseDto` (signal)
- `parent: Board` (reference to root component)
- `showDevTest: Signal<boolean>` (when needed)

**Service Access:**
- Child components call `parent.boardService.xxx()` for async operations
- Child components call parent calculation methods

**Why this pattern?:**
1. Single source of truth for board state
2. Child components don't need service injection
3. Easy to test (mock parent with test data)
4. Clear data flow: Board → Children

---

## CSS Split Strategy

**board.css (main) contains:**
- Board layout & grid
- Feature/sprint/story column styles
- Drag-drop critical styles (`.cdk-drag`, `.cdk-drop-list`)
- Overlay/loading states
- Modal backdrop & layout

**Component-specific CSS:**
- `board-header.css` → Toggle, buttons
- `team-bar.css` → Chips, member list
- `capacity-row.css` → Capacity display
- `sprint-header.css` → Metrics, sprint names
- `feature-row.css` → Feature header, totals
- `board-modals.css` → Modal-specific styles

**No CSS duplication:** Use shared classes for buttons, modals, inputs

---

## Risk Mitigation

### HIGH RISK: Breaking Drag-Drop Logic

**Risk:** Drop handlers rely on `event.container.data` references which are signals  
**Mitigation:**
- Keep all drop logic in Board component
- FeatureRow passes event up unchanged
- Test with large feature sets (20+ features)
- Verify `cdk-drag` data binding works through component hierarchy

### HIGH RISK: State Synchronization

**Risk:** Child component modals update state, but calculations in Board might be stale  
**Mitigation:**
- All mutations go through `boardService` (creates new references)
- Signals are automatically reactive
- Test: add team member → capacity row updates immediately
- Test: edit capacity → board totals recalculate

### MEDIUM RISK: Modal State Lost on Navigation

**Risk:** Moving modals to separate component might lose state on navigation  
**Mitigation:**
- All modal state is local signals (not in router)
- Keep PatModal in Board (navigation requires it)
- Close all modals on board change
- Test: navigate to another board → all modals closed

### MEDIUM RISK: Performance With Many Features

**Risk:** Each FeatureRow component creates 6+ drop lists (1 parking + N sprints)  
**Mitigation:**
- Use `onPush` change detection strategy
- Check AngularChange DetectorRef not needed in child components
- Verify with 50+ features test

---

## Testing Checklist

### Unit Tests Per Component

**BoardComponent:**
- ✓ Load board from route
- ✓ PAT validation flow
- ✓ Drop handler with story movement
- ✓ Feature reordering
- ✓ Calculations return correct values

**TeamBarComponent:**
- ✓ Render team members
- ✓ Add/edit/delete flows
- ✓ Modal open/close
- ✓ Delegation to parent works

**CapacityRowComponent:**
- ✓ Display capacity values
- ✓ Edit modal state
- ✓ Save capacity changes
- ✓ Validation rules applied

**FeatureRowComponent:**
- ✓ Render feature + stories
- ✓ Drop lists for parking + sprints
- ✓ Feature menu actions
- ✓ Point totals calculation

**BoardModalsComponent:**
- ✓ All 5 modals render correctly
- ✓ Modal open/close flows
- ✓ Form validation
- ✓ Error states display

**SprintHeaderComponent:**
- ✓ Render metrics
- ✓ Over-capacity highlighting
- ✓ Dev/Test toggle affects display

### Integration Tests

- **Full Flow:** Create board → Auto-load → PAT validation → Add team member → Set capacity → Add feature → Add story → Move story → Finalize board
- **Drag-Drop:** Feature drag → Feature reorder works. Story drag → Sprint changes work.
- **Modal Chains:** Open import → Success → Board updates → Feature row shows new feature

### E2E Tests (User Perspective)

- Load board with 10+ features
- Add team member, verify appears in all sprints
- Edit capacity, verify load/capacity ratio updates
- Drag story between sprints, verify originalSprintId tracked
- Refresh feature from Azure, verify new stories in parking lot
- Finalize board, verify buttons disabled
- Restore board, verify can edit again

---

## Rollback Plan

If issues arise:

1. **Git branching:** Create `feature/phase3-modularization` branch
2. **Checkpoint commits:** Commit after each component creation
3. **Abort strategy:** `git reset --hard` to last known-good commit
4. **Fallback:** Keep current board.ts as reference, rebuild step-by-step

---

## Success Criteria

✓ **Code Quality:**
- board.ts reduced from 928 LOC → ~300 LOC
- Each child component: 50-200 LOC (focused responsibility)
- No code duplication
- 0 compilation errors
- 0 broken tests

✓ **Functionality:**
- All existing features work identically to before
- No regression in drag-drop, modals, or calculations
- Performance same or better
- Accessibility unchanged

✓ **Architecture:**
- Clear component ownership
- Easy to add new features (e.g., Story Dependencies)
- Easy for new team members to navigate
- No circular dependencies

✓ **Documentation:**
- Component README with interface docs
- Migration guide for future developers
- Comments in tricky code (drop handlers, calculations)

---

## Timeline Estimate

| Task | Hours | Owner |
|------|-------|-------|
| Planning & Analysis | 0.5 | Done |
| Create 6 new components | 2.5 | Dev |
| Refactor board.ts | 1.5 | Dev |
| Move CSS, update imports | 0.5 | Dev |
| Unit testing | 1 | QA |
| Integration testing | 1 | QA |
| E2E testing | 1 | QA |
| Code review & fixes | 0.5 | Team |
| **TOTAL** | **8 hours** | **2-3 days** |

---

## Additional Benefits (Post-Phase 3)

Once components are modularized, Phase 4+ becomes easier:

### Story Dependencies/Blockers Integration
- ✅ Add `StoryRelationshipsComponent`
- ✅ Integrate into FeatureRow (not crowded!)
- ✅ Drag-drop becomes drag to link stories
- ✅ Modal for relationship editor fits naturally

### SignalR Real-time Collaboration
- ✅ Subscribe to board updates in Board component
- ✅ Broadcast story movements to other users
- ✅ Display "User X is moving Story Y" in FeatureRow
- ✅ No impact on modal/team/capacity components

### Story Refresh Confirmation Modal
- ✅ Add to BoardModalsComponent
- ✅ Show before importing from Azure
- ✅ Let users choose which stories to add/update

### Sprint Planning Recommendations
- ✅ Create ServiceComponent alongside capacity
- ✅ Show "Recommend X story to this sprint"
- ✅ Visual highlighting in FeatureRow

---

## Appendix: Method Dependency Graph

```
DROP (event) ────────────────┐
                             ├─→ parseSprintIdFromDropListId()
                             ├─→ transferArrayItem() [CDK]
                             ├─→ boardService.moveStory()
                             └─→ cdr.detectChanges()

DROPFEATURE (event) ─────────┬─→ moveItemInArray() [CDK]
                             ├─→ boardService.reorderFeatures()
                             └─→ cdr.detectChanges()

FEATURE TOTAL ──────────────→ getStoryTotalPoints() → story.devStoryPoints + story.testStoryPoints

SPRINT TOTALS ──────────────┬─→ getDisplayedSprints()
                             ├─→ getStoriesInSprint()
                             └─→ getStoryTotalPoints()

CAPACITY TOTALS ────────────→ getTeamMembers() → getMemberSprintCapacity()

OVER CAPACITY ──────────────┬─→ getSprintTotals()
                             └─→ getSprintCapacityTotals()

PARKING LOT ID ─────────────→ getParkingLotSprintId() → isParkingLotSprint()

PARKING LOT STORIES ────────→ getParkingLotSprintId() → feature.userStories.filter()

CONNECTED LISTS ────────────┬─→ getDisplayedSprints()
                             └─→ (feature_id_parkingLot, feature_id_sprint_X, ...)

FEATURE SPRINT DEV/TEST ───→ getStoriesInSprint() → story.devStoryPoints + story.testStoryPoints
```

---

## Questions for Review

1. **Component Boundaries:** Are the proposed component boundaries clear?
2. **Data Flow:** Is parent→child Input, child→parent delegation clear?
3. **Testing:** Any additional test scenarios we should consider?
4. **Performance:** Concerns with N components rendering N features?
5. **CSS:** Any shared styling that might break on component split?

