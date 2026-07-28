# TelemetryManager

A small .NET library that abstracts logging and metrics behind two interfaces — `ILogWriter` and `IMetrics` — so that the underlying provider (log4net, Loggly, Datadog, console/debug output, etc.) can be swapped without touching application code.

## Overview

TelemetryManager was originally written as a proof of concept to show that a logging implementation can be fully abstracted away. It has since grown to also cover metrics (counters, gauges, timers) behind the same kind of provider-agnostic interface. Consuming code depends only on `ILogWriter` / `IMetrics`; which concrete class you `new` up at startup determines where the data actually goes.

## Features

- **Provider-agnostic logging** via `ILogWriter`, with sync and async variants of every method (`Debug`/`DebugAsync`, `Info`/`InfoAsync`, etc.)
- **Provider-agnostic metrics** via `IMetrics` — counters, gauges, durations, and arbitrary tagged values
- Multiple ready-made logger implementations: log4net (with Loggly support), stdout ("CloudWatch"), and an in-memory debug logger for tests
- Multiple ready-made metrics implementations: a Datadog-style HTTP(S) statsd shipper and an in-memory debug metrics collector for tests
- Per-instance runtime log level control (`SetLocalLoggingLevel`)
- Targets `net10.0`

## Requirements

- .NET 10 SDK
- (Log4NetLogger only) a `log.config` file deployed alongside your application

## Project Structure

```
TelemetryManager/
├── Logging/
│   ├── ILogWriter.cs         # logging abstraction
│   ├── Log4NetLogger.cs      # log4net-backed implementation (file config or Loggly)
│   ├── CloudWatchLogger.cs   # writes structured key/value lines to stdout
│   ├── DebugLogger.cs        # in-memory logger, useful in unit tests
│   ├── LoggingLevel.cs       # Standardized logging levels, as recognized by many providers.
│   ├── LoggingHelper.cs      # string -> LoggingLevel parsing
│   ├── LogMessage.cs         # POCO used by DebugLogger to record calls
│   └── Appender.cs           # named appenders known to Log4NetLogger
├── Metrics/
│   ├── IMetrics.cs           # metrics abstraction
│   ├── StatsdMetrics.cs      # ships statsd-style JSON payloads over HTTP (e.g. Datadog)
│   ├── DebugMetrics.cs       # in-memory metrics collector, useful in unit tests
│   └── MetricType.cs         # gauge | rate | count
├── CustomAppenders/
│   └── AsyncHttpAppender.cs  # internal log4net appender that POSTs events to a REST endpoint on a background thread
├── TelemetryConfig.cs        # shared config-key constants
├── log.config                # default log4net config shipped with the library
└── TelemetryManager.csproj

TelemetryManagerTestApp/       # console app exercising the library end-to-end
```

## Installation

Reference the project directly (this repo does not currently publish to NuGet):

```xml
<ItemGroup>
  <ProjectReference Include="..\TelemetryManager\TelemetryManager.csproj" />
</ItemGroup>
```

Package dependencies pulled in by `TelemetryManager.csproj`:

| Package | Version |
|---|---|
| log4net | 3.3.2 |
| log4net-loggly | 9.0.0 |
| Newtonsoft.Json | 13.0.4 |

## Quick Start

```csharp
using TelemetryManager;

// Logging
ILogWriter log = new Log4NetLogger(typeof(Program), applicationName: "MyApp", environment: "dev");

log.Info("Application starting");

try
{
    DoWork();
}
catch (Exception ex)
{
    log.Error("DoWork failed", ex);
}

// Metrics
IMetrics metrics = new DebugMetrics(applicationName: "MyApp", environment: "dev");

metrics.IncrementCounter("requests.received");
metrics.LogMethodDurationInMs("requests.duration", () => DoWork());
```

Both interfaces expose an `*Async` counterpart for every method (`InfoAsync`, `IncrementCounterAsync`, etc.) for use in async call paths.

## Logging

### `ILogWriter`

```csharp
bool IsDebugEnabled { get; }
bool IsErrorEnabled { get; }
bool IsFatalEnabled { get; }
bool IsInfoEnabled  { get; }
bool IsWarnEnabled  { get; }

void Debug(string message, Exception ex = null, object data = null);
void Info (string message, Exception ex = null, object data = null);
void Warn (string message, Exception ex = null, object data = null);
void Error(string message, Exception ex = null, object data = null);
void Fatal(string message, Exception ex = null, object data = null);

void SetLocalLoggingLevel(LoggingLevel newLevel);
```

Every method above has an `Async` twin (`DebugAsync`, `SetLocalLoggingLevelAsync`, …). `LoggingLevel` is `Debug | Info | Warn | Error | Fatal`.

### Implementations

