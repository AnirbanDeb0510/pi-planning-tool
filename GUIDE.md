# PI Planning Tool - Project Summary & Guidance

**Created:** February 6, 2026  
**Project Lead:** Anirban Deb

---

## 🎯 EXECUTIVE SUMMARY

The PI Planning Tool is a **collaborative board planning application** (inspired by Miro/Mural) that enables Agile teams to plan Program Increments (PIs) with real-time multi-user support, Azure DevOps integration, and capacity management.

**Current Status:** ~30% complete  
**Architecture:** Clean, well-designed, scalable  
**Blockers:** None; ready to build

---

## ✨ WHAT YOU'VE DONE WELL

1. **Architecture First** - Designed domain model before coding
2. **No Technical Debt** - EF Core relationships resolved cleanly
3. **Layered Design** - Controllers → Services → Repositories → DbContext
4. **Placeholder Pattern** - Smart UX: imported items don't auto-distribute
5. **Move Tracking** - OriginalSprintId vs SprintId enables rich UX
6. **Service-Centric** - Business logic centralized, not scattered
7. **Docker Ready** - Full containerization with docker-compose
8. **Azure First** - Integration planned from day one

---

## 📋 CURRENT STATE

### ✅ COMPLETED

**Backend (70% feature-complete)**
- Domain model fully defined
- Database migrations stable
- 80% of services implemented
- 90% of repositories implemented
- Core APIs for import/move/refresh working
- Azure integration service working
- Team management endpoints working
- DI fully configured
- Swagger enabled

**Frontend (10% started)**
- Angular 20 setup
- Material Design configured
- Basic component structure
- Models defined
- Service skeleton exists

**Infrastructure**
- PostgreSQL containerized
- Docker Compose fully configured
- Auto-migration on startup
- CORS enabled
- SignalR registered (not wired)

### ⚠️ INCOMPLETE

**Backend APIs**
- ✅ Complete board fetch (tested)
- ❌ Board lock/unlock endpoints
- ❌ Finalization logic exposed
- ❌ Global exception handling (basic middleware present, needs refinement)
- ❌ Input validation
- ❌ Request logging

**Frontend UI**
- ❌ No styling applied
- ❌ No Material components in use
- ❌ No data binding to backend
- ❌ No drag-drop wired
- ❌ No Azure fetch UI
- ❌ No real-time display

**Real-Time**
- ❌ SignalR message handlers
- ❌ Broadcast logic
- ❌ Client-side listeners

---

## 🗺️ YOUR ROADMAP (4 PHASES, ~8 WEEKS)

### PHASE 1: Backend API Completion (2 weeks)
**Goal:** All backend endpoints production-ready so frontend can consume them

**Key Deliverables:**
1. **Complete Board Fetch** - `GET /api/boards/{id}` returns full hierarchy
2. **Exception Handling** - Global middleware for standardized errors
3. **Validation** - Input validation on all endpoints
4. **Documentation** - Swagger fully documented
5. **Status:** `🔴 NOT STARTED`

**Why First?** Frontend is blocked. Can't build UI without data APIs.

### PHASE 2: Frontend Board View (2-3 weeks)
**Goal:** Functional, styled board where users see sprints, features, stories

**Key Deliverables:**
1. **Board Layout** - Features × Sprints grid with Material Design
2. **Data Binding** - Fetch board from backend, render stories
3. **Drag-Drop** - Drag stories to different sprints (horizontal)
4. **Drag-Drop** - Drag features to reorder (vertical)
5. **Team Panel** - Show team members and their capacity
6. **Status:** `🔴 NOT STARTED`

**Why Second?** Backend drives frontend. Parallel is OK but API-first is safer.

### PHASE 3: Azure & Real-Time (2 weeks)
**Goal:** Fetch features from Azure DevOps, multi-user collaboration with SignalR

**Key Deliverables:**
1. **Azure Fetch Modal** - Form to fetch feature by ID
2. **Import Flow** - Click "Add to Board", feature appears in placeholder
3. **Real-Time Sync** - SignalR broadcasts moves to other clients
4. **Cursor Presence** - Show who's looking at what
5. **Status:** `🔴 NOT STARTED`

