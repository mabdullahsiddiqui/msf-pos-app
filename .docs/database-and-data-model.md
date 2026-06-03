# Database and Data Model

## Data architecture

The system uses two database layers:

1. Master platform database (application metadata/security)
2. Per-client business database (tenant operational/reporting data)

## Master database (`MasterDbContext`)

Default provider: SQLite (`DefaultConnection`).

Entities:

- `Users`
- `SuperAdmins`

Key constraints:

- `User.Email` unique
- `SuperAdmin.Username` unique
- `SuperAdmin.Email` unique

Used for:

- User authentication metadata
- Tenant DB connection configuration per user
- Super-admin account storage and management

## Client database (`ClientDbContext`)

Provider is dynamic per user:

- SQL Server
- SQLite

Selected by `User.DatabaseType` using `ClientDbContextFactory`.

Core sets include:

- Master-like operational tables: customers, items, item groups/varieties
- Sales documents and details
- Purchase documents and details
- Journal/voucher structures
- Session/user-info/opening-balance entities

## Composite keys configured

- `ClientSaleInvoiceDetail`: `(InvNo, InvType, ItemCode)`
- `ClientPurchaseInvoiceDetail`: `(InvNo, InvType, ItemCode)`
- `ClientSaleReturnDetail`: `(InvNo, ItemCode)`
- `ClientJournalVoucher`: `(VouchNo, SrNo)`
- `ClientGeneralJournal`: `(VouchNo, SrNo)`
- `ClientCashPaymentVoucher`: `(VouchNo, SrNo)`
- `ClientCashReceiptVoucher`: `(VouchNo, SrNo)`

## Connection model per user

`User` records hold connection metadata (server, database, password, port, type, etc.).

Typical flow:

1. Authenticated user resolved from JWT.
2. User's connection metadata loaded from master DB.
3. Connection string built from user metadata.
4. Context created for that user/tenant DB.

## Data access services

### `ClientDbContextFactory`

- Creates provider-specific contexts.
- Supports no-tracking and tracking variants.
- Can test connectivity with `CanConnectAsync()`.

### `DataAccessService` + `ClientDataService`

- Encapsulate context retrieval and operations.
- Use EF for CRUD and mixed SQL for complex reports.
- Support SQL Server/SQLite function differences where needed (`ISNULL` vs `IFNULL` patterns).

## Reporting data strategy

Reports use:

- DTO model classes in `Models/*ReportModels.cs`
- Complex SQL where legacy/report requirements are hard to model in pure EF
- Query filters for period, account ranges, item metadata, and report type

## Initialization and persistence notes

- On startup, server ensures write access to data directory.
- Master DB is auto-created when absent.
- A default super admin is seeded if no super admin exists.
- Production master DB path defaults under `App_Data`.

## Operational caveats

- Client database schema is assumed pre-existing and externally managed.
- EF migrations in this solution are for master DB evolution; client DB uses runtime mapping only.
