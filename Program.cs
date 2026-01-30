using Microsoft.EntityFrameworkCore;
using TmtInventoryApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add session support for authentication
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8); // Session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure anti-forgery tokens to prevent expiration issues
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
});

// Add response caching to prevent stale pages
builder.Services.AddResponseCaching();

builder.Services.AddDbContext<TmtInventoryApp.Data.InventoryContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("InventoryContext")));

// Register ActivityLogger service
builder.Services.AddScoped<TmtInventoryApp.Services.AuthService>();
builder.Services.AddScoped<TmtInventoryApp.Services.ActivityLogger>();
builder.Services.AddScoped<TmtInventoryApp.Services.StockHistoryService>();
builder.Services.AddScoped<TmtInventoryApp.Services.DealerPortalSeeder>();

var app = builder.Build();

// Auto-apply migrations
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<TmtInventoryApp.Data.InventoryContext>();
        context.Database.Migrate();
        
        // Seed dealer portal data
        var seeder = services.GetRequiredService<TmtInventoryApp.Services.DealerPortalSeeder>();
        seeder.SeedAsync().GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
        // Continue running the app even if seeding fails
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseResponseCaching(); // Enable response caching

app.UseSession(); // Enable session middleware

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Welcome}/{id?}")
    .WithStaticAssets();


app.Run();