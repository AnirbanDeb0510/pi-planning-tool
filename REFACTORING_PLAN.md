# Refactoring & Improvement Plan

**Created:** February 9, 2026  
**Status:** Planning Phase  
**Priority:** Medium-High (Should be addressed before Phase 3)

---

## 📊 Current State Analysis

### Codebase Metrics

| File                      | Lines | Status         | Concern Level |
| ------------------------- | ----- | -------------- | ------------- |
| `board.ts`                | 359   | Large          | 🟡 Medium     |
| `board.html`              | 284   | Large          | 🟡 Medium     |
| `board.css`               | 813   | Very Large     | 🔴 High       |
| `board.service.ts`        | 322   | Acceptable     | 🟢 Low        |
| **Total Board Component** | 1,778 | **Monolithic** | 🔴 **High**   |

### Architecture Overview

**Current Structure:**

```
frontend/src/app/
├── Components/
│   ├── board/                    # 🔴 Monolithic (1,778 lines)
│   │   ├── board.ts             # 359 lines - too many responsibilities
│   │   ├── board.html           # 284 lines - includes 2 modals
│   │   └── board.css            # 813 lines - everything in one file
│   ├── story-card/              # ✅ Well-structured
│   └── enter-your-name/         # ✅ Simple component
├── Services/
│   └── user.service.ts          # ✅ Simple service
├── core/
│   └── services/
│       └── theme.service.ts     # ✅ Well-structured
├── features/
│   └── board/
│       └── services/
│           └── board.service.ts # 🟡 Currently handles mock data
└── shared/
    └── models/
        └── board.dto.ts         # ✅ Well-structured DTOs
```

**Issues Identified:**

1. **No API Integration Layer** - BoardService mixes state management with mock data
2. **No Routing Structure** - Can't create/load different boards
3. **Hardcoded Strings** - ~40+ UI strings directly in HTML templates
4. **Component Monolith** - board.ts/html/css handles too many concerns
5. **No HTTP Abstraction** - No centralized API client service
6. **Large CSS Files** - 813 lines in single file, hard to maintain
7. **No Environment Config** - No environment-specific settings

---

## 🎯 Proposed Improvements (Refined)

### 1. Routing & Board Management 🔴 HIGH PRIORITY

**Current State:**

- Simple routing: home (name entry) → board
- No concept of board ID or multiple boards
- No board creation/loading flow

**Proposed Enhancement:**

**New Routes:**

```typescript
// app.routes.ts (enhanced)
export const routes: Routes = [
  { path: "", component: HomeComponent },
  { path: "boards", component: BoardListComponent }, // List all boards
  { path: "boards/new", component: CreateBoardComponent }, // Create new board form
  { path: "boards/:id", component: BoardComponent }, // View/edit board
  { path: "name", component: EnterYourName }, // Name entry (modal?)
  { path: "**", redirectTo: "" },
];
```

**Components to Create:**

**a) HomeComponent (Landing Page):**

- Welcome message
- "Create New Board" button → `/boards/new`
- "Load Existing Board" button → `/boards`
- Recent boards list (if user has history)

**b) CreateBoardComponent (Form):**

- Form fields:
  - Board Name (required)
  - Organization (optional)
  - Project (optional)
  - Number of Sprints (default: 6)
  - Sprint Duration (default: 14 days)
  - Start Date (required)
  - Dev/Test Toggle (default: false)
- Submit → POST /api/boards → Navigate to `/boards/:id`

**c) BoardListComponent (Board Browser):**

- Table/card view of all boards
- Columns: Name, Organization, Project, Created Date, Status (locked/finalized)
- Search/filter by name, org, project
- Actions: Open, Duplicate, Delete
- Pagination (if many boards)

**Implementation Steps:**

1. Create home/create-board/board-list components
2. Update routing configuration
3. Add board creation form with validation
4. Integrate with POST /api/boards endpoint
5. Add board list fetch with filters
6. Update EnterYourName to be a modal or step within flow

**Estimated Effort:** 12-16 hours

---

### 2. API Integration Architecture 🔴 HIGH PRIORITY

**Current State:**

- BoardService has mock data generation
- No HTTP calls to backend
- No API base URL configuration
- No error handling or loading states

**Proposed Architecture:**

**Layer Separation:**

