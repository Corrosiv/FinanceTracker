# FinanceTracker Roadmap (Sprint-based)

This file organizes the V1 scope into short sprints with priorities, acceptance criteria, and rough estimates so we can iterate predictably.

---

Sprint 1 — MVP (P0)
- Goal: Deliver a runnable API with core data model, CRUD for categories & expenses, and CI.
- Acceptance criteria:
  - Database migrations run and local SQLite created.
  - Category and Expense CRUD endpoints documented in OpenAPI and covered by integration tests.
  - Minimal seed data available on first run.
- Tasks:
  - Implement EF Core models: `User` (placeholder), `Transaction`/`Expense`, `Category`, `Budget`, `Import` (4h)
  - CRUD endpoints for `Category` and `Expense` (controllers, DTOs, validators) (6h)
  - Configure `FinanceDbContext` and initial data seeding (2h)
  - Add Scalar / OpenAPI and basic examples (1h)
  - Add GitHub Actions: build + tests on PR (2h)
  - Unit tests for `UpdateAsync` (Category and Expense services) (1h)
  - Unit tests for validators (`CreateCategoryValidator`, `CreateExpenseValidator`) (1h)
  - Controller-level integration tests (HTTP status codes, routing, DTO mapping) (3h)
  - Edge-case tests (duplicate category names, invalid CategoryId on expense, empty/null inputs) (2h)

---

Sprint 2 — Imports & Deduplication (P0)
- Goal: Add CSV import pipeline and deduplication so users can ingest data reliably.
- Acceptance criteria:
  - Import endpoint accepts CSV, stores raw rows and normalized transactions.
  - Deduplication prevents duplicate transactions (composite key and optional SHA256 hash).
  - Unit tests cover common edge cases.
- Tasks:
  - CSV import endpoint with flexible column mapping and raw store (8h)
  - Normalization pipeline that maps CSV -> `Transaction` entities (4h) 
  - Deduplication strategy (composite unique constraint + optional SHA256 hash) and tests (4h)
  - Import validation/error reporting for malformed files (3h)
  - DB-level edge-case tests using SQLite in-memory (requires real provider, not EF InMemory) (2h)
    - Duplicate category name per user → `UNIQUE(UserId, Name)` returns 409/Conflict
    - Duplicate transaction (same user+date+amount+description) → `UNIQUE` constraint
    - Expense with non-existent `CategoryId` → FK violation handling
    - Add `UpdateExpenseValidator` to block zero amount and empty description on update path

---

Sprint 3 — Business Logic & Analytics (P1)
- Goal: Provide category assignment, budget tracking, analytics, and tips scaffolding.
- Acceptance criteria:
  - Category assignment service and endpoint exist and are test-covered.
  - Budgets can be created per category and period; system suggests limits based on spending patterns; overspend detection works.
  - Analytics endpoints return: spending per category (ranked), category trends (month-over-month), recurring charges, and income vs. expenses summary.
  - Tips endpoint is scaffolded with both raw-data and generated-tip options (final approach TBD).
- Tasks:
  - Category assignment service and endpoint (4h)
  - Budget model, CRUD, suggestion engine, and tracking per period (8h)
  - Analytics: spending per category with ranking (3h)
  - Analytics: category trends — month-over-month comparison (4h)
  - Analytics: recurring charges detection (4h)
  - Analytics: income vs. expenses summary and savings rate (2h)
  - Tips/recommendations endpoint — scaffold both options (raw data + generated strings) (3h)
  - Budget alerts endpoint (3h)

---

Sprint 4 — Hardening & Polish (P1)
- Goal: Stabilize V1, fill test gaps, add developer experience assets, and prepare for merge to `main`.
- Acceptance criteria:
  - `ExceptionHandlingMiddleware` has unit tests covering all mapped status codes (400, 404, 409, 500) and unhandled exception fallback.
  - Sample CSV files exist in `examples/` for manual and automated import testing.
  - API request/response examples documented in `examples/` for each endpoint group.
  - CI pipeline runs EF migrations check (verify migrations are up to date and apply cleanly).
  - DTO naming is consistent across all endpoint groups (Create/Update/Response pattern).
  - `UpdateExpenseValidator` exists and is tested (carried from Sprint 2 if not yet done).
  - All tests pass, build is clean, `dev` merged to `main` as V1.
- Tasks:
  - `ExceptionHandlingMiddleware` unit tests (2h)
  - Add sample CSV files to `examples/` (bank statement formats, edge cases) (1h)
  - Add API request/response examples per endpoint group in `examples/` (2h)
  - CI migration check — add `dotnet ef migrations` validation step to GitHub Actions (1h)
  - DTO audit and cleanup — ensure consistent Create/Update/Response naming across Category, Expense, Budget (2h)
  - `UpdateExpenseValidator` — implement and test if not completed in Sprint 2 (1h)
  - Final test pass — run full suite, fix any failures, review coverage gaps (2h)
  - Documentation review — ensure `README.md`, `API-SPEC.md`, `domain-model.md`, `system-overview.md` are accurate and up to date (1h)
  - Merge `dev` → `main` as V1 release (0.5h)

---

Backlog / Future (V2+)
- Multi-user support and authentication
- Automatic transaction categorization (ML)
- Multi-currency support and FX handling
- Real-time alerts/notifications and webhooks
- Budget history — track budget limit changes over time (start/end dates or versioning)
- Category assignment by description — bulk assign all transactions matching a normalized description to a category

Acceptance criteria template (use per task)
- Given X, When Y, Then Z — include endpoint, payload, expected DB state, and tests.
