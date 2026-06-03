# Architecture

## Solution overview

`pos-app` is a .NET 8 unified Blazor solution with:

- A server host (`pos-app`) that serves the UI and exposes REST APIs.
- A WebAssembly client (`pos-app.Client`) that renders the UI and calls APIs.
- A master metadata database (SQLite) for platform users and super admins.
- Dynamic per-user client database connections (SQL Server or SQLite) for business data.

## Projects and responsibilities

### `pos-app` (server host)

- Boots Razor Components + interactive WebAssembly render mode.
- Hosts API controllers under `/api/*`.
- Configures JWT authentication/authorization.
- Configures CORS for production and localhost origins.
- Initializes and seeds master database (default super admin if none exists).
- Contains deployment scripts and IIS config (`web.config`).

Key files:

- `Program.cs`
- `Controllers/*.cs`
- `Data/MasterDbContext.cs`
- `Data/ClientDbContext.cs`
- `Services/*.cs`

### `pos-app.Client` (Blazor WebAssembly UI)

- Implements pages and report screens.
- Manages local auth token storage and API calls.
- Provides user and super-admin workflows.
- Binds report filters to backend report endpoints.

Key files:

- `Program.cs`
- `Components/Pages/*.razor`
- `Components/Routes.razor`
- `Services/AuthService.cs`
- `Services/DataService.cs`
- `Services/SuperAdminService.cs`

## High-level request flow

1. User authenticates (user or super admin), receives JWT.
2. Client stores token in `localStorage` and sends it as `Authorization: Bearer`.
3. API controller validates JWT and resolves current user identity.
4. Server reads user connection metadata from master DB.
5. Server creates a scoped client DB context for that tenant/user.
6. Controller executes EF/LINQ and raw SQL report logic, then returns JSON.
7. Blazor page renders report/dashboard from response models.

## Multi-tenant data access strategy

- Master app metadata lives in `MasterDbContext` (`Users`, `SuperAdmins`).
- Business data lives in each client database via `ClientDbContext`.
- `ClientDbContextFactory` chooses provider from `User.DatabaseType`:
  - `SQLServer`
  - `SQLite`
- Read-heavy contexts default to no-tracking.
- Write operations can request tracking-enabled contexts.

## Report architecture

Reporting is centralized in `ReportsController` and consumed by report pages in `pos-app.Client`.

Patterns used:

- EF Core queries for strongly modeled retrieval.
- Raw SQL for heavy/legacy report logic and performance-sensitive queries.
- Query-string based filters (date ranges, account ranges, invoice/report type).

## Security model

- JWT bearer auth configured in server startup.
- Protected controller usage:
  - Whole-controller protection on reports, client data, and generic data endpoints.
  - Protected endpoints in auth controller for profile/password management.
- Password hashing: BCrypt (`BCrypt.Net-Next`).
- Tokens include user identity claims and expire in 7 days.

## Hosting/runtime model

- Development:
  - Swagger enabled
  - WebAssembly debugging enabled
- Production:
  - Exception handler + HSTS
  - Static assets served by ASP.NET Core/IIS
  - App data persisted under `App_Data`

## Directory map (important areas)

- `pos-app/Controllers` - API endpoints
- `pos-app/Services` - auth, connection/context, data access logic
- `pos-app/Data` - EF contexts
- `pos-app/Models` - domain + report DTOs
- `pos-app.Client/Components/Pages` - routeable UI screens
- `pos-app.Client/Components/Shared` - reusable report/search components
- `pos-app.Client/Services` - HTTP clients and auth/session helpers
