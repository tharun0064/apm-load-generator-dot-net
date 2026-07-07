# APM Load Generator - .NET Version

This is a .NET port of the Java-based APM load generator system. It consists of 4 ASP.NET Core applications that generate **EXTREME HEAVY LOAD** on an Oracle database for APM (Application Performance Monitoring) testing purposes.

## Architecture

### App1 - OLTP Load Generator (Port 8080)
- **Purpose**: High-frequency transactional workload generator
- **Load Pattern**: 10,000 - 40,000 operations/second
- **Threads**: 50 concurrent worker threads (configurable)
- **Database**: `oltp_user` schema
- **Connection Pool**: Max 100 connections, Min 20
- **Operations**: Create orders, update customers, manage inventory, process transactions, session management
- **Data Management**: Automatic table truncation every 35 minutes
- **APM**: New Relic instrumentation enabled

### App2 - Analytics Load Generator (Port 8081)
- **Purpose**: Heavy analytical query workload generator
- **Load Pattern**: 500 - 2,000 complex queries/second
- **Threads**: 20 concurrent worker threads (configurable)
- **Database**: `analytics_user` schema (reads from `oltp_user` tables)
- **Connection Pool**: Max 50 connections, Min 10
- **Query Timeouts**: 180 seconds for long-running analytics
- **Query Types**: Customer data (6-table joins), sales analytics, customer analytics, product analytics, reporting, data warehouse operations
- **APM**: New Relic instrumentation enabled

### App3 - Analytics Load Generator Duplicate (Port 8082)
- Exact duplicate of App2 for parallel load testing
- APM: New Relic instrumentation enabled

### App4 - Analytics Load Generator (No APM) (Port 8083)
- Analytics load generator WITHOUT APM instrumentation for baseline performance comparison
- APM: DISABLED

## Prerequisites

- **.NET 8.0 SDK** or later
- **Oracle Database** 19c or later with schema from `oracle-setup.sql`
- **New Relic Account** (for Apps 1, 2, 3)

## Quick Start

1. **Setup Database**:
```bash
sqlplus sys as sysdba @oracle-setup.sql
```

2. **Configure Each App**:
```bash
cd App1OltpLoadGenerator
cp .env.example .env
# Edit .env with your DB credentials and New Relic license key
```

3. **Run** (use the provided `run.sh` — it builds, enables the New Relic agent, then launches):
```bash
./run.sh
```

Repeat for App2, App3, App4 — or start all four with `./runAllApps.sh` from the repo root.

