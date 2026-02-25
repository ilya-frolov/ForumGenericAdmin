using AutoMapper;
using Dino.Common.Files;
using Dino.Common.Hangfire;
using Dino.Common.Helpers;
using Dino.Core.AdminBL;
using Dino.Core.AdminBL.Cache;
using Dino.Core.AdminBL.Contracts;
using Dino.Core.AdminBL.Data;
using Dino.Core.AdminBL.Settings;
using Dino.CoreMvc.Admin.Contracts;
using Dino.CoreMvc.Admin.Logic;
using Dino.CoreMvc.Admin.Logic.Helpers;
using Dino.CoreMvc.Common.Files;
using Dino.Infra.Files.Uploaders;
using DinoGenericAdmin.Api.Logic.Converters;
using DinoGenericAdmin.Api.Models;
using DinoGenericAdmin.BL.Cache;
using DinoGenericAdmin.BL.Contracts;
using DinoGenericAdmin.BL.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Serialization;
using NLog;
using NLog.Web;

const string CORS_POLICY_NAME = "AllowOrigins";
const string ALL_CORS_POLICY_NAME = "AllowAllOrigins";

var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("init main");

try
{
    var builder = WebApplication.CreateBuilder(args);

    var apiConfigSection = builder.Configuration.GetSection("ApiConfig");
    var blConfigSection = builder.Configuration.GetSection("BlConfig");
    var apiConfig = apiConfigSection.Get<ApiConfig>();
    var blConfig = blConfigSection.Get<BlConfig>();

    // Add services to the container.
    builder.Services.AddControllers()
        .AddApplicationPart(typeof(Dino.CoreMvc.Admin.Controllers.DinoAdminBaseHomeController).Assembly)        // For example, for the settings. It's under the other assembly.
        .AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        });

    // NLog: Setup NLog for Dependency injection
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Auth
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.SlidingExpiration = true;
        //options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    });
    builder.Services.AddAuthorization();

    // Cors
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(name: CORS_POLICY_NAME,
            builder =>
            {
                var corsOrigins = apiConfig.AllowCorsOrigins.Split(";");
                builder.WithOrigins(corsOrigins).AllowCredentials().AllowAnyHeader().AllowAnyMethod();
            });

        options.AddPolicy(name: ALL_CORS_POLICY_NAME,
            builder =>
            {
                builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });

    });

    // Settings
    builder.Services.Configure<ApiConfig>(apiConfigSection);
    builder.Services.Configure<BlConfig>(blConfigSection);
    
    // Also configure base configurations for proper DI resolution
    builder.Services.Configure<BaseApiConfig>(apiConfigSection);
    builder.Services.Configure<BaseBlConfig>(blConfigSection);

    // Database context
    builder.Services.AddDbContext<BaseAdminDbContext, MainDbContext>();
    builder.Services.AddDbContext<DbContext, MainDbContext>();
    builder.Services.AddDbContext<MainDbContext>();

    // HttpContextAccessor
    builder.Services.AddHttpContextAccessor();

    // Admin Services
    builder.Services.AddAdminServices(builder.Configuration);
    
    // Register DinoCacheManager as the implementation for IDinoCacheManager (Singleton recommended for cache managers)
    builder.Services.AddSingleton<DinoCacheManager>();
    builder.Services.AddSingleton<IDinoCacheManager>(sp => sp.GetRequiredService<DinoCacheManager>());
    
    // Session
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
    });

    // Path Resolver
    builder.Services.AddScoped<ILocalPathResolver, WebLocalPathResolver>();
    builder.Services.AddScoped<IWebFilePathResolver, WebFilePathResolver>();

    // BL Services
    builder.Services.AddScoped<BLFactory<MainDbContext, BlConfig, DinoCacheManager>>();

    // AutoMapper
    builder.Services.AddSingleton(provider => new MapperConfiguration(cfg =>
    {
        // Register application-specific profiles
        var apiMapper = new MapperProfile(provider);
        cfg.AddProfile(apiMapper);
        cfg.AddProfile(new BLAutoMapperProfile(provider, apiMapper.PublicGetUploadsPath));

        // Initialize cache types BEFORE configuring mappings
        CacheUtils.InitializeCacheTypes();

        // Configure mappings based on CacheModelAttribute
        CacheUtils.ConfigureAutoMapperCacheMappings(cfg);

    }).CreateMapper());

    // Register AdminModelMapper implementation
    builder.Services.AddSingleton<IAdminModelMapper, AdminModelMapperImpl>();
    
    // Register SettingsProvider
    builder.Services.AddSingleton<ISettingsProvider, SettingsProvider>();

    // Add Hangfire services (Uncomment if needed)
    builder.Services.AddDinoHangfire(builder.Configuration, "Hangfire");

    // Demo jobs removed during project cleanup.

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseSession();

    app.UseRouting();


    // Allow Cors
    app.UseCors(CORS_POLICY_NAME);

    app.UseAuthentication();
    app.UseAuthorization();

    // Configure Hangfire dashboard and initialize jobs (Uncomment if needed)
    //app.UseDinoHangfire();

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllerRoute(
            name: "areas",
            pattern: "DinoAdmin/{controller=Home}/{action=Index}/{id?}",
            defaults: new {area = "Admin"}
        );

        endpoints.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
    });

    // Init Cache.
    var cacheManager = app.Services.GetService<DinoCacheManager>();
    await InitCacheAsync(cacheManager);

    // Init files
    var rootPath = app.Environment.WebRootPath.IsNotNullOrEmpty() ? app.Environment.WebRootPath : app.Environment.ContentRootPath;
    FileSystemFileUploader.Init(Path.Combine(rootPath, apiConfig.UploadsFolder));

    // Initialize AppSettings
    var settingsProvider = app.Services.GetRequiredService<ISettingsProvider>();
    await AppSettings.InitAsync(settingsProvider);

    app.Run();
}
catch (Exception exception)
{
    // NLog: catch setup errors
    logger.Error(exception, "Stopped program because of exception");
    throw;
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    NLog.LogManager.Shutdown();
}


#region InitCache

async Task InitCacheAsync(DinoCacheManager cacheManager)
{
    await cacheManager.LoadAll();
}

#endregion
