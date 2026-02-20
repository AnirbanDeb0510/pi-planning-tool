# Phase 3A: Component Modularization - Risk Analysis & Mitigation ✅ COMPLETED

**Purpose:** Prevent functionality breakage by understanding all interconnections  
**Created:** February 19, 2026  
**Updated:** February 20, 2026 - All risks addressed and mitigated  
**Status:** ✅ PHASE 3A COMPLETE - All identified risks successfully resolved  
**Audience:** Development team (reference for future maintenance)

---

## PART 1: SIGNAL FLOW ANALYSIS

### Current Signal Dependencies (board.ts)

```
┌─ SERVICE SIGNALS (read-only from boardService)
│  ├─ board: Signal<BoardResponseDto | null>
│  │  └─ Consumed by: Template + all calculation methods
│  ├─ loading: Signal<boolean>
│  │  └─ Used by: Loading overlay conditional
│  └─ error: Signal<string | null>
│     └─ Used by: Error overlay conditional
│
├─ UI STATE SIGNALS
│  ├─ showDevTest: Signal<boolean>
│  │  ├─ Consumed by: CapacityRow, SprintHeader, FeatureRow, Team operations
│  │  └─ Updated by: toggleDevTest()
│  ├─ cursorName, cursorX, cursorY
│  │  └─ Used by: Cursor tracking div (local only)
│  └─ showDevTest propagates to children via @Input
│
├─ TEAM MODAL SIGNALS (→ TeamBar)
│  ├─ showAddMemberModal
│  ├─ editingMember
│  ├─ newMemberName, newMemberRole
│  ├─ memberFormError
│  └─ showDeleteMemberModal, memberToDelete
│     └─ All trigger: boardService.addTeamMember/updateTeamMember/removeTeamMember
│
├─ CAPACITY MODAL SIGNALS (→ CapacityRow)
│  ├─ showCapacityModal
│  ├─ selectedSprintId
│  ├─ capacityEdits: Record<memberId, {dev, test}>
│  └─ capacityFormError
│     └─ Triggers: boardService.updateTeamMemberCapacity()
│
├─ IMPORT FEATURE SIGNALS (→ BoardModals)
│  ├─ showImportFeatureModal
│  ├─ importFeatureId, importPat, rememberPatForImport
│  ├─ importLoading, importError
│  └─ Triggers: boardService.importFeature()
│
├─ REFRESH FEATURE SIGNALS (→ BoardModals)
│  ├─ showRefreshFeatureModal
│  ├─ selectedFeature
│  ├─ refreshPat, rememberPatForRefresh
│  ├─ refreshLoading, refreshError
│  └─ Triggers: boardService.refreshFeature()
│
├─ DELETE FEATURE SIGNALS (→ BoardModals)
│  ├─ showDeleteFeatureModal
│  ├─ featureToDelete
│  ├─ deleteLoading, deleteError
│  └─ Triggers: boardService.deleteFeature()
│
├─ PAT VALIDATION SIGNALS (→ BoardModals)
│  ├─ showPatModal
│  ├─ patModalInput, patValidationError, patValidationLoading
│  ├─ currentBoardId, patValidated
│  ├─ boardPreview
│  └─ Decision point: Determines if board.ts logic proceeds
│
└─ FINALIZATION SIGNALS (→ BoardModals)
   ├─ showFinalizeConfirmation
   ├─ finalizationWarnings, finalizationLoading, finalizationError
   ├─ operationBlockedError
   └─ Used to block: addTeamMember, removeTeamMember, addFeature, deleteFeature
      But allow: moveStory, dropFeature, updateCapacity, refreshFeature
```

### Critical Risk: PAT Modal in Board vs BoardModals?

**Current:** PAT modal in board.ts  
**Question:** Should it move to BoardModals?

**Risk Analysis:**

```
OPTION 1: Keep in Board.ts
├─ Pros:
│  ├─ PAT validation blocks everything
│  ├─ ngOnInit in Board controls flow
│  └─ Ensures board.ts can check patValidated()
├─ Cons:
│  ├─ Increases board.ts signals (keeps 7 signals)
│  └─ PAT Modal implementation separate from other modals
└─ Recommendation: KEEP IN BOARD.TS

OPTION 2: Move to BoardModals
├─ Pros:
│  ├─ All modals in one component
│  └─ Cleaner board.ts
├─ Cons:
│  ├─ BoardModals must return validation state to Board
│  ├─ ngOnInit in Board needs to call BoardModals method
│  ├─ Tight coupling between components
│  └─ **Flow becomes: Board.ngOnInit → BoardModals.showPatModal() → Board.patValidated callback**
└─ Recommendation: NOT RECOMMENDED - too complex
```

