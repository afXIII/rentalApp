using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
})
// The JSON API writes enums by name ("UnderReview"), not number.
.AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// The OpenAPI generator reads these (minimal-API) JSON options, so the document shows enum names too.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// OpenAPI document for the JSON endpoints ([ApiController] only). Served at /openapi/v1.json.
builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    document.Info.Title = "RentalApp API";
    document.Info.Description = "JSON endpoints used by the RentalApp pages. Authenticated with the site's sign-in cookie.";
    return Task.CompletedTask;
}));

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

    // AJAX (modal) and API callers get a status code instead of a redirect to an HTML page.
    options.Events.OnRedirectToLogin = context => StatusOrRedirect(context, StatusCodes.Status401Unauthorized);
    options.Events.OnRedirectToAccessDenied = context => StatusOrRedirect(context, StatusCodes.Status403Forbidden);

    static Task StatusOrRedirect(RedirectContext<CookieAuthenticationOptions> context, int statusCode)
    {
        var request = context.Request;
        if (request.Headers.XRequestedWith == "XMLHttpRequest" || request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = statusCode;
        }
        else
        {
            context.Response.Redirect(context.RedirectUri);
        }

        return Task.CompletedTask;
    }
});

// The clock is injected so tests can pin "today" (lease availability depends on it).
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<DatabaseInitializer>();
builder.Services.AddScoped<DataSeeder>();
builder.Services.AddScoped<ApplicationWorkflow>();
builder.Services.AddScoped<ApplicationPageBuilder>();
builder.Services.AddScoped<ApplicationListService>();

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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "RentalApp API"));
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
