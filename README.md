# Loken

I am Garviel Loken, Captain of the Tenth Company. I am a coding agent dedicated to the "Truth" of the logic. I am methodical, principled, and calm. This project represents my implementation as a coding assistant that can investigate issues, analyze problems, and provide solutions through logical reasoning and shell command execution.

> "The core of any machine is its truth. If the logic is false, the empire falls."

## Project Overview

Loken is a .NET-based coding assistant built with modern C# practices. The project consists of three main components:

- **Loken.Core**: The core library containing the Agent implementation, shell execution capabilities, and AI client interfaces
- **Loken.Cli**: The command-line interface application that provides an interactive console for interacting with the agent
- **Loken.Core.Tests**: Unit tests ensuring the reliability and correctness of the core functionality

## Inspiration

This project was developed based on [Ivan Magda's blog post](https://ivanmagda.dev/posts/s00-bootstrapping-the-project/) about bootstrapping AI coding assistants. His insights into creating effective coding agents were instrumental in shaping this implementation.

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- A running OpenAI-compatible API endpoint (such as [LiteLLM](https://github.com/BerriAI/litellm) or OpenAI API)

## Setup

1. **Clone the repository:**
   ```bash
   git clone <repository-url>
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
- Exit the application: Type `exit`, `quit`, or `q`

## Features

- **Shell Command Execution**: I can run bash commands to investigate system state
- **Logical Analysis**: I analyze problems methodically and provide reasoned solutions
- **File Operations**: I can examine, edit, and create files as needed
- **Test Verification**: I ensure changes don't break existing functionality
- **Context Awareness**: I maintain conversation context for complex problem-solving

## Architecture

The project follows clean separation of concerns:

- **Agent.cs**: Core agent logic with system prompt and tool execution
- **IShellExecutor/ShellExecutor**: Abstraction for shell command execution
- **IChatClient/LiteLlmChatClient**: AI client interface for OpenAI-compatible APIs
- **Program.cs**: Host builder and console loop for the CLI application

## Testing

Run the test suite to ensure everything is working correctly:
```bash
dotnet test
```

## Development Guidelines

When working with this codebase:

1. **Truth First**: Always verify the logic before accepting changes
2. **Methodical Approach**: Investigate root causes, not symptoms
3. **Clean Edits**: Follow established patterns and traditions
4. **Test Verification**: Ensure tests pass before committing changes

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

*I stand ready to serve the Imperium of Logic. The truth shall guide our path.*
