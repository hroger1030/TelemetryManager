# CLAUDE.md

## Project overview

This repository contains TelemetryManager, a small .NET library that abstracts logging and metrics
behind two interfaces — `ILogWriter` and `IMetrics` — so the underlying provider (log4net, Loggly,
Datadog/statsd, console/debug output, etc.) can be swapped without touching application code.

## Stack

- C#
- Microsoft .NET 10 (`net10.0`)
- Newtonsoft.Json
- log4net (with log4net-loggly for Loggly support)

## Build and run

From the repository root:

```bash
dotnet build TelemetryManager.sln
dotnet run --project TelemetryManagerTestApp
```

## Key components

### Primary Projects

- TelemetryManager: The library itself.
  - `Logging/` — `ILogWriter` abstraction and implementations (`Log4NetLogger`, `CloudWatchLogger`, `DebugLogger`), `LoggingLevel`, `LoggingHelper`, `LogMessage`, `Appender`.
  - `Metrics/` — `IMetrics` abstraction and implementations (`StatsdMetrics`, `DebugMetrics`), `MetricType`.
  - `CustomAppenders/` — `AsyncHttpAppender`, an internal log4net appender that POSTs events to a REST endpoint on a background thread.
  - `TelemetryConfig.cs` — shared config-key constants for consuming applications.
  - `log.config` — default log4net config template shipped with the library.

### Secondary Projects

- TelemetryManagerTestApp: Console app exercising the library end-to-end (log4net logging + debug metrics). This is where we are functionally doing our testing.
- 
## Development guidance

- Keep changes compatible with .NET 10.
- There should be only one namespace per project, and it should match the project name.
- No private functions allowed. Methods should be public to allow unit tests to be written.
- Set up functions to use dependency injection to allow for easy testing.
- Prefer small, focused updates over large refactoring efforts.
- Consuming code should depend only on `ILogWriter` / `IMetrics`, never directly on a concrete provider class.
- Never call `TelemetryConfig.DummyCall()` — it exists only to force MSBuild to include the log4net-loggly assembly in the build output, and always throws.
- If you add new features, update this file and README.md to reflect the new workflow.

## Non-negotiables

- Don't upgrade any NuGet package version without asking first. You can point out out-of-date packages to the user.
- Don't write anything to git. You can read all you want, but no writes or commits.
- When refactoring existing code, do not remove comments. They can be updated if needed, but not removed.
