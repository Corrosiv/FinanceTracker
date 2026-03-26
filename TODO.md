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
  - Add Swagger / OpenAPI and basic examples (1h)
  - Add GitHub Actions: build + tests on PR (2h)

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

---

Sprint 3 — Business Logic & Analytics (P1)
- Goal: Provide category assignment, budget tracking, and analytics endpoints.
- Acceptance criteria:
  - Category assignment service and endpoint exist and are test-covered.
  - Budgets can be created per category and period; overspend detection works.
  - Analytics endpoints return spending per category and budget alerts.
- Tasks:
  - Category assignment service and endpoint (4h)
  - Budget model, CRUD, and tracking per period (6h)
  - Analytics endpoints: spending per category, budget overspend alerts (6h)

---

Cross-cutting (ongoing)
- Add unit tests for services and integration tests for critical flows.
- Global error handling and logging (`ExceptionHandlingMiddleware`) and tests.
- Migration strategy and CI migration checks.
- Add sample CSVs and API request/response examples in `spec/` or `examples/` folder.

Backlog / Future (V2+)
- Multi-user support and authentication
- Automatic transaction categorization (ML)
- Multi-currency support and FX handling
- Real-time alerts/notifications and webhooks

Acceptance criteria template (use per task)
- Given X, When Y, Then Z — include endpoint, payload, expected DB state, and tests.