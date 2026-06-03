# API Reference

Base pattern: `/api/{controller}/{action}`

Authentication:

- JWT Bearer for protected routes.
- Swagger is available in development.

## AuthController (`/api/auth`)

Primary purpose: end-user account and profile actions.

- `POST /api/auth/login`
- `GET /api/auth/profile` (protected)
- `POST /api/auth/signup`
- `POST /api/auth/change-password`
- `POST /api/auth/user/change-password` (protected)
- `POST /api/auth/forgot-password`
- `POST /api/auth/update-profile`
- `PUT /api/auth/profile` (protected)
- `POST /api/auth/logout`

Observed in client service usage:

- `POST /api/auth/user/forgot-password`
- `POST /api/auth/user/verify-code`
- `POST /api/auth/user/reset-password`
- `POST /api/auth/user/resend-code`
- `PUT /api/auth/update-connection`

Note: verify route parity between controller and client methods for all `user/*` actions.

## SuperAdminController (`/api/superadmin`)

Primary purpose: super-admin auth, tenant user management, and super-admin profile flows.

- `POST /api/superadmin/login`
- `GET /api/superadmin/debug/superadmins`
- `POST /api/superadmin/debug/create-superadmin`
- `POST /api/superadmin/create-user`
- `GET /api/superadmin/users`
- `PUT /api/superadmin/users/{userId}`
- `POST /api/superadmin/change-password`
- `POST /api/superadmin/test-user-connection/{userId}`
- `POST /api/superadmin/logout`
- `POST /api/superadmin/forgot-password`
- `POST /api/superadmin/verify-code`
- `POST /api/superadmin/reset-password`
- `POST /api/superadmin/resend-code`
- `GET /api/superadmin/profile`
- `PUT /api/superadmin/profile`

## ClientDataController (`/api/clientdata`) [Protected]

Primary purpose: operational client data and lightweight dashboard/lookup endpoints.

- `GET /api/clientdata/dashboard`
- `GET /api/clientdata/customers-ef`
- `POST /api/clientdata/customers`
- `GET /api/clientdata/customers/{accCode}`
- `PUT /api/clientdata/customers/{accCode}`
- `DELETE /api/clientdata/customers/{accCode}`
- `GET /api/clientdata/customers`
- `GET /api/clientdata/customers/simple`
- `GET /api/clientdata/debug/connection`
- `POST /api/clientdata/setup/test-db`
- `GET /api/clientdata/items`
- `GET /api/clientdata/recent-sales`
- `GET /api/clientdata/reports/sales-summary`
- `GET /api/clientdata/reports/monthly-sales/{year}`
- `POST /api/clientdata/test-connection`
- `GET /api/clientdata/session/start-date`
- `GET /api/clientdata/item-groups`

## DataController (`/api/data`) [Protected]

Primary purpose: generic/testing data access.

- `GET /api/data/sales`
- `GET /api/data/inventory`
- `GET /api/data/reports/summary`
- `GET /api/data/query`

## ReportsController (`/api/reports`) [Protected]

Primary purpose: accounting and business reporting endpoints.

- `GET /api/reports/trial-balance`
- `GET /api/reports/trial-balance-csharp`
- `GET /api/reports/trial-balance-sql`
- `GET /api/reports/monthly-account-balance`
- `GET /api/reports/three-trial-balance`
- `GET /api/reports/ledger`
- `GET /api/reports/account-position`
- `GET /api/reports/sales-summary`
- `GET /api/reports/cash-book`
- `GET /api/reports/journal-book`
- `GET /api/reports/transaction-journal`
- `GET /api/reports/purchase-journal`
- `GET /api/reports/sales-journal`
- `GET /api/reports/purchase-register`
- `GET /api/reports/sales-register`
- `GET /api/reports/item-purchase-ledger`
- `GET /api/reports/item-sales-ledger`
- `GET /api/reports/customer-sales-ledger`
- `GET /api/reports/item-purchase-register`
- `GET /api/reports/item-sales-register`
- `GET /api/reports/broker-sales-report`
- `GET /api/reports/supplier-purchase-ledger`
- `GET /api/reports/supplier-tax-ledger`
- `GET /api/reports/customer-aging`
- `GET /api/reports/supplier-aging`
- `GET /api/reports/item-purchase-summary`
- `GET /api/reports/item-sales-summary`

## AzureSqlTestController (`/api/azuresqltest`)

- `GET /api/azuresqltest/test-connection`

## Authorization coverage summary

Controller-level `[Authorize]`:

- `ReportsController`
- `ClientDataController`
- `DataController`

Endpoint-level `[Authorize]` in auth controller:

- `GET /api/auth/profile`
- `PUT /api/auth/profile`

## Service-to-endpoint map (client side)

### `pos-app.Client/Services/AuthService.cs`

- user login/signup/password/profile + forgot/verify/reset/resend flows

### `pos-app.Client/Services/SuperAdminService.cs`

- super-admin login, user management, test user DB connection, profile/password, reset flows

### `pos-app.Client/Services/DataService.cs`

- dashboard, client data CRUD/read operations, and full report suite calls

## Error handling patterns

- Most endpoints return JSON payloads with `success/message` conventions.
- Client services often deserialize with case-insensitive JSON options.
- Some service calls parse raw strings first to handle inconsistent response envelopes.
