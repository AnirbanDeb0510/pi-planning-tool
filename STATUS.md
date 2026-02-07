# PI Planning Tool - Development Status & Checklist

**Status:** Phase 1 - Backend API Completion  
**Last Updated:** February 6, 2026  
**Team Lead:** Anirban Deb

---

## 📊 CURRENT IMPLEMENTATION STATUS

### 🟢 COMPLETE (Production Ready)

#### Backend Models & Database

- [x] Domain model fully designed (Board, Sprint, Feature, UserStory, TeamMember, TeamMemberSprint, CursorPresence)
- [x] EF Core DbContext configured with foreign key relationships
- [x] Initial migration (`InitialCreate`) applied successfully
- [x] PostgreSQL containerized with Docker Compose
- [x] Database auto-migrates on application startup
- [x] Password hashing utility implemented

#### Backend DI & Infrastructure

- [x] Dependency Injection configured (Controllers, Services, Repositories)
- [x] DbContext registered as scoped service
- [x] CORS enabled for development
- [x] SignalR registered (but not wired)
- [x] Swagger/OpenAPI enabled
- [x] HttpClient configured for Azure integration

#### Backend Services Implemented

- [x] `IBoardService.CreateBoardAsync()` - Create board with auto-generated sprints
- [x] `IBoardService.GetBoardAsync()` - Get board (basic, no hierarchy)
- [x] `IFeatureService.ImportFeatureToBoardAsync()` - Import feature + stories to placeholder
- [x] `IFeatureService.MoveUserStoryAsync()` - Move story between sprints
- [x] `IFeatureService.ReorderFeatureAsync()` - Reorder feature by priority
- [x] `IFeatureService.RefreshFeatureFromAzureAsync()` - Refresh feature data from Azure
- [x] `IFeatureService.RefreshUserStoryFromAzureAsync()` - Refresh story data from Azure
- [x] `ITeamService.GetTeamAsync()` - Get team members
- [x] `ITeamService.AddOrUpdateTeamAsync()` - Add/update team members
- [x] `ITeamService.UpdateCapacityAsync()` - Update capacity per sprint/person
- [x] `IAzureBoardsService.GetFeatureWithChildrenAsync()` - Fetch from Azure DevOps API

#### Backend Repositories Implemented

- [x] `IBoardRepository` - Board CRUD + queries
- [x] `IFeatureRepository` - Feature CRUD + queries
- [x] `IUserStoryRepository` - Story CRUD + queries
- [x] `ITeamRepository` - Team CRUD + queries

#### Backend Controllers Implemented

- [x] `BoardsController` - Create board (POST /api/boards)
- [x] `FeaturesController` - Import, refresh, reorder features
- [x] `UserStoriesController` - Move, refresh stories
- [x] `TeamController` - Get, add, update team
- [x] `AzureController` - Fetch from Azure (if exists)

---

### 🟡 IN PROGRESS (Actively Being Built)

#### Backend API Completion (Week 1-2)

- [x] **Priority 1: Complete Board Fetch**
  - [x] Create `BoardResponseDto` class
  - [x] Implement `GetBoardWithFullHierarchyAsync()` in repository
  - [x] Implement `GetBoardWithHierarchyAsync()` in service
  - [x] Update `GET /api/boards/{id}` endpoint
  - [x] Test with Swagger (local)
  - **Status:** COMPLETED (locally tested)

- [ ] **Priority 2: Fix Controller Routing**
  - [ ] Review `BoardsController` routing
  - [ ] Ensure consistency (all plural or all singular)
  - **Status:** NOT STARTED

- [ ] **Priority 3: Global Exception Handler**
  - [ ] Create `GlobalExceptionHandlingMiddleware`
  - [ ] Register in `Program.cs`
  - [ ] Test error responses
  - **Status:** NOT STARTED

#### Frontend Foundation (Week 2-3)

- [ ] Board component with Material Design layout
- [ ] Service layer for API communication
- [ ] Basic drag-drop for stories
- [ ] Team member UI
- **Status:** Components exist but no styling/data binding

---

### 🔴 NOT STARTED (On Roadmap)

#### Backend Advanced Features

- [ ] Board lock/unlock endpoints
- [ ] Board finalization (visual mode)
- [ ] Capacity calculations & load visualization
- [ ] Input validation & standardized errors
- [ ] Request/response logging
- [ ] API authentication/authorization
- [ ] Rate limiting

#### SignalR & Real-Time

- [ ] Implement `FeatureMoved` event handler
- [ ] Implement `StoryMoved` event handler
- [ ] Implement cursor tracking/presence
- [ ] Frontend SignalR integration
- [ ] Broadcast user actions to other clients

#### Frontend UI/UX

