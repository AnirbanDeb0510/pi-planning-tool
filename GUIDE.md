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

### ⚠️ IN PROGRESS

**Backend APIs**
- ✅ Board search with filters
- ✅ Board preview endpoint
- ✅ Organization/project mandatory validation
- ❌ Global exception middleware
- ❌ Comprehensive input validation
- ❌ Board lock/unlock endpoints
- ❌ Board finalization endpoints

**Frontend UI**
- ✅ Board list component with search
- ✅ PAT validation modal
- ❌ Board detail view (grid layout)
- ❌ Drag-drop wired to backend
- ❌ Team capacity visualization
- ❌ Real-time collaboration

**Real-Time**
- ❌ SignalR message handlers
- ❌ Broadcast logic
- ❌ Client-side listeners

---

## 🗺️ PROJECT ROADMAP (4 PHASES, ~12 WEEKS)

See **[ROADMAP_CURRENT.md](./ROADMAP_CURRENT.md)** for detailed priorities, estimates, and acceptance criteria.

### High-Level Phases

**PHASE 1: Backend API & Validation** (Current - 2 weeks)
- Global exception middleware
- Input validation & error handling  
- Board state endpoints (lock, unlock, finalize)

**PHASE 2: Board State Management** (Weeks 3-4)
- Board lock/unlock functionality
- Board finalization workflow
- State persistence & validation

**PHASE 3: Frontend UI & Components** (Weeks 5-8)
- Component modularization (reduce board.ts complexity)
- Real-time collaboration with SignalR
- Cursor presence & live updates

**PHASE 4: Polish & Deployment** (Weeks 9-12)
- Unit & E2E tests
- Performance optimization
- Documentation finalization
- Cloud deployment (Google Cloud Run)

---

## 🚀 PROGRESS UPDATE (February 17, 2026)

### ✅ Recently Completed
- Board search API with filters (organization, project, search, status)
- Board preview endpoint (secure, lightweight access)
- PAT validation security flow with modal
- Board list UI with search and filtering
- Mandatory organization & project parameters (frontend + API)
- [BindRequired] attribute for server-side validation
- Clean documentation (removed outdated docs)

### 🔄 Currently Working On
- **Branch:** `boardSearchFiltering` (Ready for PR)
- **Next:** Global exception middleware (blocking for validation layer)

---

## 🎯 NEXT IMMEDIATE ACTIONS (THIS SPRINT)

### Priority 1: Global Exception Middleware (Today)
```bash
# Add centralized error handling
# File: Middleware/GlobalExceptionHandlingMiddleware.cs
# Register in: Program.cs
# Benefit: Standard error responses, no info leakage
```

### Priority 2: Input Validation & Error Handling (Tomorrow-Next Day)
```bash
# Add data annotations to DTOs
# Add business rule validation to services
# Update all error responses for clarity
# Files: DTOs/*.cs, Services/Implementations/*.cs
```

### Priority 3: Board Lock/Unlock & Finalization (Next Week)
```bash
# Add board state management
# PATCH /api/boards/{id}/lock
# PATCH /api/boards/{id}/unlock
# PATCH /api/boards/{id}/finalize
# File: Controllers/BoardsController.cs
```

**Expected Result:** Solid, validated, production-ready backend API.

---

## 📚 KEY DOCUMENTS TO READ

1. **[ROADMAP_CURRENT.md](./ROADMAP_CURRENT.md)** ← Start here
   - Current priorities and next steps
   - Phase breakdown with time estimates
   - Dependency chain visualization
   - Success metrics and acceptance criteria

2. **[ARCHITECTURE.md](./ARCHITECTURE.md)** ← Reference while coding
   - System architecture diagram
   - Data flow examples (create board, import, move, lock)
   - Service layer patterns
   - Development workflow
   - Design decisions explained
   - Common gotchas

3. **[CONFIGURATION.md](./CONFIGURATION.md)** ← Setup & deployment
   - Docker runtime configuration
   - Environment variables
   - Local development setup
   - Production deployment

4. **[CHANGELOG.md](./CHANGELOG.md)** ← Track what's done
   - What's been completed
   - Feature history
   - Version notes

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

**Need guidance?** Check these in order:
1. ROADMAP_CURRENT.md - Current priorities & next steps
2. ARCHITECTURE.md - Patterns & examples
3. Existing code - Look for similar implementations
4. Azure DevOps docs - For Azure API questions

**Need to learn?**
- [EF Core docs](https://learn.microsoft.com/en-us/ef/core/) - Database patterns
- [Angular Material](https://material.angular.io/) - UI components
- [CDK Drag-Drop](https://material.angular.io/cdk/drag-drop/overview) - Drag logic
- [SignalR](https://learn.microsoft.com/en-us/aspnet/signalr/overview/) - Real-time
- [ASP.NET Core Middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware) - Exception handling

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

**Open [ROADMAP_CURRENT.md](./ROADMAP_CURRENT.md) — Start with Global Exception Middleware**

Clear priorities. One sprint at a time. Let's build. 🚀

