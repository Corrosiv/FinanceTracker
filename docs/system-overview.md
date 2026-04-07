# System Overview

## 1. Purpose

The Financial Tracker API is a backend service designed to help users analyze and manage their personal finances by importing transaction data from external financial institutions.

Many banks provide transaction history as CSV exports, but these exports differ in format and structure. This system provides a flexible ingestion pipeline that normalizes transaction data and enables consistent financial analysis.

The API focuses on:

- Importing financial transactions from arbitrary CSV formats
- Detecting and preventing duplicate transactions
- Categorizing expenses and income
- Providing insights into spending patterns
- Supporting user-defined budgets and alerts

---

## 2. Goals

The system aims to provide:

### Flexible Data Import
Support CSV files from different financial institutions by dynamically mapping columns during import.

### Reliable Transaction Deduplication
Prevent duplicate transactions when users import overlapping CSV files.

### Financial Insight
Allow users to track spending, categorize transactions, and monitor budgets. Specifically:

- **Recurring charges detection** — identify subscriptions and repeated expenses
- **Category trends** — month-over-month spending changes per category
- **Top spending categories** — rank categories by total spend
- **Budget suggestions and alerts** — suggest limits based on patterns; alert when exceeded
- **Income context** — provide summaries (savings rate, income vs. expenses) without generating tips for income

### Extensible Architecture
Design the system so additional data sources (e.g., APIs from financial institutions) can be added later.

---

## 3. Non-Goals

The following are intentionally out of scope for the initial version:

- Direct bank integrations (Plaid, Open Banking, etc.)
- Real-time transaction syncing
- Multi-currency financial modeling
- Investment portfolio tracking
- Automated tax reporting

These may be considered in future versions.

---

## 4. Core Domain Concepts

### User

Represents a system user with their own financial data and configuration.

Each user has:

- their own transactions
- categories
- budgets
- import history

### Transaction

A financial event representing either an expense or income.

- **Expense**: a Transaction where `Amount < 0`. Expense is a *view/lens* on Transaction, not a separate entity or table.
- **Income**: a Transaction where `Amount > 0`. Stored and used for summary/context (savings rate, income vs. expenses overview) but no tips or recommendations are generated for income.

Attributes may include:

- date
- description
- amount
- account
- category
- import source
- deduplication hash

Transactions are imported from CSV files and normalized into a consistent internal format.

### Category

A classification applied to transactions.

Examples:

- Housing
- Groceries
- Transportation
- Salary
- Entertainment

Users can create or modify their own categories.

### Budget

A user-defined spending limit for a category over a given time period (initially monthly).

Budgets allow the system to generate alerts when spending approaches or exceeds limits.

**Budget flow**: user imports transactions → system shows spending patterns → system suggests budget limits based on patterns → user accepts or adjusts → budget alerts kick in.

### Import

Represents a CSV file uploaded by a user.

It tracks:

- source file metadata
- column mappings
- import timestamp
- number of transactions processed
- duplicates detected

---

## 5. High-Level Architecture

The system follows a modular architecture composed of several main components:

### API Layer

Handles HTTP requests from clients and exposes endpoints for:

- transaction import
- financial data retrieval
- budget configuration
- analytics

### Import Processor

Responsible for:

- parsing CSV files
- identifying column mappings
- normalizing transaction data
- computing deduplication hashes
- inserting valid transactions

### Deduplication Engine

Prevents duplicate transaction insertion by comparing normalized transaction data using a deterministic hash derived from key attributes.

### Budget Engine

Tracks spending against configured budgets and generates alerts when thresholds are exceeded. Also suggests budget limits based on observed spending patterns.

### Storage Layer

Persists system data including:

- users
- transactions
- categories
- budgets
- import history

### Analytics Engine

Analyzes expense transactions (Amount < 0) to produce:

- Recurring charge detection (subscriptions, repeated expenses)
- Category trends (month-over-month spending changes)
- Top spending categories
- Income vs. expense summaries and savings rate

### Tips / Recommendations (approach TBD)

Both options are scaffolded so the decision can be made later:

- **Option A**: API returns raw analytics data; the client generates user-facing tips.
- **Option B**: API generates tip strings (e.g., "You spent 30% more on Dining this month").

No tips are generated for income transactions.

---

## 6. Data Flow

Typical transaction ingestion flow:

1. User uploads a CSV file.
2. The system analyzes column structure.
3. User confirms or adjusts column mappings.
4. Each row is normalized into the internal transaction format.
5. A deduplication hash is computed.
6. The system checks for existing transactions with the same hash.
7. New transactions are inserted; duplicates are skipped or flagged.
8. Budget calculations are updated.

---

## 7. Key Design Principles

### Import Flexibility

The system does not assume a fixed CSV structure and instead relies on column mapping.

### Deterministic Deduplication

Transactions are identified using a stable hash derived from key fields such as:

- user ID
- date
- normalized description
- amount

### User Data Isolation

All financial data is scoped to a specific user to ensure privacy and security.

---

## CI and Examples

The repository includes a GitHub Actions workflow that runs on pushes and pull requests. The CI job restores, builds, runs tests, and validates EF Core migrations (lists migrations and performs a dry-run update) to ensure migrations apply cleanly.

Example assets are provided in the `examples/` directory and include sample CSV bank statements (`us-bank-statement.csv`, `mx-bank-statement.csv`) and API request/response JSON files for each endpoint group. These are useful for manual testing and documentation.

## 8. Future Extensions

Potential improvements include:

- automatic transaction categorization
- machine learning for merchant normalization
- direct bank integrations
- support for additional file formats
- multi-account analysis
- multi-currency support
