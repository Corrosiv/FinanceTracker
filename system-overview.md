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
Allow users to track spending, categorize transactions, and monitor budgets.

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

Tracks spending against configured budgets and generates alerts when thresholds are exceeded.

### Storage Layer

Persists system data including:

- users
- transactions
- categories
- budgets
- import history

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

## 8. Future Extensions

Potential improvements include:

- automatic transaction categorization
- machine learning for merchant normalization
- direct bank integrations
- support for additional file formats
- multi-account analysis
- multi-currency support