**Why Third?** UI must work before adding real-time complexity.

### PHASE 4: Polish & Deployment (1 week)
**Goal:** Production-ready with tests, docs, cloud deployment

**Key Deliverables:**
1. **Unit Tests** - Services & components
2. **E2E Tests** - Full user workflows
3. **Documentation** - README, API docs, dev guide
4. **Performance** - Virtual scrolling, lazy loading if needed
5. **Cloud Deploy** - Google Cloud Run setup
6. **Status:** `🔴 NOT STARTED`

---

## 🚀 NEXT IMMEDIATE ACTIONS (THIS WEEK)

### Day 1-2: Backend Board Fetch
**DO THIS FIRST** - Everything else depends on it

```bash
# Step 1: Create BoardResponseDto
# File: DTOs/BoardResponseDto.cs
# Returns: Board with sprints, features (with children), team members

# Step 2: Update repository
# File: Repositories/Implementations/BoardRepository.cs
# Add: GetBoardWithFullHierarchyAsync() with eager loading

# Step 3: Update service
# File: Services/Implementations/BoardService.cs
# Add: GetBoardWithHierarchyAsync() - map to DTO

# Step 4: Update controller
# File: Controllers/BoardsController.cs
# Change: GET /api/boards/{id} to use new service method

# Step 5: Test
dotnet run
# Visit: http://localhost:5000/swagger
# Try: GET /api/boards/1
```

### Day 3-4: Controller Routing & Exception Handler
```bash
# Fix inconsistent routing
# Fix: BoardsController vs BoardController

# Add global exception handling
# File: Middleware/GlobalExceptionHandlingMiddleware.cs
# Register in Program.cs
```

### Day 5: Validation & Testing
```bash
# Add input validation
# Test all endpoints with Postman/Swagger
# Document API response schemas
```

**Expected Result:** Frontend team can start building immediately.

---

## 📚 FOUR KEY DOCUMENTS TO READ

1. **[NEXT_STEPS.md](./NEXT_STEPS.md)** ← Start here
   - Detailed Week 1 tasks with code samples
   - Exact file locations and implementations
   - Quick checklist format

2. **[PROJECT_ROADMAP.md](./PROJECT_ROADMAP.md)** ← Review this
   - 4-phase high-level plan
   - Work breakdown by phase/week
   - Architecture patterns explained
   - 2000+ line comprehensive guide

3. **[ARCHITECTURE.md](./ARCHITECTURE.md)** ← Reference this
   - System architecture diagram
   - Data flow examples (create board, import, move, lock)
   - Service layer patterns
   - Development workflow
   - Design decisions explained
   - Common gotchas

4. **[STATUS.md](./STATUS.md)** ← Track your progress here
   - Current implementation status
   - Phase-by-phase checklists
   - Milestone tracking
   - Weekly check-in template
   - Risk register

---

## 🏆 KEY ARCHITECTURAL PRINCIPLES

1. **Service Layer is Authoritative** - All business logic lives here, never in controllers
2. **Repositories are Thin** - Only queries + persistence, no decisions
3. **DTOs for APIs** - Decouple HTTP contracts from entities
4. **Eager Load Everything** - For board fetch, get it all in one query
5. **Backend First** - Get APIs rock-solid before frontend touches them
6. **Placeholder Pattern** - Imported items go to Sprint 0 (parking lot)
7. **Move Tracking** - OriginalSprintId vs SprintId enables rich analytics
8. **SignalR Mirrors State** - Real-time broadcasts user actions, but backend is source of truth

---

## 🎯 FOCUS AREAS FOR WEEK 1

### Backend (80% of effort)
- [x] Review existing code (you just did!)
- [ ] Implement board fetch hierarchy
- [ ] Fix routing inconsistencies  
- [ ] Add exception handling
- [ ] Write unit tests for services
- [ ] Document API thoroughly

### Frontend (20% of effort)
- [ ] Understand Material Design layout patterns
- [ ] Review CDK drag-drop examples
- [ ] Plan component structure
- [ ] Don't start building yet - wait for backend APIs

---

## 💡 KEY INSIGHTS

