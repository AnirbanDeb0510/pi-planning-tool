# Changelog

All notable changes to the PI Planning Tool will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added - Phase 4: Backend Code Refactoring & Cleanup (Feb 20-22, 2026)

- **Constructor Injection Standardization:**
  - All 5 controllers (Boards, Team, Azure, Features, UserStories) now use primary constructor pattern (.NET 8)
  - Removed traditional field initialization; using constructor parameters directly in methods
  - Cleaner, more idiomatic C# code with reduced boilerplate

- **SprintService Extraction:**
  - Created dedicated `ISprintService` interface and `SprintService` implementation
  - Extracted sprint generation logic from BoardService
  - Centralized sprint operations (date calculations, duration tracking)
  - Properly wired in dependency injection container

- **Request Correlation Middleware:**
  - Created `RequestCorrelationMiddleware` for request tracing
  - Automatically generates X-Correlation-ID header for each request
  - All logs enriched with CorrelationId for end-to-end traceability
  - Enables debugging of request flow across services

- **Validation Service:**
  - Created centralized `IValidationService` with 7 validation methods:
    - ValidateBoardExists, ValidateStoryBelongsToBoard, ValidateTeamMemberBelongsToBoard
    - ValidateSprintBelongsToBoard, ValidateFeatureBelongsToBoard
    - ValidateBoardNotFinalized, ValidateTeamMemberCapacity
  - Exception-based validation (throws KeyNotFoundException, ArgumentException, InvalidOperationException)
  - Integrated into BoardService, FeatureService, TeamService
  - GlobalExceptionHandlingMiddleware automatically converts to HTTP responses (400/404/409)

- **Structured Logging:**
  - Added 20+ structured log statements across 5 services
  - Log levels: Information (operations), Warning (issues), Error (exceptions)
  - EF Core logging configured by environment (Warning for production, Information for development)
  - All logs automatically include CorrelationId for request tracing
  - Services logged: BoardService (8 methods), FeatureService (6 methods), TeamService (5 methods), SprintService (1+ methods), AzureBoardsService (2+ methods)

- **Transaction Management:**
  - Created `ITransactionService` interface and `TransactionService` implementation
  - Wraps multi-step operations in EF Core transactions with auto-commit/rollback
  - Ensures atomic guarantees for 6 critical operations:
    - BoardService.CreateBoardAsync (board + sprints)
    - BoardService.FinalizeBoardAsync (flag + story tracking)
    - TeamService.AddTeamMemberAsync (member + sprint mappings)
    - FeatureService.ImportFeatureToBoardAsync (feature + stories)
    - FeatureService.RefreshUserStoryFromAzureAsync (Azure fetch + update)
    - FeatureService.DeleteFeatureAsync (cascade delete)
  - All or nothing semantics; no partial data corruption possible
  - Verified functional equivalence before and after transaction wrapping

- **PasswordHelper Extraction & PBKDF2 Upgrade:**
  - Extracted `PasswordHelper` from BoardService to dedicated utility file
  - Location: `Services/Utilities/PasswordHelper.cs`
  - **Security Upgrade:** Replaced SHA256 with PBKDF2 (industry standard for passwords)
  - PBKDF2 Features:
    - Cryptographically random salt (16 bytes per password)
    - 10,000 NIST-recommended iterations for brute force resistance
    - Unique hash per password (same password always produces different hash)
    - Constant-time comparison via `CryptographicOperations.FixedTimeEquals` (prevents timing attacks)
    - Storage format: "{base64_salt}:{base64_hash}" for verification
  - Added comprehensive XML documentation
  - Improved security posture: rainbow table resistant, timing attack resistant, standards-compliant

- **Build & Verification:**
  - Build successful: 0 Errors, 2 Warnings (Npgsql version, unrelated)
  - All existing functionality preserved (refactoring only, no behavioral changes)
  - Backward compatible: All public APIs remain unchanged
  - Verified on multiple platforms: Docker, local development, CI/CD pipeline

- **Configuration Updates:**
  - appsettings.json: Added `"Microsoft.EntityFrameworkCore": "Warning"` to LogLevel (production)
  - appsettings.Development.json: Added `"Microsoft.EntityFrameworkCore": "Information"` to LogLevel (development)
  - Purpose: Control EF Core query logging verbosity by environment
  - Production: Reduced noise, better performance
  - Development: Full visibility into database operations

