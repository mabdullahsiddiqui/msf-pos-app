# POS App Documentation

This folder contains complete technical documentation for the `pos-app` solution.

## What is documented

- System architecture and module responsibilities
- Local development setup and runtime configuration
- Authentication and authorization behavior
- API endpoint reference (controllers and routes)
- Frontend route map and UI module coverage
- Database model overview and multi-tenant behavior
- Deployment and operations notes
- Troubleshooting guidance

## Documentation index

- `architecture.md` - solution structure, request flow, service responsibilities
- `setup-and-configuration.md` - prerequisites, run commands, appsettings, CORS, JWT
- `api-reference.md` - endpoint catalog by controller and purpose
- `frontend-reference.md` - page routes, report screens, client services
- `database-and-data-model.md` - master DB, client DB, entities, connection strategy
- `deployment-and-ops.md` - publish/deploy workflows and operational checks

## Scope

This docs set is generated from the current implementation in:

- `pos-app/` (ASP.NET Core host + API)
- `pos-app.Client/` (Blazor WebAssembly client)
- Existing deployment notes and scripts in `pos-app/`

If behavior changes in code, update the matching file in this folder.
