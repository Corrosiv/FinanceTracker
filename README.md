# 💰 FinanceTracker API

> A personal finance REST API that imports bank transactions from CSV files, categorizes spending, tracks budgets, and generates financial insights — built end-to-end as a portfolio project.

**Tech stack:** ASP.NET Core · EF Core · SQLite · xUnit · GitHub Actions · OpenAPI (Scalar)

---

## Overview

- **CSV Import Pipeline** — upload bank statements from any format; flexible column mapping, raw row storage, and SHA-256 deduplication
- **Budget Tracking** — per-category spending limits with pattern-based suggestions and overspend alerts
- **Analytics Engine** — top spending categories, month-over-month trends, recurring charge detection, income vs. expenses summary
- **Tips Scaffold** — both raw analytics data and generated recommendation strings (approach TBD)

---

## What This Project Demonstrates

| Area | Details |
|------|---------|
| **Architecture** | Layered design (Controllers → Services → EF Core), DTOs, input validators, global exception-handling middleware |
| **Data Handling** | CSV parsing (CsvHelper), SHA-256 dedup, composite unique constraints, EF Core migrations |
| **Testing** | 133 tests (xUnit + Moq) — unit, integration (`WebApplicationFactory`), and real-SQLite edge-case tests |
| **CI/CD** | GitHub Actions pipeline: restore → build → test → EF migration validation |
| **API Design** | RESTful endpoints with pagination, filtering, consistent JSON error responses (400/404/409/500) |
| **Real-World Problem Solving** | Handles arbitrary bank CSV formats, prevents duplicate imports, detects subscriptions automatically |

---

## API Usage Example

**1. Create a category**

```
POST /api/v1/categories
```
```json
{ "name": "Groceries" }
```

**2. Import a bank statement**

```
POST /api/v1/imports/upload
Content-Type: multipart/form-data
```

**3. View spending analytics**

```
GET /api/v1/analytics/spending?period=monthly
```
```json
[
  { "category_name": "Groceries", "total_spent": 250.50, "rank": 1 },
  { "category_name": "Utilities", "total_spent": 80.00, "rank": 2 }
]
```

**4. Check budget alerts**

```
GET /api/v1/analytics/alerts
```
```json
[
  {
    "category_name": "Housing",
    "spent_amount": 900.00,
    "budget_limit": 1000.00,
    "alert": "Approaching budget limit"
  }
]
```

> Full request/response examples for every endpoint are in [`examples/`](examples/).

---

## Getting Started

```bash
git clone https://github.com/Corrosiv/FinanceTracker.git
cd FinanceTracker
dotnet run --project FinanceTracker.API
dotnet test
```

OpenAPI docs available at `http://localhost:5000/scalar/v1` after running.

---

## Documentation

Detailed design docs live in [`docs/`](docs/):

- [System Overview](docs/system-overview.md) — architecture, data flow, design principles
- [Domain Model](docs/domain-model.md) — entities, relationships, ER diagram
- [API Specification](docs/API-SPEC.md) — all REST endpoints, request/response formats, error handling
- [Database Design](docs/database-design.md) — tables, columns, indexes, constraints

---

## Future Improvements

- User authentication and multi-user support
- Automatic transaction categorization (ML)
- Direct bank integrations
- Multi-currency support