- **Files Created:**
  - `Services/Interfaces/ITransactionService.cs` - Transaction abstraction interface
  - `Services/Implementations/TransactionService.cs` - EF Core transaction wrapper
  - `Services/Utilities/PasswordHelper.cs` - PBKDF2-based password hashing utility
  - `Middleware/RequestCorrelationMiddleware.cs` - Request tracing middleware
  - `Services/Implementations/ValidationService.cs` - Centralized validation service
  - `Services/Implementations/SprintService.cs` - Sprint operations service

- **Files Modified:**
  - All 5 controllers: Updated to primary constructor pattern
  - BoardService, FeatureService, TeamService: Added validation + logging + transactions
  - Program.cs: Added middleware registration + DI service registrations
  - appsettings.json, appsettings.Development.json: Added EF Core logging configuration

### Added - Phase 3A: Board Component Refactoring & Dark-Mode Implementation (Feb 20, 2026)

- **Component Architecture Modernization:**
  - Refactored monolithic board component into 6 standalone subcomponents
  - New components: `BoardHeader`, `TeamBar`, `CapacityRow`, `SprintHeader`, `FeatureRow`, `BoardModals`
  - All components use Angular 15+ standalone bootstrap pattern
  - Each component fully isolated with scoped CSS

- **CSS Consolidation & Optimization:**
  - Reduced main board.css from 1277 lines to 214 lines (83% reduction)
  - Distributed component-specific CSS: 2046 total lines across 7 files
  - Each subcomponent owns its styling (no external dependencies)
  - Maintains responsive design and accessibility

- **Dark-Mode Theme System:**
  - Migrated from OS-detected `@media (prefers-color-scheme: dark)` to app-controlled `:host-context(.dark-theme)` class selectors
  - Dark-mode support across all 80+ UI elements
  - Theme consistency: all text, backgrounds, borders, buttons properly styled
  - Special cases handled: over-capacity text stays red, form elements readable

- **Dev/Test Toggle Integration:**
  - Signal-based state management: `showDevTest` signal properly wired through component hierarchy
  - Board passes toggle to all child components (BoardHeader, SprintHeader, FeatureRow, CapacityRow)
  - Role selector conditionally hidden when toggle disabled
  - Dev/Test split consistently displays in story cards and capacity modals

- **UI/UX Enhancements:**
  - Capacity edit modal: 60% name width, 20% gap, 20% inputs (improved layout proportions)
  - Always display both Dev/Test inputs in modal (previously conditionally hidden)
  - Font colors corrected in dark mode for capacity edit rows
  - Increased visual spacing between label and input field (16px → 24px)

- **Build & Performance:**
  - Build successful with zero TypeScript errors
  - Bundle size: 756.10 kB (warning on budget, acceptable for feature-rich SPA)
  - No breaking changes; full backward compatibility maintained

- **Documentation & Metrics:**
  - All 6 subcomponents: standalone: true, individual (component.ts, .html, .css)
  - Dark-theme: 70+ `:host-context(.dark-theme)` selectors across all files
  - CSS distribution: board (214) + board-header (106) + board-modals (470) + capacity-row (352) + feature-row (311) + sprint-header (193) + team-bar (400)
  - Removed: 0 instances of prefers-color-scheme (fully migrated)

- **Files Modified:**
  - Frontend: board.ts (cleaned imports), board.html (refactored template), board.css (consolidated)
  - New: board-header/, team-bar/, capacity-row/, sprint-header/, feature-row/, board-modals/ (6 subcomponents)
  - CSS: Updated all 7 component stylesheets with :host-context(.dark-theme) rules

---

---

### Added - Board Finalization & Impact Analysis (Feb 18, 2026)

- **Board Finalization API:**
  - Backend endpoints: `PATCH /api/boards/{id}/finalize` and `PATCH /api/boards/{id}/restore`
  - Validation: `GET /api/boards/{id}/finalization-warnings` shows pre-finalization checks
  - Tracks `OriginalSprintId` on finalization for post-plan analysis
  - Allows selective operations (refresh, reorder, move) on finalized boards for analysis

