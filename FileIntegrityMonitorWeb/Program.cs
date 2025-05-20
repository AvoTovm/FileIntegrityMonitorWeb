using FileIntegrityMonitor.Models;
using FileIntegrityMonitor.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Define the exact path for your SQLite database file
string projectDirectory = Directory.GetCurrentDirectory();
string dbFilePath = Path.Combine(projectDirectory, "fim_web.db");
string connectionString = $"Data Source={dbFilePath}";

Console.WriteLine($"Database will be stored at: {dbFilePath}");

// Configure DbContext with explicit SQLite connection string
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// Configure Identity with basic settings
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Basic password requirements
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;

    // Disable email confirmation requirement
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure authentication cookies
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "FIM.AuthCookie";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
});

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<FileIntegrityService>();

builder.Services.AddHostedService<DatabaseInitializationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Ensure database is created and tables are set up
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // Force database creation
        bool wasCreated = context.Database.EnsureCreated();

        if (wasCreated)
        {
            Console.WriteLine("Database was created successfully.");
        }
        else
        {
            Console.WriteLine("Database already exists.");
        }

        // Verify tables were created
        bool hasUsers = context.Users.Any();
        Console.WriteLine($"Users table exists and has records: {hasUsers}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR initializing database: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }
}

app.Run();