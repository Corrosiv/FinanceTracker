# Database Design

This document defines the database schema for the FinanceTracker API, mapping the domain model to concrete tables, columns, and relationships. It includes indexes, constraints, and notes on performance and deduplication.

---

## 1. Users Table

Stores system users.

| Column | Type | Notes |
|--------|------|-------|
| id | UUID (PK) | Primary key |
| name | VARCHAR(255) | Optional |
| email | VARCHAR(255) | Optional, unique if used |
| created_at | TIMESTAMP | Account creation |

**Indexes / Constraints:**

- Primary Key: `id`
- Optional unique index on `email` for future authentication

---

## 2. Imports Table

Stores CSV uploads per user.

| Column | Type | Notes |
|--------|------|-------|
| id | UUID (PK) | Primary key |
| user_id | UUID (FK → users.id) | Owner of the import |
| file_name | VARCHAR(255) | Original CSV file name |
| upload_date | TIMESTAMP | When file was uploaded |
| row_count | INT | Total rows in CSV |
| processed_count | INT | Transactions successfully imported |
| duplicate_count | INT | Duplicates detected |
| status | ENUM(pending, processing, completed, failed) | Import status |
| column_mapping | JSON | Maps CSV columns to internal fields |

**Indexes / Constraints:**

- Foreign Key: `user_id` → `users.id`
- Index on `(user_id, upload_date)` for fast queries

---

## 3. Categories Table

Stores user-defined transaction categories.

| Column | Type | Notes |
|--------|------|-------|
| id | UUID (PK) | Primary key |
| user_id | UUID (FK → users.id) | Owner |
| name | VARCHAR(255) | Category name |
| description | TEXT | Optional |
| created_at | TIMESTAMP | |

**Indexes / Constraints:**

- Unique constraint `(user_id, name)` to prevent duplicate category names per user

---

## 4. Transactions Table

Stores all financial transactions.

| Column | Type | Notes |
|--------|------|-------|
| id | UUID (PK) | Primary key |
| user_id | UUID (FK → users.id) | Owner |
| import_id | UUID (FK → imports.id) | Source import |
| date | DATE | Transaction date |
| amount | DECIMAL(12,2) | Positive for income, negative for expense |
| raw_description | TEXT | As provided in CSV |
| normalized_description | TEXT | Cleaned / standardized |
| balance | DECIMAL(12,2) | Optional, from CSV |
| category_id | UUID (FK → categories.id) | Optional category assignment |
| created_at | TIMESTAMP | |
| deduplication_hash | CHAR(64) | Optional SHA256 hash for duplicate detection |

**Indexes / Constraints:**

- Foreign Keys: `user_id`, `import_id`, `category_id`
- **Composite Unique Index for Deduplication (preferred for V1):**

```sql
UNIQUE(user_id, date, amount, normalized_description)
```

- Optional index on `deduplication_hash` if using hashes

- Index on `(user_id, category_id, date)` for analytics queries

**Notes:**

- Transactions are immutable except for category assignment
- Raw data is preserved for reprocessing or normalization improvements

---

## 5. Budgets Table

Stores user-defined budgets per category and period.

| Column | Type | Notes |
|--------|------|-------|
| id | UUID (PK) | Primary key |
| user_id | UUID (FK → users.id) | Owner |
| category_id | UUID (FK → categories.id) | Applies to category |
| period | ENUM(weekly, monthly, yearly) | Budget period |
| limit_amount | DECIMAL(12,2) | Spending limit |
| created_at | TIMESTAMP | |

**Indexes / Constraints:**

- Foreign Keys: `user_id`, `category_id`
- Composite index `(user_id, category_id, period)` for fast budget checks

---

## 6. Optional Tables for Future Versions

### 6.1 Tags

| Column | Type | Notes |
|--------|------|-------|
| id | UUID (PK) | Primary key |
| name | VARCHAR(255) | Tag name |

Many-to-many relation table: `transaction_tags(transaction_id, tag_id)`

---

### 6.2 Merchants

| Column | Type | Notes |
|--------|------|-------|
| id | UUID (PK) | Primary key |
| name | VARCHAR(255) | Normalized merchant name |
| pattern | VARCHAR(255) | Regex / pattern for matching CSV descriptions |
| category_hint | UUID (FK → categories.id) | Optional suggested category |

---

### 6.3 Accounts (Multi-bank support)

| Column | Type | Notes |
|--------|------|-------|
| id | UUID (PK) | Primary key |
| user_id | UUID (FK → users.id) | Owner |
| name | VARCHAR(255) | e.g., "Checking" |
| institution | VARCHAR(255) | Optional bank name |

Transactions table may gain `account_id` FK in future versions.

---

## 7. Performance Considerations

- Use **indexes** on all foreign keys for fast joins
- Use **batch inserts** during CSV imports
- Deduplication via composite unique index is sufficient for expected V1 data sizes
- Keep `deduplication_hash` optional; mainly useful if transaction comparison becomes more complex or if using multi-system imports

---

## 8. Summary

This schema ensures:

- **User data isolation** — all entities reference `user_id`
- **Immutable transactions** — reliable deduplication
- **Flexible imports** — can handle arbitrary CSV formats
- **Extensibility** — future tables for tags, merchants, accounts, and multi-currency

The design balances **simplicity for V1** and **scalability for future extensions**.