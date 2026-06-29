# Code Health Analysis Report

**Project:** ecommerce-app
**Date:** 2026-06-29
**Overall Score: 7.8 / 10** 🟡

---

## Executive Summary

- **Overall Code Health Score:** 7.8 / 10 (weighted by file size)
- **Total violations found:** 12 critical, 8 advisory
- **Files analyzed:** 18 source files (excluding auto-generated `obj/` files)
- **Top 3 highest-priority findings:**
  1. Brain Class / God Class in `UnhealthyCatalogEndpoints.cs` — 453 lines with 6+ unrelated responsibilities (CRUD, reporting, CSV, email, discounts, logging)
  2. DRY Violation (x3) — discount calculation logic copy-pasted identically across search, bulk-update, and report handlers
  3. Nested Complexity (6 levels) — bulk-update validation forms a pyramid of doom with 6+ nesting levels
- **Recommended immediate actions:**
  - Extract a `CalculateDiscount()` method to eliminate 60+ duplicated lines
  - Flatten nested validation with guard clauses and early returns
  - Split `UnhealthyCatalogEndpoints` into focused endpoint classes
  - Replace static mutable state (`_logBuffer`, `_requestCount`) with `ILogger`

---

## File-by-File Analysis

### `UnhealthyCatalogEndpoints.cs` — 1.0 / 10 🔴 Critical