**Why Backend First?**
- Frontend can't work without API contracts
- Backend APIs are easier to iterate
- Database is the source of truth
- Sign-off on data model is critical

**Why Board Fetch is Critical?**
- Every feature depends on it
- Unblocks entire frontend team
- Tests eager loading patterns
- Foundation for real-time sync

**Why Placeholder Sprint?**
- Users explore without commitment
- Prevents accidental capacity overruns
- Clean UX: explicit "move to sprint" action
- Aligns with Miro/Mural patterns

**Why Service-Centric?**
- Testable without database
- Reusable across controllers
- Business rules in one place
- Easy to find bugs

---

## ⚡ QUICK REFERENCE

| Component | File | Status | Next Action |
|-----------|------|--------|-------------|
| Board Model | Models/Board.cs | ✅ Complete | Use as-is |
| Board Service | Services/BoardService.cs | ⚠️ Partial | Add hierarchy fetch |
| Board Repository | Repositories/BoardRepository.cs | ⚠️ Partial | Add eager loading |
| Board Controller | Controllers/BoardsController.cs | ⚠️ Partial | Fix routing, complete GET |
| Feature Service | Services/FeatureService.cs | ✅ Complete | Use as-is |
| Frontend Board | Components/board/board.ts | 🔴 Empty | Wait for backend APIs |

---

## 🚨 CRITICAL SUCCESS FACTORS

1. **Complete board fetch by Feb 15** - Unblocks frontend
2. **Get foundations right** - Don't rush architecture
3. **Test thoroughly** - Edge cases bite later
4. **Document APIs** - Swagger is your friend
5. **Keep scope tight** - MVP first, features later
6. **Communicate status** - Weekly updates

---

## ❓ FREQUENTLY ASKED QUESTIONS

**Q: Should I start frontend now?**  
A: No. Wait for `GET /api/boards/{id}` to be working. Better to align on data shapes first.

**Q: How do I test the API without frontend?**  
A: Use Swagger UI (`http://localhost:5000/swagger`) or Postman.

**Q: Should I add authentication now?**  
A: Not yet. Get MVP working first, add auth in next iteration.

**Q: How do I handle large boards (1000+ stories)?**  
A: Implement later. Optimize when you have real data.

**Q: Do I need to worry about migrations during dev?**  
A: No. Remove old migrations, start fresh with `InitialCreate`. DB auto-migrates.

**Q: Should I use SignalR for persistence?**  
A: No! SignalR only broadcasts events. Backend is the source of truth. Refresh from DB if unsure.

**Q: How do I debug database issues?**  
A: `docker exec -it pi-postgres psql -U postgres -d PIPlanningDB` then `SELECT * FROM "UserStories";`

**Q: What's the deal with Sprint 0?**  
A: It's the placeholder/parking lot. Imported features land here. Users manually move to real sprints.

---

## 📞 SUPPORT & RESOURCES

**Stuck?** Check these in order:
1. NEXT_STEPS.md - Code examples for what you're doing
2. ARCHITECTURE.md - Patterns & examples
3. Existing code - Look for similar implementations
4. Azure DevOps docs - For Azure API questions

**Need to learn?**
- [EF Core docs](https://learn.microsoft.com/en-us/ef/core/) - Database patterns
- [Angular Material](https://material.angular.io/) - UI components
- [CDK Drag-Drop](https://material.angular.io/cdk/drag-drop/overview) - Drag logic
- [SignalR](https://learn.microsoft.com/en-us/aspnet/signalr/overview/) - Real-time

---

## 🎉 YOU'RE IN A GREAT POSITION

✅ Domain model is solid  
✅ Architecture is clean  
✅ No technical debt  
✅ Infrastructure works  
✅ Team knows patterns  
✅ You have 8 weeks to an MVP  

**You're not starting from scratch. You're building the UI on a rock-solid foundation.**

---

## 📍 YOUR NEXT MOVE

**Open [NEXT_STEPS.md](./NEXT_STEPS.md) and start with "Day 1-2: Backend Board Fetch"**

That's it. One clear task. Get board fetch working. Everything else follows.

Good luck! 🚀

