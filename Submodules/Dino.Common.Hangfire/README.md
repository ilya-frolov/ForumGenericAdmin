# Dino.Common.Hangfire

A reusable Hangfire integration library for Dino projects, providing structured job scheduling, authorization, and configuration.

## Features

- Easy service registration with extension methods
- IP-based authorization filter for dashboard access
- Base job structure for creating recurring jobs
- Job registration extensions for different registration patterns
- Standardized job scheduling and execution
- Configurable dashboard visibility
- Automatic database schema creation

## Installation

Add a reference to the Dino.Common.Hangfire project in your solution.

## Usage

### 1. Configuration

Add Hangfire configuration in your `appsettings.json`:

```json
{
  "Hangfire": {
    "ConnectionString": "Server=...;Database=HangfireDb;Trusted_Connection=True;",
    "DashboardAllowedIps": ["127.0.0.1", "::1"],
    "EnableProcessing": true,
    "EnableDashboard": true,
    "DashboardPath": "/hangfire",
    "Queues": ["default", "critical", "emails"],
    "CompatibilityLevel": 180,
    "CreateDatabaseTablesIfNotExist": true
  }
}
```

#### Configuration Options

| Option | Description | Default |
|--------|-------------|---------|
| ConnectionString | Database connection string for Hangfire storage | null |
| DashboardAllowedIps | IP addresses allowed to access the dashboard | [] |
| EnableProcessing | Whether to run job processing on this instance | true |
| EnableDashboard | Whether to expose the Hangfire dashboard UI | true |
| DashboardPath | URL path for the dashboard | "/hangfire" |
| Queues | List of job queues to process | ["default"] |
| CompatibilityLevel | Hangfire compatibility level | 180 |
| CreateDatabaseTablesIfNotExist | Create DB tables if they don't exist | true |

#### Understanding Hangfire Queues

Hangfire queues allow you to categorize and prioritize different types of jobs:

1. **What are queues?**
   - Queues are named job channels that help organize and process jobs
   - Each job is assigned to a specific queue
   - Workers can be configured to process specific queues

2. **Common queue usage patterns:**
   - `default` - General purpose jobs
   - `critical` - High-priority jobs that need immediate processing
   - `emails` - Email sending jobs that might have different throughput needs
   - `long-running` - Jobs that take a long time to complete

3. **Queue processing:**
   - Queues are processed in the order they're defined in the configuration
   - Workers will first process all jobs from the first queue before moving to the next one
   - This allows for prioritization of certain job types

4. **Example:**
   ```
   Queues: ["critical", "default", "low"]
   ```
   With this configuration, critical jobs will be processed first, then default jobs, and finally low-priority jobs.

### 2. Register services in Program.cs

```csharp
// Register Hangfire services
builder.Services.AddDinoHangfire(builder.Configuration);

// Register custom jobs
builder.Services.AddHangfireJob<YourCustomJob>();

// Or register with a factory function
builder.Services.AddHangfireJob<AnotherCustomJob>(provider => 
    new AnotherCustomJob(
        provider.GetRequiredService<ILogger<AnotherCustomJob>>(), 
        provider.GetRequiredService<YourDependency>()));
```

### 3. Configure the application

```csharp
// Configure Hangfire dashboard and initialize jobs
app.UseDinoHangfire();
```

### 4. Creating custom jobs

```csharp
public class YourCustomJob : BaseHangfireJob
{
    private readonly YourDependency _dependency;

    public YourCustomJob(ILogger<YourCustomJob> logger, YourDependency dependency) 
        : base(logger)
    {
        _dependency = dependency;
    }

    public override string JobName => "YourCustomJob";
    public override string CronSchedule => "0 */6 * * *"; // Every 6 hours
    public override string Queue => "critical";

    protected override async Task ExecuteJobAsync()
    {
        // Your job implementation here
        await _dependency.DoSomethingAsync();
    }
}
```

## Database Tables

When `CreateDatabaseTablesIfNotExist` is set to `true` (default), Hangfire will automatically create the following tables in your database if they don't exist:

- `HangFire.AggregatedCounter`
- `HangFire.Counter`
- `HangFire.Hash`
- `HangFire.Job`
- `HangFire.JobParameter`
- `HangFire.JobQueue`
- `HangFire.List`
- `HangFire.Lock`
- `HangFire.Schema`
- `HangFire.Server`
- `HangFire.Set`
- `HangFire.State`

You don't need to run any migrations or scripts to set up the database.

## License

Copyright (c) Dino. All rights reserved. 