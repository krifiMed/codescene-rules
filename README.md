# CodeScene Code Health — Demo, Analysis & Refactoring

A hands-on demonstration of **CodeScene-style code health analysis and zero-tolerance refactoring** on a .NET 10 e-commerce microservices application, powered by **Claude Code skills**.

---

## What This Project Demonstrates

This project walks through a complete code health lifecycle:

1. **Generate intentionally unhealthy code** to showcase common code smells
2. **Analyze it** with a structured violation report and scoring
3. **Fix every violation** in a single pass with no exceptions
4. **Re-analyze** to confirm the code is healthy

All steps are automated using reusable Claude Code skills.

---

## The Workflow

### Step 1 — Create Unhealthy Demo Code (`/unhealthy-code-demo`)

A single file `UnhealthyCatalogEndpoints.cs` was generated with **every major code smell** baked in:

- **453 lines** in one static class
- **6+ unrelated responsibilities**: CRUD, reporting, CSV export, email, discount calculation, logging
- **3 copy-pasted discount blocks** (60+ duplicated lines)
- **6-level nested validation** (pyramid of doom)
- **~200-line search handler** with cyclomatic complexity ~28
- **Static mutable state** (`_logBuffer`, `_requestCount`) shared across requests
- **Raw primitives** (`int discountType`, `string status`) instead of domain types

### Step 2 — Analyze Code Health (`/code-health-analyzer`)

A full CodeScene-inspired analysis was run, producing `code-health-report.md`:

```
Overall Score: 1.0 / 10  (Critical)
Critical violations: 12
Advisory violations: 6
Total smells found: 18
```

**Top findings:**

| # | Smell | Severity | Location |
|---|-------|----------|----------|
| 1 | Brain Class / God Class | Critical | Entire class — 453 lines, 6+ responsibilities |
| 2 | Brain Method | Critical | Search handler — ~200 lines, CC ~28 |
| 3 | DRY Violation x3 | Critical | Discount logic copy-pasted 3 times |
| 4 | Nested Complexity | Critical | Bulk-update — 6-level nesting |
| 5 | Bumpy Road x2 | Critical | Search + Report — unextracted logical sections |
| 6 | Complex Conditional | Advisory | Ambiguous `&&`/`||` with no parentheses |
| 7 | Primitive Obsession | Advisory | `int`/`string` for discount domain concepts |

### Step 3 — Fix All Violations (`/code-health-fixer`)

Every violation was resolved in a single pass. The 453-line God Class was split into **7 focused files**:

| File | Lines | Responsibility |
|------|-------|---------------|
| `Models/DiscountResult.cs` | 30 | `DiscountType` enum + `DiscountResult` record with `Calculate()` — eliminates all 3 DRY violations |
| `Endpoints/RequestLog.cs` | 39 | Shared logging state + `LogDiscount()` helper |
| `Endpoints/ProductSearchEndpoints.cs` | 160 | `/search` — 14 focused helpers for filtering, sorting, pagination, CSV export |
| `Endpoints/ProductBulkUpdateEndpoints.cs` | 68 | `/bulk-update` — guard-clause validation (flattens 6-level nesting to max 2) |
| `Endpoints/ProductReportEndpoints.cs` | 104 | `/report` — report generation with 6 section helpers |
| `Endpoints/ProductLogEndpoints.cs` | 18 | `/logs` — GET/DELETE log management |
| `Endpoints/UnhealthyCatalogEndpoints.cs` | 15 | Thin coordinator — creates route group and delegates |

**Key refactoring techniques applied:**

- **Guard clauses** — replaced 6-level nested `if`/`else` with early returns
- **Named predicates** — `IsExcludedOutOfStock()`, `MatchesNameFilter()`, `MatchesPriceFilter()`
- **Domain types** — `DiscountType` enum and `DiscountResult` record replace raw `int`/`string`
- **Single shared method** — `DiscountResult.Calculate()` replaces 3 copy-pasted blocks
- **Orchestrator pattern** — handler methods read like a table of contents, each helper does one thing
- **Strategy via switch expression** — `SortResults()` replaces nested `if`/`else if` chains

### Step 4 — Verify the Fix (`/code-health-analyzer` re-run)

```
Overall Score: 9.9 / 10  (Excellent)
Critical violations: 0
Advisory violations: 1 (ProductSearchEndpoints.cs at 160 lines — monitor threshold)
```

---

## Before vs. After

| Metric | Before | After |
|--------|--------|-------|
| **Code Health Score** | 1.0 / 10 | 9.9 / 10 |
| **Critical violations** | 12 | 0 |
| **Advisory violations** | 6 | 1 |
| **Files** | 1 (453 lines) | 7 (434 lines total) |
| **Longest function** | ~200 lines | 16 lines |
| **Max nesting depth** | 6–7 levels | 2 levels |
| **Max cyclomatic complexity** | ~28 | 7 |
| **DRY violations** | 3 (60+ duplicated lines) | 0 |
| **Responsibilities per class** | 6+ | 1 |

