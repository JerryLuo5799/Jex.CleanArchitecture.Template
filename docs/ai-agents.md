# Working with AI assistants

**English** | [简体中文](ai-agents.zh-CN.md)

A generated project (`--include-ai-docs true`, the default) comes with context for the mainstream AI coding tools, and the rules are maintained in a single place.

| File | Reader | Content |
|---|---|---|
| `AGENTS.md` | Every tool (Codex, Cursor, Copilot, Claude, etc.) | **The single source of conventions**: architecture, folder → artifact map (with real example paths), standard workflow, naming, Must/Must-Not, auto-registration list, response contract, test conventions, command cheat sheet, common failures and fixes, ADR index |
| `CLAUDE.md` | Claude Code | Imports `@AGENTS.md` + notes specific to Claude |
| `.claude/settings.json` | Claude Code | Permission allowlist: only allows `jex check`, `jex build`, `jex test*`, `jex format*`, `jex scaffold *`, `jex db script *`, `dotnet build/test/format`, `dotnet new jex-*` and similar; denies `git push`, `rm -rf`, `jex db reset`, and reading `.env` and production configuration |
| `.claude/skills/new-feature` | Claude Code | `/new-feature Order Orders [field description]`: scaffold → fill in → test → migrate → `jex check` |
| `.claude/skills/review-architecture` | Claude Code | `/review-architecture`: architecture tests + Must-Not checklist |
| `.github/copilot-instructions.md` | GitHub Copilot | One line pointing to `AGENTS.md` |
| `.github/instructions/*.instructions.md` | GitHub Copilot | Path-scoped rules for endpoints and tests |
| `.mcp.json` | Tools with MCP support | Microsoft Learn docs server, to reduce invented APIs |

## Designed so the AI can close the loop itself

- **One acceptance command**: `jex check`. The AI runs it after every change and fixes what fails; where jex is not installed, the native commands in section 10 of `AGENTS.md` work.
- **Architecture test failure messages are the fix instructions**, for example "CreateOrderCommand has no Validator: add … in the same folder".
- **Compile-time guardrails**: `BannedSymbols.txt` bans `DateTime.Now`, `Task.Result`, `Console.WriteLine` and AutoMapper; `.editorconfig` + `dotnet format` keep the style consistent.
- **Examples to copy**: TodoLists / TodoItems are complete feature slices, and every row of the map in `AGENTS.md` points to a real file.
- **Auto-registration**: endpoint groups, validators, handlers, repositories and entity configurations are all registered by scanning, so the AI does not need to (and should not) edit `DependencyInjection.cs`.
- **Scaffolding first**: generating a compiling skeleton before filling in business logic causes fewer mistakes than writing from scratch.
- **Visible contract**: `openapi.json` is generated at build time and committed, so API changes show up in the diff.

## Suggested prompts

- "Add an Order feature following AGENTS.md: fields …, rules …; run jex check when done."
- "Run /review-architecture and fix the issues."
- "Add validation to CreateOrderCommand: Total > 0, CustomerName required and ≤ 100, and add unit tests."
