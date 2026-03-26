# API Specification – FinanceTracker API (V1)

This document defines the REST API endpoints for the FinanceTracker backend. It is designed to support CSV imports, transaction management, categories, budgets, and analytics.

---

## Base URL

```
http://localhost:5000/api/v1
```

*(Change to production URL when deployed)*

---

## 1. Imports

### 1.1 Upload CSV

**POST** `/imports/upload`

**Description:** Upload a CSV file and create an import record.

**Request:**

- Content-Type: `multipart/form-data`
- Body:

| Field | Type | Description |
|-------|------|------------|
| file | file | CSV file to import |

**Response (201 Created):**

```json
{
  "id": "uuid-import-id",
  "file_name": "transactions_march.csv",
  "upload_date": "2026-03-04T12:34:56Z",
  "status": "pending",
  "row_count": 100,
  "processed_count": 0,
  "duplicate_count": 0
}
```

**Notes:**

- CSV parsing is flexible; the user may map columns if the system cannot auto-detect.
- Deduplication occurs during import processing.

---

### 1.2 List Imports

**GET** `/imports`

**Query Parameters:**

| Param | Type | Description |
|-------|------|------------|
| page | integer | Page number (default 1) |
| per_page | integer | Items per page (default 20) |

**Response (200 OK):**

```json
{
  "imports": [
    {
      "id": "uuid-import-id",
      "file_name": "transactions_march.csv",
      "upload_date": "2026-03-04T12:34:56Z",
      "status": "completed",
      "row_count": 100,
      "processed_count": 95,
      "duplicate_count": 5
    }
  ],
  "page": 1,
  "per_page": 20,
  "total": 10
}
```

---

## 2. Transactions

### 2.1 List Transactions

**GET** `/transactions`

**Query Parameters (optional filters):**

| Param | Type | Description |
|-------|------|------------|
| start_date | date | Filter from this date |
| end_date | date | Filter up to this date |
| category_id | uuid | Filter by category |
| min_amount | decimal | Minimum transaction amount |
| max_amount | decimal | Maximum transaction amount |
| page | integer | Page number (default 1) |
| per_page | integer | Items per page (default 50) |

**Response (200 OK):**

```json
{
  "transactions": [
    {
      "id": "uuid-tx-id",
      "import_id": "uuid-import-id",
      "date": "2026-03-01",
      "amount": -6.50,
      "raw_description": "Starbucks #0233",
      "normalized_description": "starbucks",
      "balance": 1443.70,
      "category_id": "uuid-category-id",
      "created_at": "2026-03-04T12:35:00Z"
    }
  ],
  "page": 1,
  "per_page": 50,
  "total": 120
}
```

---

### 2.2 Get Transaction by ID

**GET** `/transactions/{transaction_id}`

**Response (200 OK):**

```json
{
  "id": "uuid-tx-id",
  "import_id": "uuid-import-id",
  "date": "2026-03-01",
  "amount": -6.50,
  "raw_description": "Starbucks #0233",
  "normalized_description": "starbucks",
  "balance": 1443.70,
  "category_id": "uuid-category-id",
  "created_at": "2026-03-04T12:35:00Z"
}
```

---

### 2.3 Assign / Update Category

**PATCH** `/transactions/{transaction_id}/category`

**Request Body:**

```json
{
  "category_id": "uuid-category-id"
}
```

**Response (200 OK):**

```json
{
  "id": "uuid-tx-id",
  "category_id": "uuid-category-id"
}
```

**Notes:** Only category assignment can be modified in V1. Transactions are otherwise immutable.

---

## 3. Categories

### 3.1 List Categories

**GET** `/categories`

**Response (200 OK):**

```json
[
  {
    "id": "uuid-category-id",
    "name": "Groceries",
    "description": "Food and daily essentials"
  }
]
```

### 3.2 Create Category

**POST** `/categories`

**Request Body:**

```json
{
  "name": "Transport",
  "description": "Bus, taxi, metro"
}
```

**Response (201 Created):**

```json
{
  "id": "uuid-category-id",
  "name": "Transport",
  "description": "Bus, taxi, metro"
}
```

---

## 4. Budgets

### 4.1 List Budgets

**GET** `/budgets`

**Response (200 OK):**

```json
[
  {
    "id": "uuid-budget-id",
    "category_id": "uuid-category-id",
    "period": "monthly",
    "limit_amount": 500.00
  }
]
```

