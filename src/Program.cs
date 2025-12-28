using AI_Voice_Translator_SaaS.Data;
using AI_Voice_Translator_SaaS.Interfaces;
using AI_Voice_Translator_SaaS.Jobs;
using AI_Voice_Translator_SaaS.Repositories;
using AI_Voice_Translator_SaaS.Services;
using AI_Voice_Translator_SaaS.Middleware;
using AIVoiceTranslator.Data;
using Hangfire;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;

var builder = WebApplication.CreateBuilder(args);

// Add Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add HttpContextAccessor for accessing HttpContext in services
builder.Services.AddHttpContextAccessor();

// Add Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";
})
.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
{
    options.ClientId = builder.Configuration["OAuth:Google:ClientId"] ?? "";
    options.ClientSecret = builder.Configuration["OAuth:Google:ClientSecret"] ?? "";
})
.AddFacebook(FacebookDefaults.AuthenticationScheme, options =>
{
    options.AppId = builder.Configuration["OAuth:Facebook:AppId"] ?? "";
    options.AppSecret = builder.Configuration["OAuth:Facebook:AppSecret"] ?? "";
})
.AddTwitter(TwitterDefaults.AuthenticationScheme, options =>
{
    options.ConsumerKey = builder.Configuration["OAuth:Twitter:ConsumerKey"] ?? "";
    options.ConsumerSecret = builder.Configuration["OAuth:Twitter:ConsumerSecret"] ?? "";
});

// Add DbContext
builder.Services.AddDbContext<AivoiceTranslatorContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Hangfire
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();

builder.Services.AddHealthChecks().AddDbContextCheck<AivoiceTranslatorContext>().AddRedis(builder.Configuration.GetConnectionString("Redis"));
// Add Session
builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

// Register Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAudioFileRepository, AudioFileRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAudioDurationService, AudioDurationService>();
builder.Services.AddScoped<ITranslationService, AzureTranslationService>();
builder.Services.AddScoped<ISpeechService, AzureSpeechService>();
builder.Services.AddScoped<ITTSService, AzureTTSService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IMoMoPaymentService, MoMoPaymentService>();
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

// Register Jobs
builder.Services.AddScoped<ProcessAudioJob>();
builder.Services.AddScoped<SubscriptionExpiryJob>();

// Register Storage Service (Local or Azure)
var storageType = builder.Configuration["StorageType"];
if (storageType == "Azure")
{
    builder.Services.AddScoped<IStorageService, AzureBlobStorageService>();
}
else
{
    builder.Services.AddScoped<IStorageService, LocalStorageService>();
}

builder.Services.AddHttpClient();

// Add Redis Caching
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "AIVoiceTranslator_";
});

builder.Services.AddMemoryCache();

var app = builder.Build();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AivoiceTranslatorContext>();
        await SeedData.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

// Load production config
if (builder.Environment.IsProduction())
{
    builder.Configuration.AddJsonFile("appsettings.Production.json", optional: true, reloadOnChange: true);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();

app.UseResponseCompression();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache static files for 7 days
        ctx.Context.Response.Headers.Append(
            "Cache-Control", "public,max-age=604800");
    }
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

app.UseMiddleware<PerformanceMiddleware>();

app.MapHealthChecks("/health");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Schedule recurring job to check expired subscriptions (runs daily at 2 AM UTC)
using (var scope = app.Services.CreateScope())
{
    var subscriptionExpiryJob = scope.ServiceProvider.GetRequiredService<SubscriptionExpiryJob>();
    Hangfire.RecurringJob.AddOrUpdate(
        "check-expired-subscriptions",
        () => subscriptionExpiryJob.CheckAndDowngradeExpiredSubscriptions(),
        Hangfire.Cron.Daily(2));
}

app.Run();

// Hangfire Authorization Filter
public class HangfireAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context)
    {
        // Allow all in development, add auth in production
        return true;
    }
}