- [ ] Board grid layout (features × sprints)
- [ ] Story card component with Material Design
- [ ] Azure fetch modal
- [ ] Team capacity visualization
- [ ] Notes modal for features/stories
- [ ] Move story drag-drop (horizontal)
- [ ] Reorder feature drag-drop (vertical)
- [ ] Real-time cursor presence indicators
- [ ] Toast notifications for remote updates
- [ ] Styling & responsive design

#### Testing & Documentation

- [ ] Unit tests (backend services)
- [ ] Unit tests (frontend components)
- [ ] Integration tests (API endpoints)
- [ ] E2E tests (user workflows)
- [ ] API documentation (Swagger enhancements)
- [ ] Frontend component docs
- [ ] Development guide (README updates)

#### Deployment & DevOps

- [ ] Google Cloud Run setup
- [ ] Environment configuration (prod/dev/staging)
- [ ] CI/CD pipeline (GitHub Actions)
- [ ] Database backup/restore plan
- [ ] Monitoring & logging

---

## 📝 PHASE-BY-PHASE CHECKLIST

### PHASE 1: Backend API Completion (Target: 2 weeks)

#### Week 1: Core APIs

- [ ] Day 1-2: Implement board fetch hierarchy
  ```
  [ ] Create BoardResponseDto
  [ ] Implement repository eager loading
  [ ] Implement service method
  [ ] Test GET /api/boards/{id}
  ```
- [ ] Day 3: Fix routing issues
  ```
  [ ] Audit all controller routes
  [ ] Ensure consistency
  [ ] Update documentation
  ```
- [ ] Day 4-5: Exception handling
  ```
  [ ] Create middleware
  [ ] Register in DI
  [ ] Test error scenarios
  ```

#### Week 2: Validation & SignalR Prep

- [ ] Day 1-2: Input validation
  ```
  [ ] Add validation attributes/fluent validation
  [ ] Test invalid inputs
  [ ] Standardize error responses
  ```
- [ ] Day 3-4: Logging & monitoring
  ```
  [ ] Implement request logging
  [ ] Add exception logging
  [ ] Test log output
  ```
- [ ] Day 5: SignalR setup (non-functional)
  ```
  [ ] Wire PlanningHub methods
  [ ] Add connection handlers
  [ ] Prepare for Phase 3
  ```

#### Week 2+ (Overflow)

- [ ] Board lock/unlock endpoints
- [ ] Capacity calculation helpers
- [ ] Advanced repository queries

---

### PHASE 2: Frontend Board View (Target: 2-3 weeks)

#### Week 1: Layout & Styling

- [ ] Day 1-2: Material Design setup
  ```
  [ ] Import Material modules
  [ ] Set up global styles
  [ ] Create shared component library
  ```
- [ ] Day 3-5: Board layout
  ```
  [ ] Feature sidebar (list)
  [ ] Sprint header (list)
  [ ] Story grid (features × sprints)
  [ ] Responsive design
  ```

#### Week 2: Data Binding & Interactions

- [ ] Day 1-2: API integration
  ```
  [ ] Create board service
  [ ] Fetch board data
  [ ] Handle loading/error states
  ```
- [ ] Day 3-5: Drag-drop
  ```
  [ ] CDK drag-drop setup
  [ ] Story card dragging (horizontal)
  [ ] Feature reordering (vertical)
  [ ] Call move APIs on drop
  ```

#### Week 3: Team & Features

- [ ] Day 1-2: Team panel
  ```
  [ ] Display team members
  [ ] Add/remove UI
  [ ] Capacity per sprint
  ```
- [ ] Day 3-5: Notes & finalization
  ```
  [ ] Notes modal
  [ ] IsMoved visual indicator
  [ ] Finalized mode
  ```

---

### PHASE 3: Azure & Real-time (Target: 2 weeks)

#### Week 1: Azure Integration

- [ ] Days 1-3: Fetch modal
  ```
  [ ] Build modal component
  [ ] Form inputs (org, project, feature ID, PAT)
  [ ] Fetch button → API call
  [ ] Preview fetched feature
  ```
- [ ] Days 4-5: Import flow
  ```
  [ ] Add to board button
  [ ] Call import endpoint
  [ ] Refresh board view
  ```

#### Week 2: Real-time

- [ ] Days 1-3: SignalR frontend
  ```
  [ ] Connect to hub
  [ ] Listen to events
  [ ] Broadcast local changes
  ```
- [ ] Days 4-5: Presence & notifications
  ```
  [ ] Cursor presence UI
  [ ] Toast notifications
  [ ] User activity feed
  ```

---

### PHASE 4: Polish & Deployment (Target: 1 week)

