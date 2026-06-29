# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A .NET 10 microservices e-commerce application orchestrated by .NET Aspire. It uses EF Core with in-memory databases (no external DB required), YARP for API gateway routing, and Blazor Server for the web frontend.

## Key commands

```bash
# Run the full app (all services + Aspire dashboard)
dotnet run --project src/ECommerce.AppHost

# Build the solution
dotnet build

# Build a single project
dotnet build src/ECommerce.Catalog.Api

# Run all tests
dotnet test

# Run a single test project
dotnet test tests/ECommerce.Catalog.Tests

# Run a single test by name
dotnet test --filter "GetProducts_ReturnsSeededProducts"
```

The Aspire dashboard port changes every launch. Use the "Login URL" printed to stdout to access it.

## Architecture

```
Web (Blazor) → Gateway (YARP) → Catalog API (products)
                               → Ordering API (orders, calls Catalog to validate)
```

All services reference `ServiceDefaults` which wires up OpenTelemetry, health checks (`/health`, `/alive`), HTTP resilience (Polly), and Aspire service discovery. Service addresses use Aspire resource names (e.g. `https+http://catalog`), never hardcoded URLs. These names are defined in `AppHost.cs`.

### Service communication flow

- **Web** → talks to Catalog and Ordering through the **Gateway** (via `/catalog/...` and `/ordering/...` prefixed routes)
- **Gateway** → strips the prefix (`/catalog`, `/ordering`) and forwards to the respective service
- **Ordering** → calls **Catalog** directly (not through Gateway) to validate product IDs when creating orders

### Gateway routing

YARP routes are configured in `src/ECommerce.Gateway/appsettings.json`. The gateway strips the `/catalog` or `/ordering` prefix before forwarding, so backend services only see `/api/products` or `/api/orders`.

### API routes (through gateway)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/catalog/api/products` | List all products |
| GET | `/catalog/api/products/{id}` | Get product by ID |
| POST | `/catalog/api/products` | Create product |
| GET | `/ordering/api/orders` | List all orders |
| GET | `/ordering/api/orders/{id}` | Get order by ID |
| POST | `/ordering/api/orders` | Create order (validates products via Catalog) |

### Data

Both APIs use EF Core in-memory databases. Data resets on restart. The Catalog DB is seeded with 4 demo products in `CatalogDbContext.OnModelCreating`. Ordering serializes enums as strings (configured via `JsonStringEnumConverter`).

## Conventions

- **Minimal APIs**: endpoints are defined as static extension methods in `Endpoints/` classes (e.g. `MapCatalogEndpoints`), not controllers.
- **Primary constructors**: DbContexts and service clients use C# primary constructor syntax.
- **Typed HTTP clients**: inter-service communication uses `AddHttpClient<T>` with Aspire service discovery base addresses.
- **Solution format**: uses `.slnx` (XML solution file), not `.sln`.

## Tests

Tests live in `tests/` and use **xUnit** with `WebApplicationFactory<Program>` (in-process integration tests, no real network).

- **Catalog tests** use `WebApplicationFactory<Program>` directly — no mocking needed since Catalog has no upstream dependencies.
- **Ordering tests** use a custom `OrderingApiFactory` that replaces `CatalogServiceClient`'s `HttpClient` with a `MockCatalogHandler` returning 4 known products (IDs 1–4). When adding Ordering tests that create orders, use those product IDs.
