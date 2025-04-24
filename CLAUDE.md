# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build/Run Commands
- Build: `dotnet build`
- Run: `dotnet run`
- Project uses .NET 9.0

## Code Style Guidelines
- Use PascalCase for classes, methods, properties
- Croatian language is used for class/method names
- Enable nullable reference types
- Follow C# bracing and indentation conventions
- Use HtmlAgilityPack for HTML parsing
- Follow existing error handling patterns using exceptions with descriptive messages
- Use string interpolation for string formatting
- Use null-conditional operators and explicit null checks
- Use list initialization syntax with []
- Format dates as yyyy-MM-dd
- Keep consistent method organization in classes
- Include appropriate XML documentation for public APIs
- Use Task-based asynchronous pattern for I/O operations