```
BoardComponent           # UI only (presentation)
       ↓
BoardService            # State management (signals)
       ↓
BoardApiService         # HTTP calls (API layer)
       ↓
HttpClientService       # Base HTTP abstraction
       ↓
Angular HttpClient      # Framework level
```

**New Files to Create:**

**a) Environment Configuration:**

```typescript
// environments/environment.ts
export const environment = {
  production: false,
  apiBaseUrl: "http://localhost:5000",
  apiVersion: "v1",
  enableMockData: false,
};

// environments/environment.prod.ts
export const environment = {
  production: true,
  apiBaseUrl: "https://api.piplanningtool.com",
  apiVersion: "v1",
  enableMockData: false,
};
```

**b) Base HTTP Service:**

```typescript
// core/services/http-client.service.ts
@Injectable({ providedIn: "root" })
export class HttpClientService {
  private baseUrl = environment.apiBaseUrl;

  constructor(private http: HttpClient) {}

  get<T>(endpoint: string, options?): Observable<T>;
  post<T>(endpoint: string, body: any, options?): Observable<T>;
  put<T>(endpoint: string, body: any, options?): Observable<T>;
  patch<T>(endpoint: string, body: any, options?): Observable<T>;
  delete<T>(endpoint: string, options?): Observable<T>;

  // Error handling, interceptors, retry logic
}
```

**c) Board API Service:**

```typescript
// features/board/services/board-api.service.ts
@Injectable({ providedIn: "root" })
export class BoardApiService {
  constructor(private http: HttpClientService) {}

  getBoard(id: number): Observable<BoardResponseDto>;
  createBoard(dto: BoardCreateDto): Observable<BoardResponseDto>;
  getBoardList(filters?: BoardFilters): Observable<BoardResponseDto[]>;
  lockBoard(id: number): Observable<void>;
  finalizeBoard(id: number): Observable<void>;
}
```

**d) Refactored BoardService:**

```typescript
// features/board/services/board.service.ts
@Injectable({ providedIn: "root" })
export class BoardService {
  private boardSignal = signal<BoardResponseDto | null>(null);
  private loadingSignal = signal<boolean>(false);
  private errorSignal = signal<string | null>(null);

  public board = this.boardSignal.asReadonly();
  public loading = this.loadingSignal.asReadonly();
  public error = this.errorSignal.asReadonly();

  constructor(private boardApi: BoardApiService) {}

  loadBoard(id: number): void {
    this.loadingSignal.set(true);
    this.boardApi.getBoard(id).subscribe({
      next: (board) => {
        this.boardSignal.set(board);
        this.loadingSignal.set(false);
      },
      error: (err) => {
        this.errorSignal.set(err.message);
        this.loadingSignal.set(false);
      },
    });
  }

  // Remove all mock data generation
  // Keep only state management logic
}
```

**Additional Services Needed:**

- `FeatureApiService` - Feature CRUD operations
- `StoryApiService` - Story CRUD operations
- `TeamApiService` - Team member CRUD operations

**Implementation Steps:**

1. Create environment files with API base URL
2. Create HttpClientService with base HTTP methods
3. Create domain-specific API services (BoardApi, FeatureApi, etc.)
4. Refactor BoardService to use API services
5. Remove all mock data from BoardService
6. Add loading/error states to UI
7. Add HTTP interceptor for auth/error handling

**Estimated Effort:** 16-20 hours

---

### 3. String Externalization 🟡 MEDIUM PRIORITY

**Current State:**

- 40+ hardcoded strings in board.html
- No internationalization support
- Strings scattered across components

**Examples of Hardcoded Strings:**

```html
"Team Members" "+ Add Member" "Dev/Test Toggle" "Enter name" "Load" "Capacity"
"Edit" "Add" "Cancel" "Save" "Parking Lot" "Feature"
```

**Proposed Solution:**

**Option A: Constants File (Simpler, for now)**

