using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using RentalApp.Web.Data;
using RentalApp.Web.Data.Seed;
using RentalApp.Web.Domain;
using RentalApp.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    // Every POST must carry the antiforgery token rendered by <form> tag helpers (Laravel's @csrf).
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());

    // Every action requires a signed-in user unless it is marked [AllowAnonymous].
    options.Filters.Add(new AuthorizeFilter());
});

// Registers AppDbContext as a scoped service: one instance per HTTP request.
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is missing.");
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

// ASP.NET Identity: users, roles, password hashing, and the sign-in cookie.
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// The clock is injected so tests can pin "today" (lease availability depends on it).
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<DatabaseInitializer>();
builder.Services.AddScoped<DataSeeder>();
builder.Services.AddScoped<ApplicationWorkflow>();

var app = builder.Build();

// On start: create the database, apply migrations, and seed.
await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// One culture for dates, currency, and decimal parsing, whatever the server's locale.
app.UseRequestLocalization("en-US");
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
