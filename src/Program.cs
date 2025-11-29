using AI_Voice_Translator_SaaS.Data;
using AI_Voice_Translator_SaaS.Interfaces;
using AI_Voice_Translator_SaaS.Repositories;
using AI_Voice_Translator_SaaS.Services;
using AIVoiceTranslator.Data;
using Microsoft.EntityFrameworkCore;
using Amazon.S3;
using Amazon.Runtime;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add DbContext
builder.Services.AddDbContext<AivoiceTranslatorContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// Register Storage Service (Local or AWS)
var storageType = builder.Configuration["StorageType"];
if (storageType == "AWS")
{
    // AWS S3 Configuration
    var awsOptions = builder.Configuration.GetSection("AWS");
    var credentials = new BasicAWSCredentials(
        awsOptions["AccessKey"],
        awsOptions["SecretKey"]
    );

    builder.Services.AddSingleton<IAmazonS3>(sp =>
        new AmazonS3Client(credentials, Amazon.RegionEndpoint.GetBySystemName(awsOptions["Region"]))
    );

    builder.Services.AddScoped<IStorageService, AwsS3StorageService>();
}
else
{
    // Local Storage (Default)
    builder.Services.AddScoped<IStorageService, LocalStorageService>();
}

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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
