# FinanceTracker Roadmap (Sprint-based)

This file organizes the V1 scope into short sprints with priorities, acceptance criteria, and rough estimates so we can iterate predictably.

HARDENING CHECKLIST
Below are focused items that must be addressed before handing off or running the API in production. Each entry includes a one-character status bracket, target files, and clear acceptance criteria so they can be tracked from this document.

High priority (must address before handoff / production maintenance)
- [ ] User scoping & auth — `FinanceTracker.API/Controllers/CategoriesController.cs` (lines ~15	6) and all controllers
    - Acceptance: Replace hard-coded `DefaultUserId` with per-request user resolution (authentication middleware or injected `IUserContext`). Add `[Authorize]` to relevant controllers and tests that validate per-user isolation.
- [ ] Database constraint correctness vs tests — `FinanceTracker.API/Data/FinanceDbContext.cs`, `FinanceTracker.API/Migrations/*`, `tests/*`
    - Acceptance: Add SQLite-backed integration tests (in-memory/file) that verify composite unique constraints, FK violations, and deduplication behavior. Ensure migrations apply cleanly in CI.
- [ ] Deduplication & idempotent imports — `FinanceTracker.API/Services/ImportService.cs`, `FinanceTracker.API/Data/FinanceDbContext.cs`, `FinanceTracker.API/Migrations/*`, `tests/FinanceTracker.Tests/ImportServiceTests.cs`
    - Acceptance: Import flow handles DB-level unique constraint violations gracefully (mark duplicates / return 409 where appropriate). Add tests that simulate repeated imports and intra-batch duplicates and assert import record statuses and duplicate counts.
- [ ] Error handling & consistent JSON responses — `ExceptionHandlingMiddleware`, controllers
    - Acceptance: Middleware exists and maps exceptions to 400/404/409/500 with a consistent JSON schema. Add unit tests for middleware mappings and ensure validators return typed errors matching schema.

Medium priority (improve quality/readability/maintainability)
- [ ] DTO mapping & reduce repeated mapping code — `FinanceTracker.API/Controllers/*`
    - Acceptance: Centralized mapping (helper or AutoMapper) used across controllers; DTO shapes consistent and tests validate response shapes.
- [ ] Validation pipeline integration — validators referenced in this document and `Controllers`
    - Acceptance: Integrate validators via model binding / filter (e.g., FluentValidation) or a reusable filter so validation failures are returned with consistent error payloads.
- [ ] Null-safety / defensive checks in services — `FinanceTracker.API/Services/ITipsService.cs`, `TipsService.cs`
    - Acceptance: Services defend against null/empty analytics results and unit tests cover no-data scenarios.
- [ ] Make Tips behavior configurable — `TipsService` / `ITipsService`
    - Acceptance: Tips endpoint supports returning raw analytics only or generated strings (query param or request DTO). Avoid hard-coded currency symbols in service-level strings.

Low priority / nice-to-have
- [ ] CI migration & coverage checks — `.github/workflows/*`
    - Acceptance: CI includes a step that validates EF migrations apply cleanly (SQLite) and publishes test coverage.
- [ ] Add sample CSVs and examples — `examples/`
    - Acceptance: Representative CSV files used by tests are included in `examples/` and documented in `README.md`.
- [ ] Monetary formatting and localization plan
    - Acceptance: DTOs return numeric amounts; formatting is left to client or localized presentation layer.

Test-specific checklist
- [ ] Replace critical InMemory tests with SQLite provider for: unique composite index enforcement, FK violation behavior, and deduplication hash uniqueness.
- [ ] Keep quick InMemory tests for algorithmic logic and parsing, but validate DB constraints with real provider tests.

---

Sprint 1 — MVP (P0)
- Goal: Deliver a runnable API with core data model, CRUD for categories & expenses, and CI.
- Acceptance criteria:
  - [x] Database migrations run and local SQLite created. (P0)
  - [x] Category and Expense CRUD endpoints documented in OpenAPI and covered by integration tests. (P0)
  - [x] Minimal seed data available on first run. (P0)
- Tasks:
  - [x] Implement EF Core models: `User` (placeholder), `Transaction`/`Expense`, `Category`, `Budget`, `Import` (4h) (P0)
  - [x] CRUD endpoints for `Category` and `Expense` (controllers, DTOs, validators) (6h) (P0)
  - [x] Configure `FinanceDbContext` and initial data seeding (2h) (P0)
  - [x] Add Scalar / OpenAPI and basic examples (1h) (P1)
  - [x] Add GitHub Actions: build + tests on PR (2h) (P1)
  - [x] Unit tests for `UpdateAsync` (Category and Expense services) (1h) (P1)
  - [x] Unit tests for validators (`CreateCategoryValidator`, `CreateExpenseValidator`) (1h) (P1)
  - [x] Controller-level integration tests (HTTP status codes, routing, DTO mapping) (3h) (P0)
  - [x] Edge-case tests (duplicate category names, invalid CategoryId on expense, empty/null inputs) (2h) (P0)

---

