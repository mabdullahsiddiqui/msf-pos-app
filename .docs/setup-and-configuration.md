# Setup and Configuration

## Prerequisites

- .NET SDK 8.0+
- SQL Server access for client databases (if using `DatabaseType.SQLServer`)
- Optional: SQLite tooling (for local inspection)

## Running locally

From repository root:

```powershell
cd d:\POS\pos-app
dotnet run --project ".\pos-app\pos-app.csproj"
```

Default development URLs from launch settings:

- `http://localhost:5151`
- `https://localhost:7225` (with fallback to `http://localhost:5151`)

## Build/publish

```powershell
cd d:\POS\pos-app\pos-app
dotnet publish -c Release -o .\publish
```

## Configuration files

- `pos-app/appsettings.json`
- `pos-app/appsettings.Development.json`
- `pos-app/appsettings.Production.json`

### Important settings

#### Connection strings

- `ConnectionStrings:DefaultConnection`
  - Development default: `Data Source=master.db`
  - Production default: `Data Source=App_Data/master.db`
- `ConnectionStrings:ClientDbTemplate` (template/example only)

#### JWT settings

- `Jwt:Key`
- `Jwt:Issuer`
- `Jwt:Audience`

Notes:

- JWT key must be long/random and stable in production.
- Changing JWT key invalidates existing user sessions.

#### Currency and localization

- Culture: `en-PK`
- Currency metadata from `CurrencySettings` (PKR/Rs)
- Request localization is configured at startup.

## Middleware and startup behavior

`Program.cs` configures:

- Razor components + WebAssembly interactivity
- Controllers + Swagger (dev)
- JWT auth + authorization
- CORS policy `AllowBlazorWasm`
- Request localization
- Static files, antiforgery, HTTPS redirection

### CORS policy

Current explicit origins include:

- `https://softxonepk.com`
- `http://softxonepk.com`
- `http://localhost:5000`
- `https://localhost:5001`
- `http://localhost:5273`
- `https://localhost:5273`

## Database initialization

On startup, the app:

1. Resolves `DefaultConnection` DB path.
2. Ensures target data directory exists.
3. Verifies write permissions with a temp write test.
4. Calls `EnsureCreatedAsync()` for master DB.
5. Seeds a default super admin if none exists.

## Authentication and tokens

- JWT bearer validation includes issuer, audience, signing key, lifetime.
- User token lifetime: 7 days.
- Client stores token in browser `localStorage` key `auth_token`.

## Environment-specific behavior

### Development

- Swagger + Swagger UI enabled
- WebAssembly debug middleware enabled

### Production

- HSTS enabled
- Exception handler route `/Error`
- Uses production app settings and App_Data-based master DB path

## Known package notes

Current build warnings reference known advisories in:

- `System.Data.SqlClient` 4.8.5
- `System.IdentityModel.Tokens.Jwt` 7.0.3

Plan package upgrades before long-term production hardening.