```typescript
// shared/constants/ui-constants.ts
export const UI_CONSTANTS = {
  LABELS: {
    TEAM_MEMBERS: "Team Members",
    ADD_MEMBER: "Add Member",
    DEV_TEST_TOGGLE: "Dev/Test Toggle",
    ENTER_NAME: "Enter name",
    LOAD: "Load",
    CAPACITY: "Capacity",
    EDIT: "Edit",
    ADD: "Add",
    CANCEL: "Cancel",
    SAVE: "Save",
    PARKING_LOT: "Parking Lot",
    FEATURE: "Feature",
  },
  PLACEHOLDERS: {
    MEMBER_NAME: "Enter member name",
    CAPACITY: "Enter capacity",
  },
  MESSAGES: {
    LOADING: "Loading board...",
    ERROR_LOADING: "Failed to load board",
    SAVE_SUCCESS: "Changes saved successfully",
  },
};

// board/constants/board-constants.ts
export const BOARD_CONSTANTS = {
  DEFAULT_SPRINTS: 6,
  DEFAULT_SPRINT_DURATION: 14,
  MIN_SPRINT_DURATION: 7,
  MAX_SPRINT_DURATION: 30,
  SPRINT_0_NAME: "Sprint 0 (Parking Lot)",
};
```

**Usage in Template:**

```html
<div class="team-bar-title">{{ UI.LABELS.TEAM_MEMBERS }}</div>
<button (click)="openAddMember()">+ {{ UI.LABELS.ADD_MEMBER }}</button>
```

**Usage in Component:**

```typescript
export class Board {
  protected readonly UI = UI_CONSTANTS;
  protected readonly BOARD = BOARD_CONSTANTS;
}
```

**Option B: Angular i18n (For future internationalization)**

- Defer this until MVP is complete
- Can migrate from constants to i18n later
- Use `@angular/localize` when ready for multiple languages

**Implementation Steps:**

1. Create `ui-constants.ts` with all UI strings
2. Create domain-specific constants (board, feature, team)
3. Update components to import and use constants
4. Replace all hardcoded strings in templates
5. Document constants in each constant file

**Estimated Effort:** 6-8 hours

---

### 4. Component Modularization 🔴 HIGH PRIORITY

**Current State:**

- board.ts: 359 lines - too many responsibilities
- board.html: 284 lines - includes modals, team bar, capacity row, etc.
- board.css: 813 lines - styles for everything
- Poor testability and reusability

**Proposed Component Breakdown:**

**Main Board Component (Remains):**

- Purpose: Container, layout orchestration
- Responsibilities: drag-drop coordination, route params, loading state
- Estimated size after refactor: ~150 lines

**New Components to Extract:**

**a) TeamMemberBarComponent:**

```typescript
// board/components/team-member-bar/team-member-bar.component.ts
@Component({
  selector: "app-team-member-bar",
  standalone: true,
  template: `
    <div class="team-bar">
      <div class="team-bar-title">Team Members</div>
      <div class="team-member-list">
        <div class="team-member-chip" *ngFor="let member of members()">
          <span class="member-name">{{ member.name }}</span>
          <span
            *ngIf="showRoles()"
            class="member-role"
            [ngClass]="getRoleClass(member)"
          >
            {{ getRoleLabel(member) }}
          </span>
        </div>
        <button class="add-member-button" (click)="onAddClick()">
          + Add Member
        </button>
      </div>
    </div>
  `,
})
export class TeamMemberBarComponent {
  @Input() members = input.required<TeamMemberResponseDto[]>();
  @Input() showRoles = input<boolean>(false);
  @Output() addMember = output<void>();
}
```

**Estimated size:** ~100 lines (TS+HTML+CSS)

**b) AddTeamMemberModalComponent:**

```typescript
// board/components/add-team-member-modal/add-team-member-modal.component.ts
@Component({
  selector: "app-add-team-member-modal",
  standalone: true,
  // Full modal UI
})
export class AddTeamMemberModalComponent {
  @Input() isOpen = input<boolean>(false);
  @Input() showRoleSelector = input<boolean>(false);
  @Output() close = output<void>();
  @Output() save = output<{ name: string; role?: "dev" | "test" }>();
}
```

**Estimated size:** ~120 lines (TS+HTML+CSS)

**c) TeamCapacityRowComponent:**

```typescript
// board/components/team-capacity-row/team-capacity-row.component.ts
@Component({
  selector: "app-team-capacity-row",
  standalone: true,
  // Capacity row UI
})
export class TeamCapacityRowComponent {
  @Input() sprints = input.required<SprintDto[]>();
  @Input() members = input.required<TeamMemberResponseDto[]>();
  @Input() showDevTest = input<boolean>(false);
  @Output() editCapacity = output<number>(); // sprint ID
}
```

**Estimated size:** ~150 lines (TS+HTML+CSS)

**d) CapacityEditorModalComponent:**