Sprint 2 — Imports & Deduplication (P0)
- Goal: Add CSV import pipeline and deduplication so users can ingest data reliably.
- Acceptance criteria:
  - [x] Import endpoint accepts CSV, stores raw rows and normalized transactions. (P0)
  - [x] Deduplication prevents duplicate transactions (composite key and optional SHA256 hash). (P0)
  - [x] Unit tests cover common edge cases. (P1)
- Tasks:
  - [x] CSV import endpoint with flexible column mapping and raw store (8h) (P0)
  - [x] Normalization pipeline that maps CSV -> `Transaction` entities (4h) (P0)
  - [x] Deduplication strategy (composite unique constraint + optional SHA256 hash) and tests (4h) (P0)
  - [x] Import validation/error reporting for malformed files (3h) (P1)
  - [x] DB-level edge-case tests using SQLite in-memory (requires real provider, not EF InMemory) (2h) (P0)
    - [x] Duplicate category name per user → `UNIQUE(UserId, Name)` returns 409/Conflict
    - [x] Duplicate transaction (same user+date+amount+description) → `UNIQUE` constraint
    - [x] Expense with non-existent `CategoryId` → FK violation handling
    - [x] Add `UpdateExpenseValidator` to block zero amount and empty description on update path

---

Sprint 3 — Business Logic & Analytics (P1)
- Goal: Provide category assignment, budget tracking, analytics, and tips scaffolding.
- Acceptance criteria:
  - [x] Category assignment service and endpoint exist and are test-covered. (P1)
  - [x] Budgets can be created per category and period; system suggests limits based on spending patterns; overspend detection works. (P1)
  - [x] Analytics endpoints return: spending per category (ranked), category trends (month-over-month), recurring charges, and income vs. expenses summary. (P1)
  - [x] Tips endpoint is scaffolded with both raw-data and generated-tip options (final approach TBD). (P2)
- Tasks:
  - [x] Category assignment service and endpoint (4h) (P1)
  - [x] Budget model, CRUD, suggestion engine, and tracking per period (8h) (P1)
  - [x] Analytics: spending per category with ranking (3h) (P1)
  - [x] Analytics: category trends — month-over-month comparison (4h) (P1)
  - [x] Analytics: recurring charges detection (4h) (P1)
  - [x] Analytics: income vs. expenses summary and savings rate (2h) (P1)
  - [x] Tips/recommendations endpoint — scaffold both options (raw data + generated strings) (3h) (P2)
  - [x] Budget alerts endpoint (3h) (P1)

---

Sprint 4 — Hardening & Polish (P1)
- Goal: Stabilize V1, fill test gaps, add developer experience assets, and prepare for merge to `main`.
- Acceptance criteria:
  - [x] `ExceptionHandlingMiddleware` has unit tests covering all mapped status codes (400, 404, 409, 500) and unhandled exception fallback. (P1)
  - [x] Sample CSV files exist in `examples/` for manual and automated import testing. (P2)
  - [x] API request/response examples documented in `examples/` for each endpoint group. (P2)
  - [x] CI pipeline runs EF migrations check (verify migrations are up to date and apply cleanly). (P1)
  - [x] DTO naming is consistent across all endpoint groups (Create/Update/Response pattern). (P2)
  - [x] `UpdateExpenseValidator` exists and is tested (carried from Sprint 2 if not yet done). (P1)
  - [x] All tests pass, build is clean, `dev` merged to `main` as V1. (P1)
- Tasks:
  - [x] `ExceptionHandlingMiddleware` unit tests (2h) (P1)
  - [x] Add sample CSV files to `examples/` (bank statement formats, edge cases) (1h) (P2)
  - [x] Add API request/response examples per endpoint group in `examples/` (2h) (P2)
  - [x] CI migration check — add `dotnet ef migrations` validation step to GitHub Actions (1h) (P1)
  - [x] DTO audit and cleanup — ensure consistent Create/Update/Response naming across Category, Expense, Budget (2h) (P2)
  - [x] `UpdateExpenseValidator` — implement and test if not completed in Sprint 2 (1h) (P1)
  - [x] Final test pass — run full suite, fix any failures, review coverage gaps (2h) (P1)
  - [x] Documentation review — ensure `README.md`, `API-SPEC.md`, `domain-model.md`, `system-overview.md` are accurate and up to date (1h) (P2)
  - [x] Merge `dev` → `main` as V1 release (0.5h) (P1)

---

V2 roadmap

Below are concrete development steps, acceptance criteria and rough estimates to move backlog items toward a v2.0.0 release. Treat each feature as its own mini-sprint with unit + integration tests and migration considerations.

1) Multi-user support & authentication (P0)
   - Goal: Support multiple users with secure authentication and per-user data isolation.
   - Acceptance criteria:
     - [ ] Users can register, login, and receive JWT tokens for API access. (P0)
     - [ ] All transaction/category/budget data is scoped by `UserId` and verified in endpoints. (P0)
     - [ ] Tests cover auth flows and unauthorized access (401) and forbidden access (403) when accessing other users' data. (P0)
   - Tasks (estimates):
     - [ ] Add `User` identity model + migrations (2h) (P0)
     - [ ] Integrate ASP.NET Core Identity (or minimal JWT + user store) and endpoints (4h) (P0)
     - [ ] Add auth middleware and authorize attributes on controllers (2h) (P0)
     - [ ] Update Db unique constraints and queries to include `UserId` (2h) (P0)
     - [ ] Tests for multi-user isolation and auth flows (3h) (P0)

