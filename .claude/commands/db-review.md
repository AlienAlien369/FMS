# /db-review — Review Database Changes

Before any PR that touches database schema:

1. Read the migration file(s) in `src/Infrastructure/Migrations/`
2. Verify RLS policies exist for new tables
3. Check indexes: Are `tenant_id`, `created_at`, and query fields indexed?
4. Check for N+1 query risks in associated repository code
5. Verify no breaking changes to existing tables (renames, column drops)
6. Check if seed data or default values are provided
7. Run `dotnet ef migrations script` from last migration to current — review raw SQL
8. Output: Pass/Fail with specific issues and fix suggestions
