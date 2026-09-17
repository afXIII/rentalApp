using Microsoft.EntityFrameworkCore;
using RentalApp.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Registers AppDbContext as a scoped service: one instance per HTTP request.
// The provider is chosen by config so the same code can target SQL Server later.
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is missing.");
var provider = builder.Configuration["Database:Provider"];

builder.Services.AddDbContext<AppDbContext>(options =>
{
    switch (provider)
    {
        case "Sqlite":
            options.UseSqlite(connectionString);
            break;
        default:
            throw new InvalidOperationException($"Unsupported Database:Provider '{provider}'.");
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