**DECISION: Keep PAT modal signals in Board.ts** (7 signals)

---

## PART 2: METHOD DEPENDENCY GRAPH

### Critical Dependencies That Must Remain in board.ts

```
DROP HANDLER (handles story movement between sprints)
│
├─ Input: CdkDragDrop<UserStoryDto[]>
├─ Uses: event.previousContainer, event.container
├─ Must call:
│  ├─ parseSprintIdFromDropListId()
│  │  └─ Uses: getParkingLotSprintId()
│  ├─ transferArrayItem() [CDK]
│  ├─ boardService.moveStory()
│  └─ cdr.detectChanges()
├─ Returns: NEW board state via service
└─ Called from: FeatureRow (delegated)
   ├─ Template: (cdkDropListDropped)="parent.drop($event)"
   └─ MUST WORK: parent reference must be Board instance

DROPFEATURE HANDLER (reorders features)
│
├─ Input: CdkDragDrop<FeatureResponseDto[]>
├─ Must call:
│  ├─ moveItemInArray() [CDK]
│  ├─ boardService.reorderFeatures()
│  └─ cdr.detectChanges()
└─ Called from: Board template (main feature-rows container)

CALCULATIONS (these CAN move but children will call parent)
│
├─ getSprintTotals() → used by SprintHeader, may used internally
├─ getStoriesInSprint() → used by FeatureRow
├─ getParkingLotStories() → used by FeatureRow
├─ getFeatureTotal() → used by FeatureRow
├─ getFeatureSprintDevTestTotals() → used by FeatureRow
├─ getSprintCapacityTotals() → used by CapacityRow, SprintHeader
├─ getMemberSprintCapacity() → used by CapacityRow, CapacityDisplay
├─ getTeamMembers() → used by CapacityRow, TeamBar
├─ isSprintOverCapacity() → used by SprintHeader
└─ getGridTemplateColumns() → used by ALL row components
   └─ BASIS FOR ENTIRE GRID: if this breaks, layout breaks

PARKING LOT HELPERS
│
├─ getParkingLotSprintId() → used by drop(), getStoriesInSprint(), getConnectedLists()
├─ isParkingLotSprint() → used above
└─ These CANNOT move because drop() depends on them synchronously

DISPLAY FILTERED LISTS
│
├─ getDisplayedSprints() → used by SprintHeader, FeatureRow
└─ Returns: sprints minus "Sprint 0"
   └─ MUST be consistent or UI breaks
```

### Safe to Delegate (Already isolated)

```
✓ openAddMember, closeAddMember, saveNewMember → TeamBar
  └─ All state local to TeamBar
  └─ Calls: boardService.addTeamMember()

✓ openEditMember, getMemberRoleLabel → TeamBar or board
  └─ Simple methods, can be anywhere

✓ openDeleteMember, confirmDeleteMember → TeamBar
  └─ Calls: boardService.removeTeamMember()

✓ openCapacityEditor, closeCapacityEditor, saveCapacityEdits → CapacityRow
  └─ All state local to CapacityRow
  └─ Calls: boardService.updateTeamMemberCapacity()

✓ openImportFeatureModal, importFeatureFromAzure → BoardModals
  └─ All state local to BoardModals
  └─ Calls: boardService.importFeature()

✓ Feature refresh, delete, finalization → BoardModals
  └─ All state local to BoardModals
  └─ Calls: boardService.xxx()
```

---

## PART 3: TEMPLATE BINDING BREAKAGE RISKS

### Risk 1: Drop List Initialization

**Current in board.html:**

```html
<div class="feature-row" *ngFor="let feature of board()!.features">
  <div cdkDropList [id]="'feature_' + feature.id + '_parkingLot'"
       [cdkDropListData]="getParkingLotStories(feature)"
       [cdkDropListConnectedTo]="getConnectedLists(feature.id)"
       (cdkDropListDropped)="drop($event)">
```

**After refactoring → FeatureRow component:**

```html
<!-- In feature-row.component.html -->
<div cdkDropList [id]="'feature_' + feature.id + '_parkingLot'"
     [cdkDropListData]="parent.getParkingLotStories(feature)"
     [cdkDropListConnectedTo]="parent.getConnectedLists(feature.id)"
     (cdkDropListDropped)="parent.drop($event)">
```

**Risk Analysis:**