```typescript
// board/components/capacity-editor-modal/capacity-editor-modal.component.ts
@Component({
  selector: "app-capacity-editor-modal",
  standalone: true,
  // Capacity editor UI
})
export class CapacityEditorModalComponent {
  @Input() isOpen = input<boolean>(false);
  @Input() sprint = input.required<SprintDto | null>();
  @Input() members = input.required<TeamMemberResponseDto[]>();
  @Input() showDevTest = input<boolean>(false);
  @Output() close = output<void>();
  @Output() save = output<CapacityUpdate[]>();
}
```

**Estimated size:** ~180 lines (TS+HTML+CSS)

**e) SprintHeaderComponent:**

```typescript
// board/components/sprint-header/sprint-header.component.ts
@Component({
  selector: "app-sprint-header",
  standalone: true,
  // Sprint header cell UI
})
export class SprintHeaderComponent {
  @Input() sprint = input.required<SprintDto>();
  @Input() load = input.required<{
    dev: number;
    test: number;
    total: number;
  }>();
  @Input() capacity = input.required<{
    dev: number;
    test: number;
    total: number;
  }>();
  @Input() showDevTest = input<boolean>(false);
}
```

**Estimated size:** ~100 lines (TS+HTML+CSS)

**f) FeatureRowComponent:**

```typescript
// board/components/feature-row/feature-row.component.ts
@Component({
  selector: "app-feature-row",
  standalone: true,
  // Feature row with story cards
})
export class FeatureRowComponent {
  @Input() feature = input.required<FeatureResponseDto>();
  @Input() sprints = input.required<SprintDto[]>();
  @Input() showDevTest = input<boolean>(false);
  @Output() storyDrop = output<CdkDragDrop<UserStoryDto[]>>();
}
```

**Estimated size:** ~200 lines (TS+HTML+CSS)

**New Folder Structure:**

```
features/board/
├── board.component.ts            # 150 lines (orchestrator)
├── board.component.html          # 80 lines
├── board.component.css           # 150 lines
└── components/
    ├── team-member-bar/
    │   ├── team-member-bar.component.ts
    │   ├── team-member-bar.component.html
    │   └── team-member-bar.component.css
    ├── add-team-member-modal/
    │   ├── add-team-member-modal.component.ts
    │   ├── add-team-member-modal.component.html
    │   └── add-team-member-modal.component.css
    ├── team-capacity-row/
    │   ├── team-capacity-row.component.ts
    │   ├── team-capacity-row.component.html
    │   └── team-capacity-row.component.css
    ├── capacity-editor-modal/
    │   ├── capacity-editor-modal.component.ts
    │   ├── capacity-editor-modal.component.html
    │   └── capacity-editor-modal.component.css
    ├── sprint-header/
    │   ├── sprint-header.component.ts
    │   ├── sprint-header.component.html
    │   └── sprint-header.component.css
    └── feature-row/
        ├── feature-row.component.ts
        ├── feature-row.component.html
        └── feature-row.component.css
```

**Benefits:**

- ✅ Each component <200 lines
- ✅ Single responsibility
- ✅ Easier to test
- ✅ Reusable across app
- ✅ Better maintainability
- ✅ Parallel development possible

**Implementation Steps:**

1. Create component folder structure
2. Extract AddTeamMemberModal (simplest, no dependencies)
3. Extract CapacityEditorModal
4. Extract TeamMemberBar
5. Extract TeamCapacityRow
6. Extract SprintHeader
7. Extract FeatureRow
8. Update main board component to use new components
9. Move CSS to component-specific files
10. Test all functionality still works

**Estimated Effort:** 20-24 hours

---

### 5. CSS Strategy & Styling 🟡 MEDIUM PRIORITY

**Current State:**

- board.css: 813 lines - everything in one file
- All custom CSS (no utility framework)
- Some duplication (modal styles, button styles, input styles)
- Hard to maintain as features grow

**Analysis:**

**Current Approach Pros:**

- ✅ Full control over styles
- ✅ No learning curve
- ✅ No additional dependencies
- ✅ Performant (no unused CSS)

**Current Approach Cons:**

- ❌ Large single file (813 lines)
- ❌ Some duplication
- ❌ No design system consistency
- ❌ Hard to scale

**Option A: Keep Custom CSS + Component Extraction (RECOMMENDED)**

**Benefits:**

- Component extraction will naturally split CSS
- ~813 lines → ~6-7 files of 100-150 lines each
- No new dependencies or learning curve
- Can create shared styles for common elements

