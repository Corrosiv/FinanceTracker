# Copilot Instructions

## Project Guidelines
- FinanceTracker domain decisions: (1) Expense is a lens/view on Transaction where Amount < 0, not a separate entity. (2) Analytics should cover: recurring charges detection, category trends (month-over-month), and top spending categories. (3) Tips/recommendations: scaffold both options (API returns raw analytics data AND generates tip strings) so the decision can be made later. (4) Income transactions are stored and used for summary/context (savings rate, income vs expenses overview) but no tips are generated for income. (5) Budget flow: user imports → system shows patterns → system suggests budgets → user accepts/adjusts → budget alerts kick in.

## Sprint 3 Design Decisions
- Budget UNIQUE constraint on (UserId, CategoryId, Period); budget history deferred to backlog.
- Recurring charges detection uses frequency + interval consistency (min 3 occurrences, stddev ≤ 5 days).
- Category assignment is bulk by IDs; description-based rule deferred to backlog.
- Analytics time range uses predefined period enum (last30days, last3months, last6months, lastyear, alltime) with optional custom from/to override.
