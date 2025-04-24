# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Summary

ZgBibliobusTimeTable is a C# application that:
- Scrapes the Zagreb City Libraries (KGZ) Bibliobus (mobile library) schedule
- Processes and organizes the schedule data (dates, times, locations)
- Generates a static website with interactive calendar and list views
- Creates iCalendar (.ics) files for each location and the complete schedule
- Provides a user-friendly interface to browse the mobile library's schedule

The application fetches HTML content from the KGZ website, parses it using HtmlAgilityPack, extracts schedule data including map links, and then generates a responsive website that allows users to:
- View the schedule in calendar or list format
- Filter by location
- See past and future dates
- Download iCalendar files
- Add events to their personal calendars
- Access Google Maps links for each location
- Switch between Croatian and English languages

## Key Files and Components

- **Program.cs**: Entry point that coordinates the overall data flow
- **WebContent.cs**: Handles web scraping and HTML content processing
- **Tools.cs**: Contains utility functions and data structures
- **WebsiteGenerator.cs**: Generates the static website
- **ICalGenerator.cs**: Creates iCalendar files for different views
- **templates/index.html**: Main template for Croatian version
- **templates/index-en.html**: English version template
- **templates/styles.css**: Styling for the website
- **Data/index.html**: Contains the source HTML from KGZ website

## Build/Run Commands
- Build: `dotnet build`
- Run: `dotnet run`
- Project uses .NET 9.0

## UI Features
- Responsive design works on mobile and desktop
- Calendar view shows events by day with location links
- List view groups dates chronologically with recent dates at top
- Toggle to show/hide older past dates
- Location filtering via tabs
- Downloads for iCalendar files (.ics)
- Add to Calendar widget for direct calendar integration
- Language switching between Croatian (HR) and English (EN)

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
- Always end files with a newline

## Workflow Requirements
- Log every single command input with timestamps into claude-input.log
- Push changes to GitHub after each modification is completed