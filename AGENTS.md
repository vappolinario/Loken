# Loken — Agent Instructions

## Build & Run

```bash
dotnet build
dotnet run --project Loken.Cli
```

Requires .NET 10.0 SDK. Set `OPENAI_BASE_URL` and `OPENAI_API_KEY` environment variables for the AI client.

## Test

```bash
dotnet test
```

Tests are written with xUnit.

## Project Structure

- `Loken.Core/` — Core library: Agent, tool handlers, skill system, AI client interfaces
- `Loken.Cli/` — CLI application: interactive console loop, slash commands, Spectre.Console UI
- `Loken.Core.Tests/` — Unit tests for the core library

## Code Style

- **Target framework**: .NET 10.0 (`net10.0`)
- **Nullable**: Enabled (`<Nullable>enable</Nullable>`)
- **Implicit usings**: Enabled
- **Namespaces**: `Loken.Core` for core, `Loken.Cli` for CLI
- **File-scoped namespaces** are used throughout
- **Primary constructors** are used where applicable
- **No code comments** — write self-explanatory code. Let the code speak the truth.
- **No XML doc comments** on public APIs
- **No regions** — code is organized by file responsibility, not by directives
- **Async/await**: Use `Task<T>` return types. Never use `.Result` or `.Wait()` to avoid deadlocks.
- **Service injection**: Constructor injection via `IServiceProvider` and hosted services
- **Naming conventions**:
  - Interfaces: `I` prefix (e.g., `IAgentFactory`, `IToolHandler`)
  - Public methods: PascalCase
  - Private fields: `_camelCase` prefixed with underscore
  - Parameters: camelCase

## Key Patterns

- **Handlers**: Implement `IToolHandler` with `Name`, `Description`, `Parameters` properties and `ExecuteAsync` method. Register as transient in DI.
- **Services**: Interface + implementation pairs registered in `Program.cs` via standard DI.
- **Agent**: Singleton `Agent` with system prompt set via `SetSystemPrompt()`. Messages flow through `IChatClient`.
- **Skills**: Loadable knowledge files via `ISkillService/SkillService`. Skills are Markdown files in a skills directory.
- **Subagents**: Created via `IAgentFactory.CreateSubagent()`. Each subagent gets its own copy of tools and context.
- **Configuration**: Options classes (`SkillOptions`, etc.) bound from `IConfiguration`.
- **Path resolution**: `IPathResolver` provides the working directory path.

## Git Conventions

- **Commit style**: [Conventional Commits](https://www.conventionalcommits.org/) — `feat:`, `fix:`, `refactor:`, `chore:`, `docs:`, `test:`
- **Scope**: Optional scope in parentheses after type (e.g., `feat(agent):`)
- **No merge commits** — rebase workflow preferred

## Tools Available

- `bash` — Execute shell commands
- `read_file` — Read file contents
- `write_file` — Write content to files
- `edit_file` — Replace exact text in files
- `fetch_html` — Fetch HTML content from URLs
- `todo` — Track multi-step task progress
- `subagent` — Deploy specialized subagents for reconnaissance
- `load_skill` — Load specialized skill knowledge
- `load_project_instructions` — Load this AGENTS.md file at runtime