> **Do not** just `dotnet run` by hand for Apps 1/2/3. The New Relic .NET agent
> only attaches when the CoreCLR profiler environment variables are set (see
> [New Relic .NET Agent](#new-relic-net-agent) below). `run.sh` / `runAllApps.sh`
> set them for you; a bare `dotnet run` ships the agent binaries but instruments
> nothing.

## Configuration

Edit `.env` file in each app directory:

```bash
DB_HOST=localhost
DB_PORT=1521
DB_SERVICE_NAME=ORCLPDB1
DB_USERNAME=oltp_user  # or analytics_user for Apps 2/3/4
DB_PASSWORD=your_password

DB_POOL_MAX=100  # 100 for App1, 50 for Apps 2/3/4
DB_POOL_MIN=20   # 20 for App1, 10 for Apps 2/3/4

API_PORT=8080  # 8080=App1, 8081=App2, 8082=App3, 8083=App4
THREADS=50     # 50 for App1, 20 for Apps 2/3/4

NEW_RELIC_LICENSE_KEY=your_license_key
NEW_RELIC_APP_NAME=App1-OLTP-LoadGenerator-DotNet
```

The `NEW_RELIC_LICENSE_KEY` and `NEW_RELIC_APP_NAME` environment variables
override `newrelic.config`, so the `REPLACE_WITH_LICENSE_KEY` placeholder in the
config can be left as-is — the license key from `.env` is what actually
authenticates the agent. `newrelic.config` is still used for tracer settings
(transaction/database tracing, distributed tracing).

## New Relic .NET Agent

Apps 1/2/3 reference the `NewRelic.Agent` NuGet package, which places the agent
and CoreCLR profiler in each app's build output under a `newrelic/` folder. The
.NET runtime only loads that profiler when these environment variables are set:

```bash
CORECLR_ENABLE_PROFILING=1
CORECLR_PROFILER={36032161-FFC0-4B61-B559-F6C5D41BAE5A}
CORECLR_NEWRELIC_HOME=<app>/bin/.../newrelic
CORECLR_PROFILER_PATH=<app>/bin/.../newrelic/libNewRelicProfiler.so
NEW_RELIC_LICENSE_KEY=<your key>
NEW_RELIC_APP_NAME=<app name>
```

You don't set these by hand — `run.sh` (per app) and `runAllApps.sh` (all apps)
build the app, locate the profiler in the build output, copy the app's
`newrelic.config` into place, and export the variables before launching. The
shared logic lives in [`newrelic-env.sh`](newrelic-env.sh) (`enable_newrelic` /
`disable_newrelic`).

**Platform:** the profiler bundled/used here is Linux (`libNewRelicProfiler.so`).
The New Relic .NET agent supports Linux and Windows, **not macOS** — on macOS the
run scripts will print a warning and start the app without APM.

**App4** is deliberately **not** instrumented (no NuGet packages, no profiler env
vars) so it serves as a no-APM baseline for overhead comparison.

## Key Features

- **Oracle SQL Compatibility**: Preserves all Oracle-specific SQL (sequences, hints, (+) outer joins, ADD_MONTHS, SYSDATE, TRUNC, etc.)
- **New Relic Integration**: Uses `[Trace]` attributes and NewRelic.Agent.Api for custom instrumentation
- **Connection Pooling**: Oracle.ManagedDataAccess.Core with configurable pool sizes
- **Automatic Cleanup**: App1 truncates and rebuilds tables every 35 minutes to prevent unbounded growth
- **High Concurrency**: Thread-based load generation with configurable worker counts
- **REST API**: Each app exposes APIs that can be called directly for testing

## Monitoring

### Database:
```sql
SELECT username, COUNT(*) FROM v$session
WHERE username IN ('OLTP_USER', 'ANALYTICS_USER')
GROUP BY username;
```

### New Relic APM:
- Check for 3 applications (App1, App2, App3)
- Verify throughput: 10k-40k rpm (App1), 500-2k rpm (Apps 2/3)
- View database queries, transaction traces, distributed traces

### Application Logs:
```
Starting OLTP Load Generator with 50 threads...
Worker thread 0 started
...
```

## Project Structure

```
apm-load-generator-dot-net/
├── App1OltpLoadGenerator/       # OLTP load generator
│   ├── Controllers/             # REST API
│   ├── Services/                # Business logic
│   ├── Program.cs
│   ├── appsettings.json
│   ├── newrelic.config
│   └── .env.example
├── App2AnalyticsLoadGenerator/  # Analytics load generator
├── App3AnalyticsLoadGenerator/  # Duplicate of App2
├── App4AnalyticsLoadGeneratorNoApm/  # No APM version
├── oracle-setup.sql
└── README.md
```

## Troubleshooting

- **ORA-01017**: Check credentials in `.env`
- **ORA-00060 deadlock**: Expected under heavy load; app retries automatically
- **New Relic data missing**: Verify license key, wait 2-3 minutes
- **OutOfMemoryException**: Reduce THREADS value

## Technology Stack

| Java (Original) | .NET (Port) |
|----------------|-------------|
| Spring Boot | ASP.NET Core 8.0 |
| HikariCP | Oracle.ManagedDataAccess.Core pooling |
| ojdbc8 | Oracle.ManagedDataAccess.Core |
| New Relic Java Agent | New Relic .NET Agent |
| logback | Serilog |

For detailed documentation, architecture details, and advanced configuration, see the sections above.
