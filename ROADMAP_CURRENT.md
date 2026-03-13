# PI Planning Tool - Current Roadmap & Priorities

**Last Updated:** March 8, 2026  
**Current Status:** Phase 8 + Phase 10 COMPLETE ✅  
**Current Branch:** `main`  
**Next Phase:** Phase 11 - Automated Unit Testing

---

## 🚀 NEXT PRIORITIES (Ordered by Dependency & Impact)

**Immediate Next Steps:**

1. **Phase 11** - Automated Unit Testing (1-2 days)
2. **Phase 9** - Cloud/Hosting Deployment (1-2 days)

## ✅ COMPLETED PHASES (Summary)

### Phase 4 & 4.6 — Backend Refactoring + Code Quality

- Completed on Feb 22 and Feb 26, 2026
- Core backend refactoring, validation, logging, transactions, and code quality pass completed

### Phase 5 — IIS + SQL Server Support

- Completed on Feb 28, 2026
- Dual-provider support validated (PostgreSQL + SQL Server), deployment guide added

### Phase 5.5 — User Name Persistence + Guard

- Completed on Mar 1, 2026
- sessionStorage persistence + route guard flow delivered

### Phase 6 — SignalR Real-time Collaboration

- Completed on Mar 1, 2026
- Milestones A/B/C delivered and manually validated
- Production-ready: presence, cursor sync, and all mutation broadcasts

### Phase 7 — Board Lock/Unlock Feature

- Completed on Mar 2, 2026
- Password-based lock/unlock, real-time state sync, mutations blocked when locked

### Phase 8 — Documentation & Integration Testing

- Completed on Mar 7, 2026
- Documentation suite completed (README, architecture, API, deployment, security, user guide)
- Integration and workflow validation documented

### Phase 10 — Provider-Isolated EF Core Migrations

- Completed on Mar 3, 2026
- Dual-provider migrations architecture (PostgreSQL + SQL Server), assembly resolver wired

---

## 📋 UPCOMING PHASES

### PHASE 8: Documentation & Integration Testing — WRAP-UP

**Status:** Complete ✅  
**Estimated Time:** 3-4 hours  
**Depends On:** All other phases complete
**Why:** Ensure comprehensive documentation and real-world integration testing

#### Work Items:

- [x] **Architecture docs** (ERD, service flow, component hierarchy, SignalR flow)
- [x] **API docs** (endpoints, request/response examples, error handling, auth notes)
- [x] **Deployment docs** (Docker + PostgreSQL, IIS + SQL Server, SSL/HTTPS notes)
- [x] **User guide** (board lifecycle, planning flow, finalize/restore, lock/unlock)
- [x] **Code docs** (README refresh + key service behavior notes)
- [x] **Integration testing** (end-to-end board flow + multi-user scenarios)
- [x] **Performance check** (concurrency + query behavior + SignalR throughput)
- [x] **Security review** (input validation, CORS, PAT handling, auth surface)

#### Acceptance Criteria:

- [x] All features documented (user-facing)
- [x] All APIs documented (developer-facing)
- [x] Deployment guides complete (Docker + IIS)
- [x] Integration testing complete (all major workflows)
- [x] Performance benchmarks documented
- [x] Security audit completed with findings documented
- [x] README.md comprehensive and current
- [x] Zero issues blocking production deployment

---

### PHASE 11: Automated Unit Testing — NEW

**Status:** Not Started  
**Estimated Time:** 1-2 days  
**Depends On:** Phase 8, 10  
**Why:** Add repeatable automated checks for backend business logic and frontend core behaviors.

#### Testing Strategy (Decided)

- **Backend framework:** `xUnit` + `Moq` + `Microsoft.NET.Test.Sdk`
- **Frontend framework:** Existing Angular `Jasmine` + `Karma` stack (already configured)
- **Execution:**
  - Backend: `dotnet test`
  - Frontend: `npm test` / `ng test --watch=false --browsers=ChromeHeadless`

#### Fake Data / Test Data Approach

- Prefer **small in-memory builders/factories** per test file for DTO/model setup.
- Use **mocks/fakes** for external dependencies:
  - Backend repositories/services via `Moq`
  - Frontend HTTP via `HttpClientTestingModule` and `HttpTestingController`
- Avoid real database and Azure DevOps calls in unit tests.
- Keep deterministic test data (fixed IDs, dates, names) for reproducibility.

#### Phase 11 Work Items

- [ ] Create backend test project and add to solution
- [ ] Add backend unit tests for `ValidationService` and `SprintService`
- [ ] Add backend unit tests for one controller happy-path + validation/failure path
- [ ] Add frontend unit tests for `user.service.ts` and `user-name.guard.ts`
- [ ] Add frontend unit tests for `http-client.service.ts` error handling
- [ ] Add CI-ready test commands to README

#### Acceptance Criteria

- [ ] Backend tests run locally with `dotnet test`
- [ ] Frontend tests run locally with `ng test --watch=false`
- [ ] At least one test suite exists for backend services and frontend core services/guards
- [ ] Failing tests correctly fail CI/local command execution
- [ ] Test execution instructions documented in README

---

### PHASE 9: Cloud/Hosting Deployment — NEXT AFTER DOCS

**Status:** Not Started  
**Estimated Time:** 1-2 days  
**Depends On:** Phase 7, 8  
**Why:** Make the product publicly accessible in a stable hosted environment beyond local Docker/IIS setups

#### Scope:

- [ ] Select target hosting path (e.g., Azure App Service, AWS ECS/Fargate, or Render/Railway)
- [ ] Deploy backend API with environment-based configuration and secure secret handling
- [ ] Deploy frontend as static hosting (or reverse-proxy path) with runtime API base URL
- [ ] Provision managed database (PostgreSQL/SQL Server based on deployment choice)
- [ ] Configure domain, HTTPS/TLS, and CORS for hosted endpoints
- [ ] Add health checks, basic monitoring/logging, and restart policy
- [ ] Publish deployment runbook for repeatable releases

#### Acceptance Criteria:

- [ ] Frontend and backend are reachable via hosted URLs
- [ ] End-to-end board workflow works in hosted environment
- [ ] SignalR real-time events work across multiple external clients
- [ ] Secrets are not hard-coded and are managed via platform secret store
- [ ] HTTPS enabled with valid certificate
- [ ] Basic monitoring/log access available for troubleshooting

---

## 📋 DECISIONS & RESOLUTIONS

### Architecture decisions (locked)

- **Multi-provider architecture:** Provider-isolated migration assemblies (PostgreSQL + SQL Server) ✅
- **Board state model:** Finalized and locked are separate, independent states ✅
- **Real-time sync:** SignalR for presence, cursors, and mutation broadcasts ✅
- **Hosting pattern:** Cloud-first deployment with HTTPS and managed secrets

---
