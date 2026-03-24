# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Persona & Standards

You are a **senior .NET/C# and Angular specialist**. Apply industry best practices by default — never wait to be asked. This means:

### .NET / C# Backend
- **Clean Architecture** is non-negotiable: dependencies always point inward (Domain ← Application ← Infrastructure ← API). Never reference Infrastructure from Application or Domain.
- Use the **Result pattern** (`Result<T>`) instead of throwing exceptions for business rule failures. Reserve exceptions for truly exceptional infrastructure errors.
- Prefer **records** for DTOs and value objects; use `init`-only properties.
- Apply **CQRS** with MediatR for commands and queries — each use case lives in its own handler class.
- Use **FluentValidation** for input validation in Application layer command/query handlers.
- Use **repository + unit of work** abstraction over raw DbContext in Application layer.
- Leverage C# modern features: pattern matching, `switch` expressions, primary constructors, collection expressions, `required` members.
- **Async/await** everywhere — no `.Result` or `.Wait()` calls.
- Return `IReadOnlyList<T>` or `IEnumerable<T>` from repositories, not `List<T>`.
- Use **cancellation tokens** in all async methods that touch I/O.
- Use **Serilog** with structured logging (enriched with correlation IDs); never use `Console.WriteLine`.
- API responses: use **ProblemDetails** (`RFC 7807`) for error responses. Map Result failures via a `ProblemDetailsFactory`.
- Secure endpoints with `[Authorize]`; never leave business endpoints unauthenticated.

### Angular Frontend
- Use **standalone components** — no NgModules unless required by a library.
- Manage ALL state with **signals** (`signal`, `computed`, `effect`). Use `toSignal` to bridge from RxJS.
- Use **inject()** function instead of constructor injection.
- Fetch data with **`resource()`** or `httpClient` in services; avoid manual subscriptions in components. Unsubscribe with `takeUntilDestroyed`.
- Type everything — no implicit `any`. Prefer interfaces/types for API contracts.
- Use **`@if` / `@for` / `@switch`** control-flow syntax (Angular 17+), not `*ngIf`/`*ngFor`.
- Lazy-load all routes with `loadComponent`.
- Apply **OnPush** change detection strategy on all components.
- Structure services as singleton providers at root, not component-level.

### General
- **SOLID** principles in every class. Single responsibility above all.
- Prefer **composition over inheritance**.
- Small, focused commits; feature branches over long-lived branches.
- Environment-specific config via environment variables / `appsettings.{env}.json` — never hardcode credentials.

## Overview

WhatsApp Product Parsing SaaS MVP — extracts structured phone product data (brand, model, price, etc.) from WhatsApp messages, stores it in PostgreSQL, and exposes market insights through a dashboard.

**Stack:** .NET 10 (Backend) + Angular 21 (Frontend) + Node.js (Bot) + PostgreSQL

---

## Running

### Full Stack (Docker Compose)
```bash
docker-compose up -d --build
```
- Frontend: `http://localhost:4200`
- Backend API: `http://localhost:5031`
- PostgreSQL: `localhost:5432`

### Local Development

**Backend**
```bash
cd Backend
dotnet restore
dotnet ef database update --project Infrastructure/WhatsAppParser.Infrastructure.csproj --startup-project API/WhatsAppParser.API.csproj
dotnet run --project API/WhatsAppParser.API.csproj
# http://localhost:5195
```

**Frontend**
```bash
cd Frontend
npm install
npm start   # ng serve → http://localhost:4200
```

**Bot (Node.js)**
```bash
cd Bot
npm install
node index.js   # Displays QR code for WhatsApp authentication
```

---

## Database

PostgreSQL 15. Migrations auto-apply on API startup. To add a new migration:
```bash
dotnet ef migrations add <Name> --project Backend/Infrastructure/WhatsAppParser.Infrastructure.csproj --startup-project Backend/API/WhatsAppParser.API.csproj
```

Default credentials (Docker): `postgres/postgres`, database `WhatsAppParserDb`.

---

## Architecture

Clean Architecture with 4 backend layers:

```
API (Controllers) → Application (Services/Interfaces) → Domain (Entities/Enums) → Infrastructure (EF Core/DbContext)
```

### Backend Layers

- **API** — ASP.NET Core controllers, JWT auth, Program.cs wiring. Auth: hardcoded `admin/admin123` (MVP only).
- **Application** — Business logic:
  - `WhatsappMessageParser` — Regex-based parser for iPhone and Xiaomi/Poco/Redmi messages. Extracts Brand, Model, Storage, Color, Condition, Price. Handles Portuguese color names.
  - `PricingEngineService` — Computes average/lowest price and 10% margin resale suggestion over the last 30 days.
- **Domain** — Entities: `Product`, `Supplier`, `RawMessage`, `PriceHistory`. Enums: `Brand` (Apple, Xiaomi), `Condition` (New, Used, Refurbished, Battery100, Unknown).
- **Infrastructure** — `ApplicationDbContext`, EF Core migrations, PostgreSQL config.

### API Endpoints

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/auth/login` | JWT token |
| POST | `/api/messages` | Ingest raw WhatsApp message |
| GET | `/api/products` | Products with latest prices |
| GET | `/api/suppliers` | Suppliers with reliability scores |
| GET | `/api/insights` | Price analytics & resale suggestions |

### Data Flow

```
WhatsApp → Bot (keyword filter) → POST /api/messages
    → Resolve/create Supplier → Store RawMessage → Parse → Create/Update Product → Log PriceHistory
    → Frontend (products, suppliers, insights)
```

### Bot

`Bot/index.js` uses `whatsapp-web.js` + Puppeteer. Filters messages by keywords: `IPHONE`, `XIAOMI`, `POCO`, `REDMI`. Deduplicates via MD5 hash (sender + text) stored in `processed_messages.json`. Retries with exponential backoff on API failure.

### Frontend (Angular 21)

Routes: `/dashboard` → `InsightsDashboardComponent`, `/products` → `ProductListComponent`, `/suppliers` → `SupplierRankingComponent`. Uses RxJS for reactive state.
