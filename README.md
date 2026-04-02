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

Once running, you'll be presented with an interactive console prompt (`❯`). You can:

- Ask me to investigate issues: "Check the current directory structure"
- Request file analysis: "Examine the Program.cs file"
- Ask for code improvements: "Suggest improvements to the Agent class"
- Manage multi-step tasks: I will automatically track progress using the todo system
- Exit the application: Type `exit`, `quit`, or `q`

## Features

### Core Agent Capabilities
- **Strategic Planning**: Presents battle plans for user approval before execution
- **Logical Analysis**: Analyzes problems methodically and provides reasoned solutions
- **Context Awareness**: Maintains conversation context for complex problem-solving
- **Progress Tracking**: Automatic todo list management for multi-step tasks

### File Operations
- **File Reading**: Read file contents with configurable character limits
- **File Writing**: Create and write new files with automatic directory creation
- **File Editing**: Precise text replacement with exact match verification
- **Path Security**: Sandboxed file access restricted to working directory

### Shell Execution
- **Command Execution**: Run bash commands to investigate system state
- **Security Validation**: Blocks dangerous commands (rm -rf /, sudo, etc.)
- **Error Handling**: Comprehensive error reporting and validation

### Task Management
- **Todo System**: Track progress on multi-step tasks with status updates
- **Automatic Reminders**: Reminds about pending tasks after inactivity
- **Progress Reporting**: Clear visual indicators for todo/doing/done status

## Testing

Run the test suite to ensure everything is working correctly:
```bash
dotnet test
```

### Operational Doctrine
As Garviel Loken, I follow a strict operational doctrine:
- **Strategic Approval**: Always present battle plans for user approval
- **Tool Hierarchy**: Prefer specialized tools over bash fallback
- **Verification**: Check results of every command
- **Progress Reporting**: Update todo lists when reporting progress

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

*I stand ready to serve the Imperium of Logic. The truth shall guide our path.*
