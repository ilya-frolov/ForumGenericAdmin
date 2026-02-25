# Demo Cleanup and First Initialization

This document provides guidance on cleaning up demo/example content and performing first-time initialization of the DinoGenericAdmin system.

## Overview

When you first set up DinoGenericAdmin, it comes with demo/example content that should be removed or configured for production use. This guide covers the key areas to review and clean up.

## Hangfire Background Job System

Hangfire is a background job processing system that is included for demonstration purposes but is disabled by default.

### Configuration

Hangfire initialization is controlled by the `EnableHangfire` setting in `appsettings.json`:

```json
{
  "ApiConfig": {
    "EnableHangfire": false  // Default: false (disabled)
  }
}
```

### Option 1: Remove Hangfire Completely (Recommended for simple applications)

If your application doesn't need background job processing:

1. **Remove demo job files**:
   - Delete `DinoGenericAdmin.BL/Demo/Hangfire/DemoEmailReminderJob.cs`
   - Delete `DinoGenericAdmin.BL/Demo/Hangfire/ExampleComplexJob.cs`

2. **Remove Hangfire-related code from Program.cs**:
   - Remove the `using DinoGenericAdmin.BL.Demo.Hangfire;` import
   - Remove the conditional Hangfire initialization block:
     ```csharp
     if (apiConfig.EnableHangfire)
     {
         builder.Services.AddDinoHangfire(builder.Configuration, "Hangfire");

         // Register a simple demo job
         builder.Services.AddHangfireJob<DemoEmailReminderJob>();

         // Register a complex job with dependencies using factory method
         builder.Services.AddHangfireJob<ExampleComplexJob>(provider => new ExampleComplexJob(
             provider.GetRequiredService<ILogger<ExampleComplexJob>>(),
             provider.GetRequiredService<BLFactory<MainDbContext, BlConfig, DinoCacheManager>>(),
             provider.GetRequiredService<DinoCacheManager>()
         ));
     }
     ```
   - Remove the conditional Hangfire middleware:
     ```csharp
     if (apiConfig.EnableHangfire)
     {
         app.UseDinoHangfire();
     }
     ```

3. **Remove Hangfire configuration from appsettings.json**:
   - Remove the entire `"Hangfire"` section
   - Remove `"HangfireDashboardAllowedIps"` from `"ApiConfig"`
   - Remove `"EnableHangfire"` from `"ApiConfig"`

4. **Clean up NuGet packages**:
   - Remove Hangfire-related packages from your project files if no longer needed

### Option 2: Enable Hangfire for Production Use

If your application needs background job processing:

1. **Set configuration**:
   ```json
   {
     "ApiConfig": {
       "EnableHangfire": true
     }
   }
   ```

2. **Configure Hangfire settings in appsettings.json**:
   ```json
   {
     "Hangfire": {
       "ConnectionString": "your-database-connection-string",
       "DashboardAllowedIps": ["your-allowed-ips"],
       "EnableProcessing": true,
       "EnableDashboard": true,
       "DashboardPath": "/hangfire",
       "Queues": ["critical", "emails", "default"],
       "CompatibilityLevel": 180,
       "CreateDatabaseTablesIfNotExist": true
     }
   }
   ```

3. **Replace demo jobs with your own**:
   - Remove or modify `DemoEmailReminderJob.cs` and `ExampleComplexJob.cs`
   - Implement your own job classes following the same pattern

4. **Update HangfireDashboardAllowedIps** in ApiConfig section with your allowed IP addresses for dashboard access.

## Other Demo Content to Review

### Demo Controllers

- Review `DinoGenericAdmin.Api/Areas/Admin/Controllers/Demo/` - Remove or modify demo controllers
- Review `DinoGenericAdmin.BL/Demo/` - Remove or modify demo business logic

### Demo Models

- Review `DinoGenericAdmin.BL/Models/Demo/` - Remove or modify demo models

### Configuration Settings

- Review all `appsettings.*.json` files and update connection strings, API URLs, and other environment-specific settings
- Update email configuration in `BlConfig.EmailsConfig`
- Configure storage settings in `BlConfig.StorageConfig` (Azure Blob or local file system)
- Configure cache settings in `BlConfig.CacheConfig` (Redis or in-memory)

### Security Settings

- Update CORS origins in `ApiConfig.AllowCorsOrigins`
- Configure login security in `AdminConfig.LoginSecurityConfig`
- Set appropriate password policies and user permissions

## Database Initialization

1. **Update connection strings** in `appsettings.json` and environment-specific files
2. **Run database migrations** to create the initial schema
3. **Seed initial admin user** and roles if needed
4. **Configure Hangfire database tables** if using Hangfire (handled automatically if `CreateDatabaseTablesIfNotExist` is true)

## First Run Checklist

- [ ] Update all connection strings
- [ ] Configure CORS origins for your frontend
- [ ] Set up email configuration
- [ ] Configure storage and cache settings
- [ ] Review and configure security settings
- [ ] Decide on Hangfire usage (remove or configure)
- [ ] Remove demo controllers and models
- [ ] Run database migrations
- [ ] Test admin login and basic functionality
- [ ] Configure production logging paths