2) Automatic transaction categorization (ML) (P1)
   - Goal: Suggest categories for imported transactions using a repeatable, testable pipeline.
   - Acceptance criteria:
     - [ ] Import pipeline produces a recommended `CategoryId` and confidence score for transactions. (P1)
     - [ ] Admin/scoped endpoint exists to review and accept/reject suggestions (manual confirmation path). (P1)
     - [ ] Unit tests cover mapping rules and a lightweight ML model evaluation harness. (P1)
   - Tasks (estimates):
     - [ ] Implement normalization pipeline that extracts features from description/date/amount (4h) (P1)
     - [ ] Add a rule-based fallback classifier and a pluggable ML scorer interface (4h) (P1)
     - [ ] Implement a simple persisted training set and offline trainer (CSV export/import) (6h) (P2)
     - [ ] Endpoint to surface suggestions and accept overrides; record accepted training examples (3h) (P2)
     - [ ] Tests for classification pipeline and end-to-end import -> suggestion flow (3h) (P1)
   - Notes: Start with deterministic rules + lightweight model (Naive Bayes / logistic regression) to avoid heavy infra. Keep ML components pluggable for future cloud model replacement.

3) Multi-currency support & FX handling (P1)
   - Goal: Store currency and amounts, show converted values, and support simple FX updates.
   - Acceptance criteria:
     - [ ] `Transaction`/`Expense` stores `Currency` and `Amount`; API can return base-currency converted values. (P1)
     - [ ] Admin or background job can fetch FX rates and store them with timestamps; conversions use nearest available rate. (P1)
   - Tasks (estimates):
     - [ ] Schema changes: add `Currency` to transactions and budgets; migrations (2h) (P1)
     - [ ] Add FX table and a background service to refresh rates from a provider (3rd party) or seeded fixtures (4h) (P1)
     - [ ] Update response DTOs to include `Currency` and `ConvertedAmount` (2h) (P1)
     - [ ] Tests for conversion logic and FX refresh handling (2h) (P1)

4) Real-time alerts, notifications & webhooks (P2)
   - Goal: Notify users of budget breaches, recurring charge detection, and import completion via SignalR and webhooks.
   - Acceptance criteria:
     - [ ] Webhook endpoint management for user callbacks; deliver retry semantics for failed deliveries. (P2)
     - [ ] SignalR hub to push real-time alerts to connected clients. (P2)
     - [ ] Background jobs queue alerts and use both webhook and SignalR transports. (P2)
   - Tasks (estimates):
     - [ ] Add `Alert` and `WebhookSubscription` entities and migrations (3h) (P2)
     - [ ] Implement SignalR hub and basic client auth (3h) (P2)
     - [ ] Implement webhook dispatcher with retry/backoff (4h) (P2)
     - [ ] Wire alerts from budget overspend and recurring detection features (3h) (P2)
     - [ ] Tests for alert queuing, delivery, and failure/retry paths (4h) (P2)

5) Budget history & versioning (P2)
   - Goal: Track budget changes over time (start/end dates or versioning) so past analytics remain accurate.
   - Acceptance criteria:
     - [ ] Budgets have an effective period or version; historical budgets are kept for reporting. (P2)
     - [ ] Analytics queries respect budget effective dates when computing alerts and spend vs. budget. (P2)
   - Tasks (estimates):
     - [ ] Add budget versioning fields (`EffectiveFrom`, `EffectiveTo`, `Version`) and migrations (2h) (P2)
     - [ ] Update budget CRUD to create versions rather than overwrite by default (3h) (P2)
     - [ ] Update analytics and alerts to use budget periods (3h) (P2)
     - [ ] Tests for versioned budgets in analytics and alerting (2h) (P2)

6) Category assignment by description (P2)
   - Goal: Allow bulk assignment of transactions to a category by matching normalized descriptions.
   - Acceptance criteria:
     - [ ] Admin endpoint to define a mapping rule (pattern -> CategoryId) and apply it to historical transactions. (P2)
     - [ ] Rules persist and are used by the import/ML pipeline as high-confidence suggestions. (P2)
   - Tasks (estimates):
     - [ ] Add `CategoryAssignmentRule` entity and CRUD endpoints (3h) (P2)
     - [ ] Implement normalization function and bulk apply job with dry-run option (3h) (P2)
     - [ ] Tests for rule matching and bulk-apply idempotency (2h) (P2)

General notes and next steps
 - [ ] For each feature, add migrations and keep the EF provider compatibility with SQLite for tests. Use real provider tests for DB constraints where possible.
 - [ ] Add small integration tests (SQLite file-based) for cross-feature scenarios (multi-user + imports + budgets).
 - [ ] Iterate: implement rule-based behavior first, then augment with ML and background services as needed.

Acceptance criteria template (use per task)
 - Given X, When Y, Then Z — include endpoint, payload, expected DB state, and tests.