### 4.2 Create / Update Budget

**POST** `/budgets`

**Request Body:**

```json
{
  "category_id": "uuid-category-id",
  "period": "monthly",
  "limit_amount": 500.00
}
```

**Response (201 Created / 200 OK):**

```json
{
  "id": "uuid-budget-id",
  "category_id": "uuid-category-id",
  "period": "monthly",
  "limit_amount": 500.00
}
```

---

## 5. Analytics

> **Note:** Analytics focus exclusively on expense transactions (`Amount < 0`).
> Income is included only in the summary endpoint for context (savings rate, income vs. expenses).

### 5.1 Spending per Category

**GET** `/analytics/spending`

**Query Parameters:**

| Param | Type | Description |
|-------|------|------------|
| start_date | date | Optional |
| end_date | date | Optional |
| period | enum: weekly, monthly, yearly | Aggregation period |

**Response (200 OK):**

```json
[
  {
    "category_id": "uuid-category-id",
    "category_name": "Groceries",
    "total_spent": 250.50,
    "rank": 1
  }
]
```

---

### 5.2 Category Trends (Month-over-Month)

**GET** `/analytics/trends`

**Query Parameters:**

| Param | Type | Description |
|-------|------|------------|
| category_id | uuid | Optional — filter to a specific category |
| months | integer | Number of months to compare (default 3) |

**Response (200 OK):**

```json
[
  {
    "category_id": "uuid-category-id",
    "category_name": "Dining",
    "periods": [
      { "month": "2026-01", "total_spent": 180.00 },
      { "month": "2026-02", "total_spent": 230.00 },
      { "month": "2026-03", "total_spent": 310.00 }
    ],
    "change_percent": 34.78
  }
]
```

---

### 5.3 Recurring Charges

**GET** `/analytics/recurring`

**Description:** Detects transactions that repeat with similar amounts and descriptions across months (e.g., subscriptions).

**Response (200 OK):**

```json
[
  {
    "normalized_description": "netflix",
    "average_amount": -15.99,
    "occurrences": 6,
    "last_date": "2026-03-01",
    "frequency": "monthly"
  }
]
```

---

### 5.4 Income vs. Expenses Summary

**GET** `/analytics/summary`

**Query Parameters:**

| Param | Type | Description |
|-------|------|------------|
| start_date | date | Optional |
| end_date | date | Optional |

**Response (200 OK):**

```json
{
  "total_income": 5000.00,
  "total_expenses": -3200.00,
  "net": 1800.00,
  "savings_rate": 0.36
}
```

**Notes:** This is the only analytics endpoint that uses income data. No tips are generated for income.

---

### 5.5 Budget Alerts

**GET** `/analytics/alerts`

**Response (200 OK):**

```json
[
  {
    "category_id": "uuid-category-id",
    "category_name": "Housing",
    "period": "monthly",
    "spent_amount": 900.00,
    "budget_limit": 1000.00,
    "alert": "Approaching budget limit"
  }
]
```

---

### 5.6 Tips / Recommendations (scaffolded — approach TBD)

> Both options are scaffolded so the decision can be made later.

**GET** `/analytics/tips`

**Option A — Raw data only:** Returns the same data as other analytics endpoints in a combined view; the client generates user-facing tips.

**Option B — Generated tips:** Returns pre-built tip strings.

```json
[
  {
    "type": "category_trend",
    "message": "You spent 35% more on Dining this month compared to last month.",
    "data": { "category": "Dining", "change_percent": 34.78 }
  },
  {
    "type": "recurring",
    "message": "You have 4 recurring subscriptions totaling $65.96/month.",
    "data": { "count": 4, "total": 65.96 }
  }
]
```

---

## 6. Pagination & Filtering

- Pagination supported on endpoints returning lists (`transactions`, `imports`, etc.)
- Default `page=1`, `per_page=50` for transactions
- Filtering available by date ranges, amounts, categories, and import IDs

---

## 7. Status Codes

| Code | Meaning |
|------|---------|
| 200 | OK / Success |
| 201 | Created |
| 400 | Bad Request (invalid input) |
| 404 | Not Found (resource does not exist) |
| 409 | Conflict (duplicate) |
| 500 | Internal Server Error |

---

**End of API Specification – V1**