```
BEFORE (works):
├─ drop() is in board component
├─ getParkingLotStories(feature) is in board component
└─ getConnectedLists(feature.id) is in board component
   └─ All same component context ✓

AFTER (potential issue):
├─ drop() is in board component (called via parent reference)
├─ getParkingLotStories() called via parent
├─ getConnectedLists() called via parent
├─ this === FeatureRow component context
├─ parent === Board reference
└─ CDK drop events MUST pass through parent.drop()
   └─ event.container.data === Array reference from drop list
   └─ event.previousContainer === Array reference from previous drop list
   └─ These arrays MUST match actual board.features arrays
   └─ if() event.container === event.previousContainer WORKS

MITIGATION:
├─ Don't change drop handler logic
├─ Pass event unchanged: parent.drop($event)
├─ Test: Open browser DevTools
│  ├─ Drag story between sprints
│  ├─ Check console for drop handler logs
│  ├─ Verify story moved in correct feature
│  ├─ Refresh page
│  └─ Verify persistence
└─ Test with many features (20+)
```

**Likelihood:** MEDIUM - Array references must stay correct

---

### Risk 2: Grid Layout Breakage

**Critical:**
```html
[style.gridTemplateColumns]="parent.getGridTemplateColumns()"
```

**Used in:**
- TeamCapacityRow
- SprintHeader
- FeatureRow (each feature)

**If `getGridTemplateColumns()` changes or breaks:**
- Layouts won't align
- Features at different widths than headers
- Looks completely broken

**Mitigation:**
- Test with 5, 10, 20, 50 features
- Check browser DevTools → Inspect element → grid layout
- Verify grid items align vertically
- Test on mobile (if applicable)

---

### Risk 3: showDevTest Toggle Propagation

**Current flow:**
```
board.ts: protected showDevTest = signal(false)
        ↓ (via @Input)
        ├─ TeamBar
        ├─ CapacityRow
        ├─ SprintHeader
        └─ FeatureRow
```

**Issue:** showDevTest must be reactive signal

**If using manual property:**
```typescript
// WRONG ❌
protected showDevTest = false;

// RIGHT ✓ 
protected showDevTest = signal(false);
```

**Mitigation:**
- Leave showDevTest as signal in board.ts
- Pass to children: `[showDevTest]="showDevTest"`
- Children receive as `@Input() showDevTest!: Signal<boolean>`
- Use in children: `[showDevTest]()` to get current value
- Test: Toggle Dev/Test
  - Verify capacity changes immediately in all components
  - No manual refresh needed

---

## PART 4: SERVICE INTEGRATION RISKS

### Risk 1: BoardService Not Updated

**If board.ts uses methods from service:**

```typescript
this.boardService.moveStory(...)
this.boardService.updateTeamMember(...)
this.boardService.deleteFeature(...)
```

**These service methods MUST exist:**

```typescript
// Check in board.service.ts:
moveStory(storyId, previousSprintId, targetSprintId)
importFeature(boardId, org, project, featureId, pat)
refreshFeature(boardId, featureId, org, project, pat)
deleteFeature(boardId, featureId)
addTeamMember(name, role, showDevTest)
updateTeamMember(memberId, name, role, showDevTest)
removeTeamMember(memberId)
updateTeamMemberCapacity(memberId, sprintId, dev, test)
reorderFeatures(boardId, updates[])
finalizeBoard(boardId)
restoreBoard(boardId)
getFinalizationWarnings(boardId)
toggleDevTestToggle()
```

**Mitigation:**
- Run: `grep -r "this.boardService\." board.ts | cut -d. -f2 | sort | uniq`
- Cross-reference with board.service.ts
- If missing, implement before starting modularization

---

### Risk 2: Change Detection Not Triggered

**After service call, UI might not update:**

```typescript
// Problem: CDK doesn't know about signal updates
moveStory(...)  // Service updates board signal
// Board component must trigger change detection
cdr.detectChanges();  // Force Angular to render
```

**Locations where needed:**
- drop() → after moveStory()
- dropFeature() → after reorderFeatures()
- Toggle → after toggleDevTestToggle()

**Mitigation:**
- Keep cdr.detectChanges() calls in drop() and dropFeature()
- Test: Drag story → immediately see it move
- Test: No delay or visual glitches

---

## PART 5: TESTING CHECKLIST BY RISK

### HIGH RISK TESTS

**Test 1: Drop Handler Data Integrity**
```
1. Create board with 3 features
2. Each feature has 3 stories
3. Drag story from Feature 1 Sprint 1 to Feature 2 Sprint 2
4. Verify:
   ├─ Story appears in Feature 2 Sprint 2
   ├─ Story removed from Feature 1 Sprint 1
   ├─ Page refresh: story still in new location
   └─ No console errors
5. Drag from Parking Lot to Sprint 1
6. Verify:
   ├─ Story moves to Sprint 1
   ├─ originalSprintId stays "Sprint 0"
   └─ Story badge shows "🆕 Added post-plan"
```

