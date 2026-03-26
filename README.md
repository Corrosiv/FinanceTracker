# FinanceTracker

Personal Finance Tracker API built with ASP.NET Core.  
This API allows users to import financial transactions from CSV files, categorize expenses and income, track budgets, and generate financial insights.

---

## Tech Stack
- **Backend:** ASP.NET Core Web API  
- **ORM:** EF Core  
- **Database:** SQLite (V1, single-user)  
- **Testing:** xUnit  

---

## Architecture

The project uses a **layered architecture** for modularity and scalability:

- **Controllers** – handle HTTP requests and responses.  
- **Services** – business logic, including import processing, budgeting, and analytics.  
- **DTOs** – data transfer objects for API responses and requests.  
- **Data** – EF Core entities and database context.  
- **Validators** – input validation and consistency checks.  
- **Middleware** – global error handling, logging, etc.  

The architecture is designed for **extensibility**, allowing future integration of authentication, multiple users, and additional data sources.

---

## Features (V1)

- CSV import with flexible column mapping  
- Deduplication of transactions  
- Transaction categorization  
- Budget tracking per category and period  
- Financial insights and alerts for overspending  

---

## Documentation

The project includes detailed documentation:

- [System Overview](docs/system-overview.md) – high-level description of the system and architecture  
- [Domain Model](docs/domain-model.md) – core entities, attributes, and relationships  
- [Database Design](docs/database-design.md) – tables, columns, indexes, and constraints  
- [API Specification](docs/API_SPEC.md) – REST endpoints, request/response formats, filtering, and pagination  

---

## Getting Started

1. Clone the repository:  
```bash
git clone https://github.com/Corrosiv/FinanceTracker.git
```

2. Navigate to the project folder:  
```bash
cd FinanceTracker
```

3. Run the API:  
```bash
dotnet run
```

4. Run tests:  
```bash
dotnet test
```

---

## Future Improvements

- User authentication and multi-user support (planned for V2) 
- Real-time alerts and notifications
- Direct bank integrations
- Automatic transaction categorization using machine learning
- Multi-currency support

---

**End of README**