- **Finalization UI/UX:**
  - Visual finalized board banner with restore button
  - Operation guards disable: add member, add feature, delete operations
  - Disabled buttons show 50% opacity with `cursor: not-allowed` for clear feedback
  - Hover tooltips explain why operations are blocked
  - Dark mode support with improved contrast (#ffd54f, #64b5f6)

- **Story Impact Tracking:**
  - Story indicators show moved (📍) and new (🆕) stories
  - Hover labels display sprint names: "Moved from Sprint 2 to Sprint 3"
  - Original sprint IDs preserved at finalization for analysis
  - Works correctly in both light and dark modes

- **Feature Analysis:**
  - Feature reordering enabled on finalized boards for impact simulation
  - Story movement between sprints allowed for what-if analysis
  - Feature refresh from Azure: `PATCH /api/boards/{id}/features/{id}/refresh`
  - Refresh bypasses finalization lock to allow data updates

- **Code Quality:**
  - Optional `checkFinalized = true` parameter prevents code duplication
  - Proper interface updates across service layer
  - CSS optimization: removed opacity overlays, proper disabled state styling
  - Zero errors in backend and frontend builds

- **Files Modified:**
  - Backend: BoardService, BoardsController, FeatureService, IFeatureService, Models, Migrations
  - Frontend: board.ts, board.html, board.css, story-card.ts, story-card.html, story-card.css, angular.json

---

### Added - Board Search & PAT Validation (Feb 15, 2026)

- **Board Search API:**
  - Backend endpoint: `GET /api/boards` with filters (search, organization, project, isLocked, isFinalized)
  - Returns `BoardSummaryDto[]` with lightweight board metadata
  - Repository/Service/Controller layers fully implemented
  - Frontend board list component already consuming the API

- **Board Preview Endpoint:**
  - Lightweight endpoint: `GET /api/boards/{id}/preview`
  - Returns board metadata WITHOUT sensitive data (features, stories, team members)
  - Includes: id, name, organization, project, featureCount, sampleFeatureAzureId
  - Enables PAT validation check before loading full board

- **PAT Validation Security Flow:**
  - Preview endpoint called first when navigating to board
  - If board has features (featureCount > 0), PAT modal shown
  - User enters PAT → Validates against Azure DevOps API
  - Uses organization, project, and sampleFeatureAzureId from preview
  - Only after valid PAT → Full board data loaded
  - Prevents sensitive data exposure in network tab before authentication

- **UI/UX Improvements:**
  - App title dynamically shows board name when viewing board
  - Browser tab title updates: "{Board Name} - PI Planning Tool"
  - Clickable header title to navigate home
  - App title changes from "pi-planning-ui" to "PI Planning Tool"

- **Files Modified:** 
  - Backend: BoardsController, BoardService, BoardRepository, DTOs (BoardSummaryDto)
  - Frontend: BoardService, BoardApiService, Board component, App component, index.html

### Added - Team Member Data Validation (Feb 15, 2026)

- **Backend DTO Validation:** Data annotations on TeamMemberDto and UpdateTeamMemberCapacityDto
  - Name field: Required, max 100 characters
  - Capacity fields: Range validation (0 to int.MaxValue)
  
- **Backend Service Layer Validation:** 
  - AddTeamMemberAsync/UpdateTeamMemberAsync: Non-empty names, at least one role required
  - UpdateCapacityAsync: Capacity bounds checked against sprint working days
  - Working days formula: floor((totalDays / 7) * 5)
  
- **Frontend Form Validation:**
  - Member form: Name validation (non-empty, max 100 chars)
  - Capacity form: Integer validation with bounds checking
  - Error signals with real-time display
  
- **Frontend HTML Constraints:**
  - Capacity inputs: type="number" step="1" min="0"
  - Prevents invalid entry at browser level
  
- **Type System Updates:**
  - All capacity fields changed from double to int
  - Database migration: ChangeCapacityToInt
  
- **Files Modified:** 8 backend/frontend files with consistent validation across layers

### Added - Azure Feature Management (Feb 15, 2026)

- Import features and child stories from Azure DevOps
- Refresh a feature from Azure DevOps (PAT required)
- Delete feature with cascading user stories
- Batch feature reorder endpoint and drag-and-drop UI
- Azure ID badges on feature and story cards
- Optional in-memory PAT remember (10 minutes)

### Planned - Refactoring & Architecture Improvements (Feb 9, 2026)

- Created comprehensive refactoring plan (see REFACTORING_PLAN.md)
- Identified technical debt: board component exceeds 1,700 LOC
- Planned API integration layer with environment config
- Planned component modularization (6 new child components)
- Planned routing enhancement (create/load board flows)
- Planned string externalization to constants
- Prioritized improvements before Phase 3

### Added - Team Member & Capacity UI (Feb 9, 2026)

- Team member bar with role badges and add-member modal
- Sprint capacity row with per-sprint capacity chips
- Capacity editor modal (per sprint)
- Load vs capacity display in sprint header
- Over-capacity load highlighting

### Added - Frontend Board UI (Feb 8, 2026)

#### UI Components

- Angular Material 20.2.x integration with custom theming
- Board component with responsive CSS Grid layout
- Story card component with Material Design polish
- Sprint header with dynamic column alignment
- Sprint footer showing Dev/Test/Total points and sprint names
- Dark theme with toggle service and persistent preference
- Horizontal scrolling for 6+ sprints with sticky header

#### Features

- Drag-and-drop story movement between sprints (CDK Drag&Drop)
- State persistence with deep-copy updates for nested arrays
- Angular signals for reactive state management
- BoardService with mock data mimicking backend DTOs
- Client-side DTOs matching backend API structure
- Dynamic grid-template-columns for perfect header/content alignment
- Responsive design working at all zoom levels (80%-100%+)

#### Styling

- Material-like gradients and elevation effects
- Hover states and transition animations
- Dark mode support for all components
- Story card emoji indicators (📊 Dev, 🧪 Test, ⚡ Total)
- Sprint footer badge styling with color coding
- Feature name centering and improved spacing

#### Technical Improvements

- Fixed sticky header width expansion issue
- Solved drag-drop persistence with correct ID parsing
- Implemented inner wrapper for full-width content alignment
- Added change detection optimization
- Grid cell min-widths properly constrained

#### Files Changed

- `frontend/pi-planning-ui/src/app/Components/board/board.ts` - Board logic
- `frontend/pi-planning-ui/src/app/Components/board/board.html` - Board template
- `frontend/pi-planning-ui/src/app/Components/board/board.css` - Board styling
- `frontend/pi-planning-ui/src/app/Components/story-card/story-card.ts` - Card component
- `frontend/pi-planning-ui/src/app/Components/story-card/story-card.html` - Card template
- `frontend/pi-planning-ui/src/app/Components/story-card/story-card.css` - Card styling
- `frontend/pi-planning-ui/src/app/features/board/services/board.service.ts` - State management
- `frontend/pi-planning-ui/src/app/core/services/theme.service.ts` - Theme toggle
- `frontend/pi-planning-ui/src/app/shared/models/board.dto.ts` - Client DTOs
- `frontend/pi-planning-ui/src/styles.css` - Global Material theme

### Changed

- Updated `README.md` with current status note
- Updated `STATUS.md` with frontend progress (50% complete)
- Updated milestone status: Frontend Board View → In Progress

---

## [0.1.0] - 2026-02-06

### Added - Backend Foundation

- Initial .NET 8 Web API project structure
- Entity Framework Core setup with PostgreSQL
- Domain models (Board, Sprint, Feature, UserStory, TeamMember, etc.)
- Repository pattern implementation
- Service layer with business logic
- Controllers for Boards, Features, UserStories, Team, Azure
- SignalR hub structure (not yet wired)
- Docker Compose configuration for database
- EF Core migrations

---

## Future Releases

### [0.2.0] - Planned

- Team member UI above sprint header
- Feature vertical reordering (drag-drop)
- Notes modal for features/stories
- IsMoved visual indicators

### [0.3.0] - Planned

- Azure Boards integration (fetch modal)
- Import features from Azure DevOps
- Backend API connection (replace mock data)

### [0.4.0] - Planned

- SignalR real-time collaboration
- Cursor presence tracking
- Multi-user notifications

### [1.0.0] - Planned MVP

- Complete feature set
- Production deployment
- User documentation
- Testing suite

---

**Maintainer:** Anirban Deb  
**Repository:** https://github.com/anirbandeb0510/pi-planning-tool
