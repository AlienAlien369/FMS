# /catchup — Rebuild Context After /clear

Read all files modified on the current branch compared to `main` or `staging`, summarize what's been implemented, identify what's left, and check for any TODOs or FIXMEs.

Steps:
1. Get current branch name: `git branch --show-current`
2. Get changed files: `git diff --name-only main...HEAD`
3. Read each changed file (limit to 20 most relevant: .cs, .ts, .html, .sql, .json)
4. Summarize: What was built, what patterns were used, what remains
5. Search for TODO/FIXME/XXX in the codebase: `grep -rn "TODO\|FIXME\|XXX" --include="*.cs" --include="*.ts" src/`
6. Check test coverage: Are there tests for the new code?
7. Output: Concise bullet summary + next 3 recommended tasks
