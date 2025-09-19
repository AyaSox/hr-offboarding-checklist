using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OffboardingChecklist.BackgroundServices;
using OffboardingChecklist.Data;
using OffboardingChecklist.Services;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Bind to Render-provided PORT if present
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Decide database provider: Default to SQLite in production, SqlServer for development
var dbProvider = builder.Configuration["DatabaseProvider"]
                 ?? Environment.GetEnvironmentVariable("DATABASE_PROVIDER")
                 ?? (builder.Environment.IsProduction() ? "Sqlite" : "SqlServer");

if (dbProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
{
    // Determine SQLite file path
    var sqlitePath = Environment.GetEnvironmentVariable("SQLITE_DB_PATH");
    if (string.IsNullOrWhiteSpace(sqlitePath))
    {
        var dataDir = builder.Environment.IsProduction() 
            ? "/app/data" 
            : Path.Combine(AppContext.BaseDirectory, "data");
        Directory.CreateDirectory(dataDir);
        sqlitePath = Path.Combine(dataDir, "offboarding.db");
    }
    else
    {
        var dir = Path.GetDirectoryName(sqlitePath);
        if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
    }

    var sqliteConn = builder.Configuration.GetConnectionString("SqliteConnection")
                     ?? $"Data Source={sqlitePath};Cache=Shared";

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(sqliteConn));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                           ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
}

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddRazorPages();

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false; // Disable email confirmation for demo
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
    })
    .AddRoles<IdentityRole>() // Add role support
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

// Add API support with Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Employee Offboarding API",
        Version = "v1",
        Description = "API for managing employee offboarding processes"
    });
});

// Register custom services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ITaskGenerationService, TaskGenerationService>();

// Add background services
builder.Services.AddHostedService<OffboardingReminderService>();

// Add memory cache for performance
builder.Services.AddMemoryCache();

// Lightweight health checks
builder.Services.AddHealthChecks();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Employee Offboarding API V1");
        c.RoutePrefix = "api-docs";
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// Custom health endpoint that verifies DB connectivity
app.MapGet("/health", async (ApplicationDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        return canConnect
            ? Results.Ok(new { status = "Healthy" })
            : Results.Problem("Database unreachable", statusCode: 503);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Health check failed: {ex.Message}", statusCode: 503);
    }
});

// Seed the database (create roles and admin user)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    try
    {
        context.Database.Migrate();
        await DbInitializer.SeedAsync(context, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();