**Shared Styles Structure:**

```
styles/
├── _variables.css      # Colors, spacing, typography
├── _mixins.css         # Reusable style patterns
├── _modal.css          # Modal base styles
├── _buttons.css        # Button styles
├── _inputs.css         # Form input styles
└── _utilities.css      # Utility classes (flex, grid, spacing)
```

**Implementation:**

1. Extract component CSS naturally during component modularization
2. Create shared styles for common patterns
3. Use CSS variables for theming
4. Keep custom approach

**Estimated Effort:** 4-6 hours (as part of component extraction)

---

**Option B: Migrate to Tailwind CSS (CONSIDER FOR FUTURE)**

**Benefits:**

- ✅ Utility-first approach
- ✅ Smaller bundle size (purged unused styles)
- ✅ Consistent design system
- ✅ Faster development (once learned)
- ✅ Dark mode support built-in

**Drawbacks:**

- ❌ Setup overhead (~4-6 hours)
- ❌ Learning curve for team
- ❌ HTML becomes verbose with classes
- ❌ Need to rewrite existing 813 lines
- ❌ Custom animations harder to implement

**If choosing Tailwind:**

1. Install & configure Tailwind CSS 3.x
2. Configure purge/content paths for Angular
3. Create custom theme (colors, spacing)
4. Migrate existing CSS gradually
5. Create Tailwind config for dark theme

**Estimated Effort:** 20-30 hours (setup + migration)

---

**Option C: Hybrid Approach**

**Combination:**

- Use Tailwind for layout utilities (flex, grid, spacing)
- Keep custom CSS for complex components (board grid, drag-drop)
- Best of both worlds

**When to use what:**

- Tailwind: Layout, spacing, colors, typography
- Custom CSS: Complex animations, drag-drop, grid layouts

**Estimated Effort:** 12-16 hours (setup + selective migration)

---

**RECOMMENDATION:**

**Phase 1 (Now - Before Phase 3):** Option A - Component Extraction

- Split CSS naturally through component modularization
- Create shared styles for common elements
- Keep custom CSS approach
- Effort: Included in component extraction

**Phase 2 (After MVP, during polish):** Evaluate Tailwind

- Once components are extracted and stable
- Team has bandwidth to learn Tailwind
- Can migrate gradually, component by component

**Why this order?**

1. Component extraction is higher priority (testability, maintainability)
2. CSS will naturally split during extraction
3. Can evaluate Tailwind with better context after extraction
4. Don't introduce two major changes simultaneously

---

## 🎯 Implementation Roadmap

### Priority Order

| Priority | Task                       | Effort | Dependencies | Target Date |
| -------- | -------------------------- | ------ | ------------ | ----------- |
| P0       | API Integration            | 16-20h | None         | Feb 16      |
| P0       | Environment Config         | 2h     | None         | Feb 11      |
| P1       | Component Modularization   | 20-24h | None         | Feb 20      |
| P1       | Routing & Board Management | 12-16h | API layer    | Feb 23      |
| P2       | String Externalization     | 6-8h   | None         | Feb 25      |
| P3       | CSS Strategy (evaluate)    | 4-6h   | Components   | Mar 1       |

### Suggested Phases

**Phase A: Foundation (Feb 10-16) - 18-22 hours**

1. ✅ Create environment configuration
2. ✅ Create HttpClientService (base API client)
3. ✅ Create BoardApiService, FeatureApiService, TeamApiService
4. ✅ Refactor BoardService to use API services
5. ✅ Remove mock data from BoardService
6. ✅ Add loading/error states to UI
7. ✅ Test with real backend API

**Phase B: Componentization (Feb 17-20) - 20-24 hours**

1. ✅ Extract AddTeamMemberModal
2. ✅ Extract CapacityEditorModal
3. ✅ Extract TeamMemberBar
4. ✅ Extract TeamCapacityRow
5. ✅ Extract SprintHeader
6. ✅ Extract FeatureRow
7. ✅ Update main board component
8. ✅ Test all functionality

**Phase C: Routing & Navigation (Feb 21-23) - 12-16 hours**

1. ✅ Create HomeComponent (landing page)
2. ✅ Create CreateBoardComponent (form)
3. ✅ Create BoardListComponent (browser)
4. ✅ Update routing configuration
5. ✅ Integrate board creation with API
6. ✅ Add board/:id parameter to BoardComponent
7. ✅ Test full navigation flow

