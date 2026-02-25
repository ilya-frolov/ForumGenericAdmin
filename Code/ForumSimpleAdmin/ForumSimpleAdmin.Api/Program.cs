using AutoMapper;
using Dino.Common.Files;
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
using ForumSimpleAdmin.Api.Logic.Converters;
using ForumSimpleAdmin.Api.Models;
using ForumSimpleAdmin.BL.BL;
using ForumSimpleAdmin.BL.Cache;
using ForumSimpleAdmin.BL.Contracts;
using ForumSimpleAdmin.BL.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Serialization;
using NLog.Web;

const string CORS_POLICY_NAME = "AllowOrigins";

var builder = WebApplication.CreateBuilder(args);

var apiConfigSection = builder.Configuration.GetSection("ApiConfig");
var blConfigSection = builder.Configuration.GetSection("BlConfig");
var apiConfig = apiConfigSection.Get<ApiConfig>() ?? new ApiConfig();

builder.Services.AddControllers()
    .AddApplicationPart(typeof(Dino.CoreMvc.Admin.Controllers.DinoAdminBaseHomeController).Assembly)
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
    });

builder.Logging.ClearProviders();
builder.Host.UseNLog();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.SlidingExpiration = true;
});
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CORS_POLICY_NAME, corsBuilder =>
    {
        string allowCorsOrigins = apiConfig.AllowCorsOrigins ?? string.Empty;
        string[] corsOrigins = allowCorsOrigins.Split(";", StringSplitOptions.RemoveEmptyEntries);
        if (corsOrigins.Length == 0)
        {
            corsBuilder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            corsBuilder.WithOrigins(corsOrigins).AllowCredentials().AllowAnyHeader().AllowAnyMethod();
        }
    });
});

builder.Services.Configure<ApiConfig>(apiConfigSection);
builder.Services.Configure<BlConfig>(blConfigSection);
builder.Services.Configure<BaseApiConfig>(apiConfigSection);
builder.Services.Configure<BaseBlConfig>(blConfigSection);

builder.Services.AddDbContext<BaseAdminDbContext, MainDbContext>();
builder.Services.AddDbContext<DbContext, MainDbContext>();
builder.Services.AddDbContext<MainDbContext>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddAdminServices(builder.Configuration);

builder.Services.AddSingleton<DinoCacheManager>();
builder.Services.AddSingleton<IDinoCacheManager>(sp => sp.GetRequiredService<DinoCacheManager>());

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

builder.Services.AddScoped<ILocalPathResolver, WebLocalPathResolver>();
builder.Services.AddScoped<IWebFilePathResolver, WebFilePathResolver>();
builder.Services.AddScoped<BLFactory<MainDbContext, BlConfig, DinoCacheManager>>();

builder.Services.AddSingleton(provider => new MapperConfiguration(cfg =>
{
    var apiMapper = new MapperProfile(provider);
    cfg.AddProfile(apiMapper);
    cfg.AddProfile(new BLAutoMapperProfile(provider, apiMapper.PublicGetUploadsPath));
    CacheUtils.InitializeCacheTypes();
    CacheUtils.ConfigureAutoMapperCacheMappings(cfg);
}).CreateMapper());

builder.Services.AddSingleton<IAdminModelMapper, AdminModelMapperImpl>();
builder.Services.AddSingleton<ISettingsProvider, SettingsProvider>();

var app = builder.Build();

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
app.UseCors(CORS_POLICY_NAME);
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "areas",
        pattern: "DinoAdmin/{controller=Home}/{action=Index}/{id?}",
        defaults: new { area = "Admin" }
    );

    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
});

var cacheManager = app.Services.GetRequiredService<DinoCacheManager>();
await InitCacheAsync(cacheManager);

var rootPath = app.Environment.WebRootPath.IsNotNullOrEmpty() ? app.Environment.WebRootPath : app.Environment.ContentRootPath;
FileSystemFileUploader.Init(Path.Combine(rootPath, apiConfig.UploadsFolder));

var settingsProvider = app.Services.GetRequiredService<ISettingsProvider>();
await AppSettings.InitAsync(settingsProvider);
await EnsureForumInitializedAsync(app.Services);

app.Run();

async Task InitCacheAsync(DinoCacheManager cacheManager)
{
    await cacheManager.LoadAll();
}

async Task EnsureForumInitializedAsync(IServiceProvider services)
{
    using IServiceScope scope = services.CreateScope();
    MainDbContext db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
    await db.Database.EnsureCreatedAsync();

    BLFactory<MainDbContext, BlConfig, DinoCacheManager> blFactory =
        scope.ServiceProvider.GetRequiredService<BLFactory<MainDbContext, BlConfig, DinoCacheManager>>();

    ForumBL forumBl = blFactory.GetBL<ForumBL>(forceNewContext: false);
    await forumBl.EnsureInitializedAsync();
}
