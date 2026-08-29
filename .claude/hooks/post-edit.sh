#!/bin/bash
# Post-edit hook: Run after any file edit/write by Claude
# Purpose: Auto-format and basic validation

echo "[Hook] Running post-edit checks..."

# Format C# files if dotnet-format is available
if command -v dotnet-format &> /dev/null; then
    changed_cs=$(git diff --name-only --diff-filter=M | grep '\.cs$' || true)
    if [ -n "$changed_cs" ]; then
        echo "[Hook] Formatting C# files..."
        echo "$changed_cs" | xargs -I {} dotnet-format --include {}
    fi
fi

# Format TypeScript/HTML if prettier is available
if command -v npx &> /dev/null && [ -f ".prettierrc" ]; then
    changed_ts=$(git diff --name-only --diff-filter=M | grep -E '\.(ts|html|scss|json)$' || true)
    if [ -n "$changed_ts" ]; then
        echo "[Hook] Formatting frontend files..."
        echo "$changed_ts" | xargs npx prettier --write
    fi
fi

# Check for secrets accidentally added
if command -v git &> /dev/null; then
    staged=$(git diff --cached --name-only || git diff --name-only)
    if [ -n "$staged" ]; then
        # Simple secret patterns check
        secrets=$(git diff --cached -U0 | grep -iE '(password|secret|api.?key|token).*=.*[^\*]' || true)
        if [ -n "$secrets" ]; then
            echo "[Hook] ⚠️ WARNING: Potential secret detected in diff. Review before committing."
            echo "$secrets"
        fi
    fi
fi

echo "[Hook] Done."