| # | Smell | Severity | Location | Description |
|---|-------|----------|----------|-------------|
| 1 | Brain Class / God Class | 🔴 Critical | `UnhealthyCatalogEndpoints` (entire class) | 453 lines, handles CRUD, reporting, CSV export, email notifications, discount calculation, inventory management, and logging |
| 2 | Low Cohesion | 🔴 Critical | `UnhealthyCatalogEndpoints` | Report generation, log management, email sending, discount calculation — completely unrelated concerns in one class |
| 3 | Lines of Code (severe) | 🟡 Advisory | Entire file | 453 lines (threshold: > 400 = severe) |
| 4 | Brain Method | 🔴 Critical | Search lambda, lines 32–230 | ~200-line handler does filtering, discount calculation, sorting, pagination, CSV export, and stats |
| 5 | Complex Method | 🔴 Critical | Search lambda, lines 32–230 | Cyclomatic complexity ~28 (14+ `if`/`else if`, 6+ `&&`/`\|\|`, ternary) |
| 6 | Nested Complexity | 🔴 Critical | Search filtering, lines 49–62 | 4 nesting levels: `for` → `if (name != null)` → `if (p.Name != null)` → `if (!Contains)` |
| 7 | DRY Violation #1 | 🔴 Critical | Lines 104–130 vs 269–295 vs 390–407 | Identical discount calculation block copy-pasted 3 times across search, bulk-update, and report |
| 8 | Bumpy Road | 🔴 Critical | Search lambda, lines 32–230 | 5+ logically distinct sections: filtering → discount → sorting → pagination → CSV → stats |
| 9 | Nested Complexity | 🔴 Critical | Bulk-update, lines 246–322 | 6+ nesting levels: `for` → `if (id > 0)` → `if (name)` → `if (price)` → `if (stock)` → `if (product)` → `if (description)` |
| 10 | Brain Method | 🔴 Critical | Bulk-update lambda, lines 233–329 | ~97-line handler with validation, update, discount calculation, and logging |
| 11 | DRY Violation #2 | 🔴 Critical | Lines 265–295 | Discount logic copy-pasted from search handler |
| 12 | Bumpy Road | 🔴 Critical | Report handler, lines 333–430 | 6 visually distinct sections: header → price analysis → stock analysis → product listing → discount summary → email sending |
| 13 | DRY Violation #3 | 🔴 Critical | Lines 387–407 | Same discount logic copy-pasted a third time |
| 14 | Large Method | 🟡 Advisory | Report handler, lines 333–430 | ~97 lines (threshold: > 30 for C#) |
| 15 | Large Method | 🟡 Advisory | Bulk-update handler, lines 233–329 | ~97 lines |
| 16 | Complex Conditional | 🟡 Advisory | Lines 90–91 | 6 boolean operators with ambiguous precedence: `include && includeOutOfStock != null && includeOutOfStock == false && p.AvailableStock <= 0 \|\| include && includeOutOfStock == null && p.AvailableStock <= 0` |
| 17 | Primitive Obsession | 🟡 Advisory | Lines 99–101, 266–268, 387–389 | `int discountType`, `double discountPercent`, `string status` represent domain concepts that should be a `Discount` value object |
| 18 | Developer Congestion | 🟡 Advisory | Entire file | Large central file touching many features; high conflict risk in team settings |

#### Code Excerpts

**Nested Complexity (6 levels)** — bulk-update validation pyramid of doom:

```csharp
// Lines 246-322
if (u.Id > 0)
{
    if (u.Name != null && u.Name != "")
    {
        if (u.Price >= 0)
        {
            if (u.Stock >= 0)
            {
                var product = await db.Products.FindAsync(u.Id);
                if (product != null)
                {
                    // ... update logic ...
                    if (u.Description != null)
                    {
                        product.Description = u.Description;  // 7th level!
                    }
```

**DRY Violation** — discount logic repeated 3 times:

```csharp
// Appears at lines 104-130, 269-295, and 390-407
if (p.Price > 100 && p.AvailableStock > 50)
{
    discountType = 1; discountPercent = 10; status = "bulk-discount";
    var discountedPrice = p.Price - (p.Price * (decimal)discountPercent / 100);
    _logBuffer.Add($"...");
}
else if (p.Price > 50 && p.AvailableStock > 20)
{
    discountType = 2; discountPercent = 5; status = "medium-discount";
    // ... identical pattern ...
}
```

**Complex Conditional** — ambiguous boolean logic:

```csharp
// Lines 90-91 — missing parentheses, operator precedence unclear
if (include && includeOutOfStock != null && includeOutOfStock == false && p.AvailableStock <= 0
    || include && includeOutOfStock == null && p.AvailableStock <= 0)
```

#### Refactoring Recommendations

1. **Extract `DiscountCalculator`** — Move discount logic to a single method returning a `DiscountResult` record. Eliminates all 3 DRY violations.
2. **Split into separate endpoint classes** — `SearchEndpoints`, `BulkUpdateEndpoints`, `ReportEndpoints`, `LogEndpoints`.
3. **Flatten validation** with early returns (guard clauses) instead of nested `if`s.
4. **Extract query builder** — Move filtering, sorting, and pagination into a `ProductQueryBuilder`.
5. **Replace primitives** with `DiscountType` enum and `Discount` value object.
6. **Remove log/email concerns** — Use `ILogger` for logging, a separate `IReportEmailService` for email.

---

### `Extensions.cs` — 9.5 / 10 🟢 Good

| # | Smell | Severity | Location | Description |
|---|-------|----------|----------|-------------|
| 1 | Lines of Code | 🟡 Advisory | Entire file | 147 lines (at the monitor threshold, approaching 150) |

No critical issues. This is an Aspire template file — well-structured with focused methods.

---

### `OrderingApiTests.cs` — 9.5 / 10 🟢 Good

| # | Smell | Severity | Location | Description |
|---|-------|----------|----------|-------------|
| 1 | Lines of Code | 🟡 Advisory | Entire file | 203 lines (includes nested factory + mock handler; moderate range) |

Tests are well-structured. The file bundles test class, factory, and mock — acceptable for integration test setup.

---

### All Other Files — 10.0 / 10 🟢 Excellent

The following files have **no violations** detected:

| File | Lines | Assessment |
|------|-------|-----------|
| `AppHost.cs` | 24 | Clean Aspire orchestration |
| `CatalogDbContext.cs` | 27 | Clean EF context with seed data |
| `Product.cs` | 10 | Clean model |
| `Catalog/Program.cs` | 33 | Clean startup |
| `CatalogEndpoints.cs` | 44 | Clean minimal API — ideal contrast to UnhealthyCatalogEndpoints |
| `Gateway/Program.cs` | 18 | Minimal YARP setup |
| `Ordering/Program.cs` | 42 | Clean startup |
| `OrderingDbContext.cs` | 27 | Clean EF context |
| `OrderingEndpoints.cs` | 73 | Well-structured with proper validation |
| `Order.cs` | 29 | Clean domain models |
| `CatalogServiceClient.cs` | 23 | Clean typed HTTP client |
| `Web/Program.cs` | 40 | Clean Blazor setup |
| `CatalogApiClient.cs` | 13 | Clean API client |
| `OrderingApiClient.cs` | 28 | Clean API client |
| `CatalogApiTests.cs` | 94 | Well-structured tests |

---

## Cross-File Patterns

- **Clean vs. unhealthy contrast:** `CatalogEndpoints.cs` (44 lines, score 10) vs `UnhealthyCatalogEndpoints.cs` (453 lines, score 1) — demonstrates how the same feature can be implemented healthily or with severe technical debt.
- **No cross-file DRY violations** outside `UnhealthyCatalogEndpoints.cs` — the rest of the codebase follows clean patterns consistently.
- **Static mutable state** in `UnhealthyCatalogEndpoints` (`_logBuffer`, `_requestCount`, `_lastError`) is a thread-safety hazard under concurrent requests — not flagged as a code smell per se, but a correctness bug.

---

## Priority Refactoring Roadmap

| Priority | Smell | File | Estimated Impact |
|----------|-------|------|-----------------|
| 1 | DRY Violation (x3) | `UnhealthyCatalogEndpoints.cs` | **High** — Extract `CalculateDiscount()` method, eliminates 60+ duplicated lines |
| 2 | Nested Complexity (6 levels) | `UnhealthyCatalogEndpoints.cs:246-322` | **High** — Flatten with guard clauses/early returns, reduces cognitive load dramatically |
| 3 | Brain Class | `UnhealthyCatalogEndpoints.cs` | **High** — Split into 4 focused endpoint classes, improves testability and team velocity |
| 4 | Brain Method (search) | `UnhealthyCatalogEndpoints.cs:32-230` | **High** — Extract filtering, sorting, pagination, formatting into composable helpers |
| 5 | Bumpy Road (report) | `UnhealthyCatalogEndpoints.cs:333-430` | **Medium** — Extract each report section into named helper methods |
| 6 | Complex Conditional | `UnhealthyCatalogEndpoints.cs:90-91` | **Medium** — Add parentheses and simplify boolean logic |
| 7 | Primitive Obsession | `UnhealthyCatalogEndpoints.cs` | **Low** — Introduce `DiscountType` enum and `Discount` record |
| 8 | Low Cohesion (logging/email) | `UnhealthyCatalogEndpoints.cs` | **Medium** — Use `ILogger`, extract email into a service |

---

## Appendix — Smell Reference

| Smell | Definition |
|-------|-----------|
| Brain Class / God Class | A class with too many methods (>5 public), too many lines (>150), spanning multiple unrelated concerns |
| Low Cohesion | Methods in a class that don't share state or purpose — unrelated responsibilities lumped together |
| Brain Method | A function exceeding 40+ lines (C#) with high branching and nested logic |
| Complex Method | A function with cyclomatic complexity > 10 |
| Large Method | A function exceeding 30 lines (C#) of executable statements |
| Nested Complexity | Nesting depth of 4+ levels (`if` in `for` in `while`, etc.) |
| DRY Violation | Same logic block (3+ lines) appears 2+ times |
| Bumpy Road | A function with 4+ distinct logical chunks separated by blank lines, none extracted |
| Complex Conditional | A boolean expression with 3+ `&&`/`\|\|` operators |
| Primitive Obsession | Domain concepts passed as raw `int`, `string`, `bool` instead of value types |
| Lines of Code | File exceeding 200 lines (warning) or 400 lines (severe) |
| Developer Congestion | A large central file likely edited by many people, causing merge conflicts |