**Phase D: Polish & Cleanup (Feb 24-25) - 10-14 hours**

1. ✅ Extract all strings to constants
2. ✅ Update components to use constants
3. ✅ Create shared CSS for common elements
4. ✅ Document new architecture
5. ✅ Update README with new structure
6. ✅ Code review and testing

**Total Effort:** 60-76 hours (1.5-2 weeks full-time)

---

## 🚨 Risk Assessment

| Risk                                  | Probability | Impact | Mitigation                                          |
| ------------------------------------- | ----------- | ------ | --------------------------------------------------- |
| Breaking existing functionality       | Medium      | High   | Incremental refactoring, extensive testing          |
| Backend API not ready for integration | Low         | High   | Use mock mode toggle, implement mock API service    |
| Component extraction takes longer     | Medium      | Medium | Start with simplest components, timebox efforts     |
| Team unfamiliar with new architecture | Low         | Medium | Document patterns, provide examples                 |
| Routing changes affect existing users | Low         | Low    | Not in production yet, smooth transition path       |
| CSS split causes style bugs           | Medium      | Low    | Test each component in isolation, visual regression |
| HTTP interceptor causes auth issues   | Low         | Medium | Test auth flow early, add error logging             |

---

## ✅ Definition of Done

Each refactoring task is done when:

1. **Code Complete**
   - [ ] All acceptance criteria met
   - [ ] New components/services follow Angular best practices
   - [ ] TypeScript strict mode passes
   - [ ] No console errors or warnings

2. **Tested**
   - [ ] Existing functionality still works
   - [ ] New components render correctly
   - [ ] API integration tested with real backend
   - [ ] Loading/error states work as expected

3. **Documented**
   - [ ] Component README created (if complex)
   - [ ] JSDoc comments for public APIs
   - [ ] Architecture diagram updated (if applicable)
   - [ ] This plan updated with progress

4. **Code Quality**
   - [ ] ESLint passes
   - [ ] Prettier formatted
   - [ ] No duplicate code
   - [ ] Follows project conventions

---

## 📚 References & Resources

### Angular Best Practices

- [Angular Style Guide](https://angular.io/guide/styleguide)
- [Angular Architecture Patterns](https://angular.io/guide/architecture)
- [Component Communication](https://angular.io/guide/component-interaction)

### API Integration

- [HttpClient Guide](https://angular.io/guide/http)
- [RxJS Best Practices](https://rxjs.dev/guide/overview)
- [Error Handling in Angular](https://angular.io/guide/http#error-handling)

### Component Design

- [Smart vs Presentational Components](https://blog.angular-university.io/angular-2-smart-components-vs-presentation-components-whats-the-difference-when-to-use-each-and-why/)
- [Angular Signals](https://angular.io/guide/signals)

### CSS Architecture

- [BEM Methodology](http://getbem.com/)
- [CSS Architecture for Angular](https://angular.io/guide/component-styles)
- [Tailwind CSS Docs](https://tailwindcss.com/docs) (if choosing Tailwind)

---

## 📝 Progress Tracking

### Phase A: Foundation (Feb 10-16)

- [ ] Environment configuration created
- [ ] HttpClientService implemented
- [ ] BoardApiService implemented
- [ ] FeatureApiService implemented
- [ ] TeamApiService implemented
- [ ] BoardService refactored
- [ ] API integration tested

### Phase B: Componentization (Feb 17-20)

- [ ] AddTeamMemberModal extracted
- [ ] CapacityEditorModal extracted
- [ ] TeamMemberBar extracted
- [ ] TeamCapacityRow extracted
- [ ] SprintHeader extracted
- [ ] FeatureRow extracted
- [ ] Board component updated
- [ ] All functionality tested

### Phase C: Routing & Navigation (Feb 21-23)

- [ ] HomeComponent created
- [ ] CreateBoardComponent created
- [ ] BoardListComponent created
- [ ] Routing updated
- [ ] Board creation integrated
- [ ] Board loading integrated
- [ ] Full flow tested

### Phase D: Polish & Cleanup (Feb 24-25)

- [ ] Strings externalized
- [ ] Shared CSS created
- [ ] Documentation updated
- [ ] Code review completed

---

**Last Updated:** February 9, 2026  
**Next Review:** February 11, 2026 (after environment setup)  
**Owner:** Anirban Deb
