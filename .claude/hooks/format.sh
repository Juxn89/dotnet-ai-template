#!/usr/bin/env bash
# PostToolUse hook: after an agent edits a C# file, format exactly that file.
# AGENTS.md asks for tabs; this hook makes it true. A rule an agent can forget is a wish, a hook is a rule.
# Node reads the hook's JSON: the web app needs it anyway, so it is always there.
set -euo pipefail

file=$(node -e 'let s="";process.stdin.on("data",d=>s+=d).on("end",()=>{try{process.stdout.write(JSON.parse(s).tool_input?.file_path??"")}catch{}})' 2>/dev/null || true)

case "$file" in
	*.cs)
		dotnet format whitespace App.slnx --include "$file" >/dev/null 2>&1 || true
		;;
esac