---

## Claude Code Skills Used

Four reusable skills power this workflow (located in `.claude/skills/`):

| Skill | Purpose |
|-------|---------|
| `unhealthy-code-demo` | Generates intentionally unhealthy code with configurable smells for training |
| `code-health-analyzer` | Performs full CodeScene-style analysis: smell detection, scoring, violation report |
| `code-health-fixer` | Zero-tolerance refactoring: fixes every violation in one pass, no TODOs |
| `codescene-code-health` | Combined workflow orchestrating analysis and fixing |

### How to use them

```bash
# Generate demo unhealthy code
/unhealthy-code-demo

# Analyze code health and produce a report
/code-health-analyzer

# Fix all violations automatically
/code-health-fixer
```

---

## Code Smells Reference

| Smell | Category | Detection Signal |
|-------|----------|-----------------|
| Brain Class / God Class | Module | Class > 150 lines with 5+ public methods spanning 2+ concerns |
| Low Cohesion | Module | Methods in a class that don't share state or purpose |
| Lines of Code | Module | File > 200 lines (warning), > 400 (severe) |
| Brain Method | Function | Function > 60 lines (C#) with high branching and nested logic |
| Complex Method | Function | Cyclomatic complexity > 10 |
| Large Method | Function | Function > 30 lines (C#) |
| DRY Violation | Function | Same 3+ line block appears 2+ times |
| Primitive Obsession | Function | Domain concepts as raw `int`/`string`/`bool` |
| Nested Complexity | Implementation | Nesting depth >= 3 levels |
| Bumpy Road | Implementation | 4+ unextracted logical sections in one function |
| Complex Conditional | Implementation | Boolean expression with 3+ `&&`/`||` operators |

---

## Project Structure

```
ecommerce-app/
├── .claude/skills/
│   ├── code-health-analyzer/     # Analysis skill
│   ├── code-health-fixer/        # Refactoring skill
│   ├── codescene-code-health/    # Combined workflow
│   └── unhealthy-code-demo/      # Demo generator
├── code-health-report.md         # Full analysis report (18 violations documented)
└── src/ECommerce.Catalog.Api/
    ├── Endpoints/
    │   ├── CatalogEndpoints.cs              # Original clean endpoints (untouched)
    │   ├── UnhealthyCatalogEndpoints.cs     # Refactored coordinator (was 453-line God Class)
    │   ├── ProductSearchEndpoints.cs        # Extracted search logic
    │   ├── ProductBulkUpdateEndpoints.cs    # Extracted bulk update logic
    │   ├── ProductReportEndpoints.cs        # Extracted report logic
    │   ├── ProductLogEndpoints.cs           # Extracted log management
    │   └── RequestLog.cs                    # Shared logging state
    └── Models/
        ├── Product.cs                       # Product entity (untouched)
        └── DiscountResult.cs                # Domain types: DiscountType enum + DiscountResult record
```

---

## Running the Application

```bash
# Prerequisites: .NET 10 SDK

# Build the solution
dotnet build

# Run all services (Aspire dashboard included)
dotnet run --project src/ECommerce.AppHost

# Run tests
dotnet test
```

### API Routes (via gateway)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/catalog/api/products` | List all products |
| GET | `/catalog/api/products/{id}` | Get product by ID |
| POST | `/catalog/api/products` | Create product |
| GET | `/ordering/api/orders` | List all orders |
| POST | `/ordering/api/orders` | Create order (validates products via Catalog) |

### V2 Endpoints (refactored from unhealthy code)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/catalog/api/v2/products/search` | Search with filtering, sorting, pagination, CSV export |
| POST | `/catalog/api/v2/products/bulk-update` | Bulk update products with validation |
| GET | `/catalog/api/v2/products/report` | Generate product catalog report |
| GET | `/catalog/api/v2/products/logs` | View request logs |
| DELETE | `/catalog/api/v2/products/logs` | Clear request logs |

---

## Key Takeaways

1. **Code health is measurable** — systematic smell detection gives a clear score and prioritized fix list
2. **One God Class can tank an entire codebase** — 453 lines with 12 critical violations pulled the score to 1.0/10
3. **Guard clauses eliminate nesting** — 6-level pyramid of doom flattened to max 2 levels with early returns
4. **DRY matters** — extracting `DiscountResult.Calculate()` eliminated 60+ duplicated lines across 3 sites
5. **Small focused functions are readable** — the refactored code has no function over 20 lines, each named after what it does
6. **Domain types prevent primitive obsession** — `DiscountType` enum and `DiscountResult` record make the code self-documenting