- [ ] Unit tests (backend)
- [ ] Unit tests (frontend)
- [ ] E2E tests
- [ ] Performance optimization
- [ ] Documentation updates
- [ ] Cloud deployment (Google Cloud Run)
- [ ] Team training & handoff

---

## 🎯 MILESTONE TARGETS

| Milestone            | Target Date      | Status         |
| -------------------- | ---------------- | -------------- |
| Backend API complete | Feb 20, 2026     | 🔴 Not started |
| Frontend board view  | Mar 5, 2026      | 🔴 Not started |
| Azure integration    | Mar 12, 2026     | 🔴 Not started |
| Real-time features   | Mar 19, 2026     | 🔴 Not started |
| Testing & polish     | Mar 26, 2026     | 🔴 Not started |
| **MVP Launch**       | **Mar 31, 2026** | 🔴 Not started |

---

## 📊 EFFORT ESTIMATION

| Component         | Estimate      | Status   |
| ----------------- | ------------- | -------- |
| Backend APIs      | 80 hours      | 30% done |
| Frontend UI       | 100 hours     | 5% done  |
| Azure Integration | 30 hours      | 0% done  |
| SignalR/Real-time | 50 hours      | 0% done  |
| Testing           | 60 hours      | 0% done  |
| Deployment        | 20 hours      | 0% done  |
| **Total**         | **340 hours** | **~10%** |

**Assumptions:**

- 1 full-time developer
- 40 hours/week effective dev time
- No blocking issues
- Scope doesn't expand

---

## 🚨 BLOCKERS & RISKS

| Risk                               | Severity  | Mitigation                                           |
| ---------------------------------- | --------- | ---------------------------------------------------- |
| Unclear Azure API response format  | 🟠 Medium | Test with actual Azure project early                 |
| SignalR connection stability       | 🟠 Medium | Start with local testing, use retry logic            |
| Performance with large boards      | 🟠 Medium | Profile early, implement virtual scrolling if needed |
| Angular Material version conflicts | 🟡 Low    | Lock dependencies in package.json                    |
| DB migration issues in prod        | 🟠 Medium | Test migrations on Docker before production          |

---

## ✅ DEFINITION OF DONE

A feature is "done" when:

1. **Code Complete**
   - [ ] All acceptance criteria met
   - [ ] Code reviewed & approved
   - [ ] No TODO/FIXME comments

2. **Tested**
   - [ ] Unit tests pass
   - [ ] Integration tests pass
   - [ ] Manual testing verified
   - [ ] Edge cases handled

3. **Documented**
   - [ ] API endpoints documented
   - [ ] In-code comments for complex logic
   - [ ] README/guide updated if applicable

4. **Deployed**
   - [ ] Works locally
   - [ ] Works in Docker
   - [ ] Prepared for production

---

## 📅 WEEKLY CHECK-INS

### Week of Feb 10, 2026

- [ ] Backend APIs complete
- [ ] API documentation updated
- [ ] Frontend team unblocked

### Week of Feb 17, 2026

- [ ] Frontend board view working
- [ ] Drag-drop functional
- [ ] Team panel complete

### Week of Feb 24, 2026

- [ ] Azure fetch modal done
- [ ] Import flow tested
- [ ] Basic real-time working

### Week of Mar 3, 2026

- [ ] Full real-time integration
- [ ] Cursor presence visible
- [ ] UI polish & accessibility

### Week of Mar 10, 2026

- [ ] All tests written
- [ ] Performance validated
- [ ] Deployment pipeline ready

### Week of Mar 17, 2026

- [ ] Cloud deployment working
- [ ] User documentation ready
- [ ] Team training complete

### Week of Mar 24, 2026

- [ ] Bug fixes & final polish
- [ ] Production readiness check
- [ ] **Go/no-go decision**

---

## 🔗 QUICK LINKS

- [PROJECT_ROADMAP.md](./PROJECT_ROADMAP.md) - Detailed roadmap & architecture
- [NEXT_STEPS.md](./NEXT_STEPS.md) - Immediate next actions
- [ARCHITECTURE.md](./ARCHITECTURE.md) - Technical architecture & patterns
- [README.md](./README.md) - Project overview & setup
- [DBSchema.mmd](./DBSchema.mmd) - Database ER diagram

---

## 📞 CONTACTS & ESCALATIONS

**Project Lead:** Anirban Deb  
**Tech Stack Questions:** Refer to ARCHITECTURE.md  
**Stuck on Implementation:** Review similar pattern in codebase  
**Azure API Issues:** Check Azure DevOps REST API docs  
**Environment Issues:** Check docker-compose.yml

---

**Last Updated:** February 6, 2026  
**Next Update:** After first sprint completion
