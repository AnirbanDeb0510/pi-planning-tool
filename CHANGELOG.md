# Changelog

All notable changes to the PI Planning Tool will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

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