| Class | Where it writes | Notes |
|---|---|---|
| `Log4NetLogger` | Whatever appenders are configured in `log.config` (or `app.config`/`web.config`) | See below |
| `CloudWatchLogger` | `Console.Out`, one `key: value` line per field | Despite the name it does not call any AWS API directly — it emits structured lines to stdout, which is the normal way a containerized app (ECS/Fargate, etc.) feeds the CloudWatch Logs agent |
| `DebugLogger` | `System.Diagnostics.Debug` + an in-memory `LoggedMessages` list | Intended for unit tests — inspect `LoggedMessages` to assert on what was logged |


### `Log4NetLogger` configuration

`Log4NetLogger` configures the log4net repository the first time it is constructed:

- `UseConfigurationSettings: false` (default) — looks for a file named `log.config` for init data.
- `UseConfigurationSettings: true` — uses `XmlConfigurator.Configure`, which reads log4net settings out of the host's own `app.config`/`web.config` instead.

Make sure your project copies `log.config` to the output directory (`<CopyToOutputDirectory>Always</CopyToOutputDirectory>` in the consuming project, as `TelemetryManagerTestApp` does) — the library's own `log.config` is marked `Never` and is only a template.

After construction, `Log4NetLogger` checks that at least one log level is enabled; if all of `IsDebugEnabled`/`IsInfoEnabled`/`IsWarnEnabled`/`IsErrorEnabled`/`IsFatalEnabled` are false it throws, on the assumption that the config file failed to load.

#### Loggly input key

The shipped `log.config` defines a `LogglyAppender` with no `inputKey` baked in (the value is just a `TO_BE_SET_AT_RUNTIME` placeholder) so the real key never has to live in source control. Pass the key in at construction time instead:

```csharp
var log = new Log4NetLogger(
    typeof(Program),
    applicationName: "MyApp",
    environment: "prod",
    logglyInputKey: myLogglyKey);
```

If a `LogglyAppender` is present in the loaded config, `Log4NetLogger` sets its `CustomerToken` and re-activates it after the config file loads. If no Loggly appender is configured, the parameter is simply ignored.

## Metrics

### `IMetrics`

```csharp
void IncrementCounter(string metricName, string[] tags = null);
void IncrementCounter(string metricName, int count, string[] tags = null);
void DecrementCounter(string metricName, string[] tags = null);
void SetGauge(string metricName, double value, string[] tags = null);
void LogDurationInMs(string metricName, double duration, string[] tags = null);
void LogDurationInMs(string metricName, TimeSpan duration, string[] tags = null);
void LogDurationInMs(string metricName, DateTime start, DateTime end, string[] tags = null);
void LogMethodDurationInMs(string metricName, Action method, string[] tags = null);
T LogMethodDurationInMs<T>(string metricName, Func<T> method, string[] tags = null);
void Write(MetricType metricType, string metricName, string[] customTags, string value);
```

Again, every method has an `Async` twin. `MetricType` is `gauge | rate | count`.

### Implementations

| Class | Where it writes | Notes |
|---|---|---|
| `StatsdMetrics` | HTTP POST of a statsd-style JSON payload to a configured URL | Constructor takes `(urlEndpoint, apiKey, applicationName, environment)`; every call tags the payload with `env:<environment>` and `source:<applicationName>` |
| `DebugMetrics` | `System.Diagnostics.Debug` + an in-memory `RecordedMetrics` list | Intended for unit tests — inspect `RecordedMetrics` to assert on what was recorded |

## Configuration keys

`TelemetryConfig` centralizes the names of the config keys consuming applications are expected to use for metrics setup:

```csharp
TelemetryConfig.API_URL     // "Telemetry.Metrics.ApiUrl"
TelemetryConfig.API_KEY     // "Telemetry.Metrics.ApiKey"
TelemetryConfig.APP_NAME    // "Telemetry.ApplicationName"
TelemetryConfig.ENVIRONMENT // "Environment"
```

`TelemetryConfig.DummyCall()` is a deliberately-unused, always-throwing method whose only purpose is to force MSBuild to include the `log4net-loggly` assembly in the library's build output. Never call it.

## Running the test app

`TelemetryManagerTestApp` is a console app that exercises `Log4NetLogger` and `DebugMetrics` end to end (debug/info/warn logs, 100 counter increments, then a caught exception logged as error/fatal). Application name, environment, and the Loggly input key come from `App.config`'s `appSettings`:

```xml
<appSettings>
  <add key="ApplicationName" value="TelemetryTestApp"/>
  <add key="Environment" value="dev"/>
  <add key="LogglyInputKey" value=""/>
</appSettings>
```

Run it with:

```
dotnet run --project TelemetryManagerTestApp
```

## License

I wanted to use the moster liberal license available. You can do anything you want with this code, except close source it and remove the license.

MIT — see [LICENSE](LICENSE).