**Test 2: Grid Layout Alignment**
```
1. Create board with 20 features × 5 sprints
2. Open DevTools → Inspector
3. Check Sprint Header columns align with Feature rows
4. Verify no column width mismatches
5. Drag column divider (debug): widths should match exactly
6. Test on narrow screen: responsive behavior works
```

**Test 3: Dev/Test Toggle Reactivity**
```
1. Create board with team members
2. Show Dev/Test role in TeamBar
3. Click toggle OFF
4. Verify: Role labels disappear immediately
5. Verify: Capacity row shows single value
6. Click toggle ON
7. Verify: Role labels reappear immediately
8. Verify: Capacity row shows Dev/Test separately
9. Edit team member while toggle OFF
10. Verify: Toggle ON, new member has both roles visible
```

**Test 4: Modal State Isolation**
```
1. Open import feature modal
2. Fill in form (don't submit)
3. Click "Add Member" button
4. Verify: Shows both modals? 
   └─ Should close import, open add member
   └─ Or should both stay open?
   └─ Clarify expected UX
5. Close add member
6. Verify import modal still open
7. Verify import form data still there
```

### MEDIUM RISK TESTS

**Test 5: Feature Reordering with Many Features**
```
1. Create board with 50 features
2. Drag first feature to position 25
3. Verify:
   ├─ Feature moved
   ├─ Numbers updated
   ├─ No performance lag
   └─ Other features unaffected
4. Refresh page
5. Verify order persisted
```

**Test 6: Capacity Calculation Under Load**
```
1. Create board with 20 team members, 10 sprints
2. Edit capacity for each member in sprint 1
3. Verify load/capacity totals update correctly
4. Edit sprint 2 capacity
5. Verify sprint 1 totals unchanged
```

### LOW RISK TESTS

**Test 7: Cursor Tracking**
```
1. Move mouse over board
2. Verify cursor name follows (low risk, cosmetic)
3. Check position calculation correct
```

---

## PART 6: ROLLBACK DECISION TREE

```
IF build fails (compilation errors):
├─ Check: All component imports in board.ts
├─ Check: Template syntax (ngFor, ngIf, bindings)
├─ Check: Component property bindings match @Input/@Output
└─ ROLLBACK if: Still can't find issue after 15 min
   └─ git reset --hard <checkpoint>

IF drop handler fails (story won't move):
├─ Check: parent.drop($event) in FeatureRow template
├─ Check: drop() method still exists in board.ts
├─ Check: event.container.data arrays correct references
├─ Open DevTools: Console for errors
├─ Open DevTools: Network for service calls
└─ ROLLBACK if: Service calls fail or arrays break

IF layout breaks (misaligned columns):
├─ Check: getGridTemplateColumns() calculation correct
├─ Check: All row components use same calculation
├─ Open DevTools: Inspect → check grid-template-columns value
├─ Compare with original board.css grid layout
└─ ROLLBACK if: Can't identify CSS issue

IF modals don't open:
├─ Check: Modal state signals in correct component
├─ Check: CSS display: none/block working
├─ Check: template conditions matching signal names
├─ Open DevTools: Console for errors
└─ ROLLBACK if: Too many issues to debug

IF performance degrades:
├─ Check: Component tree depth (should be <5)
├─ Check: *ngFor loops not unnecessarily nested
├─ Check: Change detection triggers (cdr.detectChanges())
├─ Run: npm run build --prod
├─ Compare build size: should be same
└─ Optimize if: Size increased significantly
```

---

## PART 7: CROSS-COMPONENT CALL VERIFICATION

### Calls Each Component Will Make

**FeatureRow calls to parent (Board):**
```
parent.getStoriesInSprint(feature, sprintId)
parent.getParkingLotStories(feature)
parent.getConnectedLists(feature.id)
parent.getFeatureSprintDevTestTotals(feature, sprintId)
parent.getFeatureTotal(feature)
parent.drop($event)                    ← CRITICAL
parent.getDisplayedSprints()
parent.openRefreshFeatureModal(feature)
parent.openDeleteFeatureModal(feature)
```

**CapacityRow calls to parent:**
```
parent.getTeamMembers()
parent.getDisplayedSprints()
parent.getMemberSprintCapacity(member, sprint)
parent.getMemberRoleLabel(member)
parent.boardService.updateTeamMemberCapacity(...)
```

