# Frontend Reference

## Client structure

Main UI project: `pos-app.Client`

Important areas:

- `Components/Pages` - routeable pages
- `Components/Layout` - user/super-admin layouts and nav
- `Components/Shared` - reusable report widgets/inputs/tables
- `Services` - API interaction and auth/session state

## Router and rendering

- Router is defined in `Components/Routes.razor`.
- App uses interactive WebAssembly render mode.
- Default layout resolves through client layout components.

## Primary routes

### Public/basic

- `/` (`Home`)
- `/login` (`Login`)
- `/signup` (`Signup`)
- `/Error` (`Error`)

### User account and session flows

- `/dashboard`
- `/customers`
- `/user/profile`
- `/user/forgot-password`
- `/user/verify-code`
- `/user/reset-password`

### Super admin flows

- `/superadmin`
- `/superadmin/login`
- `/superadmin/dashboard`
- `/superadmin/profile`
- `/superadmin/forgot-password`
- `/superadmin/verify-code`
- `/superadmin/reset-password`
- `/superadmin/reports/{UserId:int}`

### Reports routes

- `/reports/trial-balance`
- `/reports/three-trial-balance`
- `/reports/monthly-account-balance`
- `/reports/ledger`
- `/reports/account-inquiry`
- `/reports/cash-book`
- `/reports/journal-book`
- `/reports/transaction-journal`
- `/reports/purchase-journal`
- `/reports/sales-journal`
- `/reports/purchase-register`
- `/reports/sales-register`
- `/reports/item-purchase-ledger`
- `/reports/item-sales-ledger`
- `/reports/customer-sales-ledger`
- `/reports/item-purchase-register`
- `/reports/item-sales-register`
- `/reports/broker-sales-report`
- `/reports/supplier-purchase-ledger`
- `/reports/supplier-tax-ledger`
- `/reports/customer-aging`
- `/reports/supplier-aging`
- `/reports/item-purchase-summary`
- `/reports/item-sales-summary`

## Shared components (not exhaustive)

- `ReportTable` - tabular report display helper
- `DateInput` - consistent date selection UX
- `SearchInput` and searchable account/item inputs
- `ReportTypeSelector`, `ReportTitleHeader`
- summary cards and loading spinner components

## Client service responsibilities

### `AuthService`

- User login/signup/logout
- User profile update and password changes
- User forgot-password verification/reset flow
- JWT token persistence via browser `localStorage`

Storage keys used:

- `auth_token`

### `SuperAdminService`

- Super-admin login
- User creation/list/update
- User DB connection tests
- Super-admin profile/password updates
- Super-admin forgot-password verification/reset flow

Additional local keys:

- `forgot_password_email`
- `reset_token`

### `DataService`

- General data endpoints
- Client dashboard and lookup endpoints
- Full report endpoint consumption with filter query parameters
- Session start date and item group loading for filter forms

## Authentication behavior in UI

- Services attach bearer token from local storage/state to outgoing requests.
- Unauthorized or malformed responses are usually transformed into `Success=false` result objects.
- Pages rely on service response envelopes to show success/error feedback.

## Notes for maintainers

- Keep route paths in page components aligned with nav menu links.
- Keep client service endpoint strings aligned with server controller route attributes.
- When adding new report screens, update:
  1. `Components/Pages` (new page)
  2. `Services/DataService.cs` (API method)
  3. nav/menu and any report selector UI
  4. `api-reference.md` and this file
