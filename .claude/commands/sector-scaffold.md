# /sector-scaffold — Scaffold a New Business Sector Module

Prompt for sector name (e.g., "mining", "railways").

Generate the full scaffold:

**Backend:**
- `src/Domain/Entities/{Sector}/` — entities
- `src/Application/Features/{Sector}/Commands/` — CQRS commands
- `src/Application/Features/{Sector}/Queries/` — CQRS queries
- `src/Infrastructure/Persistence/Configurations/{Sector}/` — EF configs
- `src/API/Controllers/{Sector}/` — API controllers
- Add sector to `FeatureRegistry` and `ModuleConfiguration`

**Frontend:**
- `src/app/remotes/{sector}/` — Angular module federation remote
- `webpack.config.js` for the remote
- Update `module-federation.manifest.json`
- Add sector routes to `DynamicRoutingService`
- Add sector nav items to seed data

**Database:**
- Add sector to `features` table seed migration
- Add default RBAC permissions for the sector

Follow existing patterns in `agent_docs/8_sectors.md`.