**SprintHeader calls to parent:**
```
parent.getDisplayedSprints()
parent.getSprintTotals(sprintId)
parent.getSprintCapacityTotals(sprintId)
parent.isSprintOverCapacity(sprintId, type)
parent.showDevTest()
```

**TeamBar calls to parent:**
```
parent.getTeamMembers()
parent.getMemberRoleLabel(member)
parent.toggleDevTest()
parent.openFinalizeConfirmation()
parent.restoreBoard()
parent.boardService.addTeamMember(...)
parent.boardService.updateTeamMember(...)
parent.boardService.removeTeamMember(...)
parent.showDevTest()
parent.finalizationLoading()
parent.isOperationBlocked()
```

**BoardModals calls to parent:**
```
parent.boardService.validatePatForBoard(...)
parent.boardService.loadBoard(boardId)
parent.boardService.importFeature(...)
parent.boardService.refreshFeature(...)
parent.boardService.deleteFeature(...)
parent.boardService.finalizeBoard(...)
parent.boardService.restoreBoard(...)
parent.boardService.getFinalizationWarnings(...)
parent.getStoredPat()
parent.storePat(pat)
parent.clearPat()
parent.router.navigate(['/'])
```

**VALIDATION:** All these methods must exist in board.ts after refactoring

---

## PART 8: ATOMIC CHANGE STRATEGY

### Safest Order to Create Components

**Phase A: Non-critical (CapacityRow, SprintHeader)**
1. Create CapacityRow component (isolated state)
2. Create SprintHeader component (pure display)
3. Test: Build works, no console errors

**Phase B: Core UI (TeamBar, FeatureRow)**
4. Create TeamBar component (isolated state)
5. Create FeatureRow component (delegates drop)
6. Test: CRUD operations work

**Phase C: Complex (BoardModals)**
7. Create BoardModals component (26+ signals)
8. Test: All modals work correctly

**Phase D: Refactor Main**
9. Update board.ts imports
10. Remove moved signals
11. Remove moved methods
12. Update template
13. Test: All functionality

**Why this order?**
- Isolated components first (low risk)
- Complex components second (can test in isolation)
- Main refactoring last (all children ready)
- Each phase can be rolled back independently

---

## PART 9: ACCEPTANCE CRITERIA

### Code Quality
- [ ] 0 compilation errors
- [ ] 0 console errors on board load
- [ ] board.ts reduced from 928 → ~300 LOC
- [ ] No code duplication
- [ ] All methods properly typed

### Functionality
- [ ] Can create/edit/delete team members
- [ ] Can edit sprint capacities
- [ ] Can drag stories between sprints
- [ ] Can reorder features
- [ ] Can import features from Azure
- [ ] Can refresh features
- [ ] Can finalize/restore board
- [ ] All modals work
- [ ] Dev/Test toggle works
- [ ] Dark mode still works

### Performance
- [ ] Component render time same or better
- [ ] No memory leaks (DevTools Profiler)
- [ ] Build size same or smaller

### Architecture
- [ ] Clear component ownership
- [ ] No circular dependencies
- [ ] Easy to test individual components
- [ ] Easy to add new features

### Testing
- [ ] 100% of manual test scenarios pass
- [ ] 0 regressions
- [ ] All edge cases tested (50+ features, 20+ members, etc.)

---

## GLOSSARY

- **CDK:** Angular CDK (Component Dev Kit) - drag-drop, virtual scroll, etc.
- **Signal:** Angular signal reactive state
- **@Input:** Component input property binding
- **cdr.detectChanges():** Force Angular change detection
- **drop():** Handler for story movement
- **dropFeature():** Handler for feature reordering
- **BoardResponseDto:** Data model for board
- **parent reference:** Child component holds reference to Board instance

---

## FINAL RECOMMENDATIONS

1. **Follow atomic phase strategy** - don't skip ahead
2. **Test after each created component** - don't wait until end
3. **Keep backup of current board.ts** - reference if confused
4. **Log all drop events** - helps debug drag-drop issues
5. **Use browser DevTools extensively** - inspect grid, check signals
6. **Document any gotchas** - helps future maintainers
7. **Pair program for complex components** - catch issues early
8. **Get code review after each phase** - prevent accumulating issues

---

## SUCCESS INDICATORS

✅ **Quick wins (first 2 hours):**
- CapacityRow component created and building
- SprintHeader component created and building
- No new errors introduced

✅ **Mid-point (4 hours):**
- TeamBar component working (can add/edit/delete members)
- FeatureRow component working (displays stories correctly)
- Drop handler delegating to parent.drop()

✅ **Final (6-8 hours):**
- All components created and integrated
- board.ts refactored to ~300 LOC
- All tests passing
- 0 regressions

