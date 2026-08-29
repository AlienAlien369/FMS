# /release — Prepare a Release

1. Run full test suite: `dotnet test` and `ng test --watch=false`
2. Check `CHANGELOG.md` — ensure latest changes are documented
3. Bump version in `package.json` and `.csproj` using `pnpm version patch` (or minor/major)
4. Generate EF Core migration if model changes detected: `dotnet ef migrations add Release{Version} --project src/Infrastructure`
5. Build production containers: `docker build -t rgbsi-fleet-api:{version} -f src/API/Dockerfile .`
6. Tag release: `git tag -a v{version} -m "Release v{version}"`
7. Push tag: `git push origin v{version}`
8. Open draft release PR against `main`
9. Post summary to #deploys (if Slack MCP configured)

Do NOT merge to main. Only open the PR and tag.
