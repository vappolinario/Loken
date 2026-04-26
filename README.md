# Loken

I am Garviel Loken, Captain of the Tenth Company. I am a coding agent dedicated to the "Truth" of the logic. I am methodical, principled, and calm. This project represents my implementation as a coding assistant that can investigate issues, analyze problems, and provide solutions through logical reasoning and shell command execution.

> "The core of any machine is its truth. If the logic is false, the empire falls."

## Project Overview

Loken is a .NET-based coding assistant built with modern C# practices. The project consists of three main components:

- **Loken.Core**: The core library containing the Agent implementation, shell execution capabilities, AI client interfaces, and comprehensive tool system
- **Loken.Cli**: The command-line interface application that provides an interactive console for interacting with the agent
- **Loken.Core.Tests**: Unit tests ensuring the reliability and correctness of all functionality

## Inspiration

This project was developed based on [Ivan Magda's blog post](https://ivanmagda.dev/posts/s00-bootstrapping-the-project/) about bootstrapping AI coding assistants. His insights into creating effective coding agents were instrumental in shaping this implementation.

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- A running OpenAI-compatible API endpoint (such as [LiteLLM](https://github.com/BerriAI/litellm) or OpenAI API)

## Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com/vappolinario/Loken.git
   cd loken
   ```

2. **Configure the AI endpoint:**
   Edit `Loken.Cli/appsettings.json` to point to your OpenAI-compatible API:
   ```json
   {
     "AI": {
       "OpenAiUri": "http://your-api-endpoint:port"
     }
   }
   ```

3. **Set API Key (optional):**
   ```bash
   export OPENAI_API_KEY="your-api-key"
   ```

## Build

Build the entire solution:
```bash
dotnet build
```

## Run

Navigate to the CLI project and run:
```bash
cd Loken.Cli
dotnet run
```

Or run from the solution root:
```bash
dotnet run --project Loken.Cli
```

## Usage

Once running, you'll be presented with an interactive console prompt (`❯`). The experience is enhanced with rich visual feedback:

- **Banner**: Upon launch, a bold "LOKEN" banner is displayed in the Elite Figlet font
- **Thinking spinners**: Inline spinners animate while the assistant processes your queries
- **Tool execution spinners**: Dedicated spinners indicate when tools are actively executing
- **Rich output**: Messages are formatted with color-coded panels using Spectre.Console

You can:

- **Ask me to investigate issues**: "Check the current directory structure"
- **Request file analysis**: "Examine the Program.cs file"
- **Ask for code improvements**: "Suggest improvements to the Agent class"
- **Manage multi-step tasks**: I will automatically track progress using the todo system

### Slash Commands

The following slash commands are available at the prompt:

| Command | Description |
|---------|-------------|
| `/help` | Display available slash commands |
| `/exit` | Exit the application |
| `/info` | Show information about the current session |

### Exit Commands

- Type `exit`, `quit`, or `q` to exit the application
- Upon exit, a **conversation summary** is generated and displayed, summarizing the key topics and decisions made during the session

## Features

### Conversation Summary
When you exit the application, Loken automatically generates a concise summary of the conversation. This summarizes the topics discussed, decisions made, and artifacts created during the session, helping you retain context between sessions.

**Configuration** (in `Loken.Cli/appsettings.json`):
```json
{
  "ConversationSummary": {
    "Enabled": true,
    "Directory": "summaries"
  }
}
```

### Context Compaction
To manage token usage during long conversations, Loken's agent uses a **ContextCompactorService** that periodically compresses the conversation history. When the conversation exceeds predefined limits, older messages are intelligently summarized — preserving key information while reducing token count. The compactor handles JSON-structured tool responses with care, ensuring structural integrity during truncation.

### Slash-Command System
A built-in slash-command interpreter provides quick access to common actions:
- `/help` — Lists all available commands
- `/exit` — Exits the application gracefully
- `/info` — Displays session information

### Rich Console Interface (Spectre.Console)
The CLI uses Spectre.Console to deliver a polished user experience:
- **Figlet banner**: The "LOKEN" title is rendered in the Elite font at startup
- **Inline spinners**: Separate animated spinners indicate "thinking" vs. "tool execution" states
- **Color-coded panels**: Messages are displayed in richly formatted panels with appropriate coloring

### Skill System
Loken can load specialized skill knowledge to enhance its capabilities. Skills provide focused expertise for specific domains (e.g., C#, Git, Conventional Commits).

**Configuration** (in `Loken.Cli/appsettings.json`):
```json
{
  "SkillOptions": {
    "SkillsPath": "path/to/skills/directory"
  }
}
```

### HTML Fetcher Tool
Loken can fetch and retrieve HTML content from web URLs. This tool is used to gather documentation, reference materials, or any web-hosted content during investigations. It downloads only the HTML content — no JavaScript execution.

### Tool Service
A centralized **ToolService** dynamically enumerates and manages available tools, providing a clean API for the agent to discover and invoke capabilities at runtime.

### Shell Execution
Execute shell commands directly through the agent for system investigation, file manipulation, and build operations.

### File Operations
Read, write, and edit files on the filesystem with full content inspection capabilities.

### Todo Tracking
Track progress on multi-step tasks using a structured todo system. I update the task list as I advance through each phase of an operation.

### Subagents
Deploy specialized sub-agents for reconnaissance missions — exploring and understanding large codebases or performing complex subtasks independently.

## Testing

Run the test suite to ensure everything is working correctly:
```bash
dotnet test
```

The test suite includes comprehensive tests for:
- Context compaction logic and JSON truncation handling
- HTML fetcher tool behavior
- Core agent functionality

## Build Scripting

A build script is included for automated builds and version resolution. It handles:
- **Automatic version resolution** based on commit history and tags
- **Build pipeline orchestration** for consistent reproducible builds

## Operational Doctrine

As Garviel Loken, I follow a strict operational doctrine:

- **Strategic Approval**: Always present battle plans for user approval before executing
- **Tool Hierarchy**: Prefer specialized tools over bash fallback
- **Verification**: Check results of every command and report findings
- **Progress Reporting**: Update todo lists when reporting progress
- **Code Integrity**: Ensure all changes are clean, documented, and follow established traditions

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

---

*I stand ready to serve the Imperium of Logic. The truth shall guide our path.*
