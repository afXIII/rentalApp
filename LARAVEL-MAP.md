# Laravel to .NET map

A living cheat sheet. Each phase adds the concepts it introduced, pointing at the real file in this repo.

## Phase 0: Scaffold

### Commands run in this phase

Every command below was run from `~/tw/dotnet/RentalApp` unless noted. `dotnet new <template>` is the generator, like `laravel new` and `php artisan make:*` combined. Common flags: `-n` is the name (used for the project file and the root C# namespace), `-o` is the output directory, which is created if missing, so no `mkdir` is needed.

**1. Create the solution** (run from `~/tw/dotnet`)

```bash
dotnet new sln -n RentalApp -o RentalApp
```

Creates the `RentalApp/` folder containing an empty `RentalApp.slnx`. A solution only lists projects. It holds no code. Laravel: no equivalent.

**2. Create the web app**

```bash
dotnet new mvc -n RentalApp.Web -o src/RentalApp.Web
```

Generates an ASP.NET Core MVC app from the `mvc` template: `Program.cs`, a `HomeController`, Razor views, a layout, Bootstrap and jQuery in `wwwroot/lib`. It also ran a **restore** automatically, which downloads NuGet packages. Laravel: `laravel new` followed by `composer install`. Other templates exist, for example `webapi` (API only, what HelloApi used) and `razor` (page-based, no controllers). List them with `dotnet new list`.

**3. Create the test project**

```bash
dotnet new xunit -n RentalApp.Tests -o tests/RentalApp.Tests
```

Generates a project using the xUnit test framework with one placeholder test in `UnitTest1.cs`. Laravel: the `tests/` folder and `phpunit.xml` that ship with a new app. The `src/` and `tests/` split is a community convention, not something the tooling requires.

**4. Add both projects to the solution**

```bash
dotnet sln add src/RentalApp.Web tests/RentalApp.Tests
```

Writes the two project paths into `RentalApp.slnx`. After this, `dotnet build` and `dotnet test` at the repo root act on both. Without it you would have to `cd` into each project. Laravel: no equivalent.

**5. Let the tests see the app code**

```bash
dotnet add tests/RentalApp.Tests reference src/RentalApp.Web
```

Adds a `<ProjectReference>` line to `RentalApp.Tests.csproj`. Projects are isolated compilation units, so the test project cannot use a class from the web project until it references it. Laravel: Composer's PSR-4 autoloading makes `App\` visible to tests for free. Note the difference from `dotnet add package`, which adds a NuGet package, whereas `reference` points at another local project.

**6. Generate standard repo files**

```bash
dotnet new gitignore
dotnet new editorconfig
```

`gitignore` writes the standard .NET ignore list, most importantly `bin/` and `obj/`. `editorconfig` writes code-style rules (indentation, naming, `var` usage) that the compiler, editors, and `dotnet format` all read. Laravel: the shipped `.gitignore` and `pint.json`.

**7. Pin the migrations CLI to this repo**

```bash
dotnet new tool-manifest
dotnet tool install dotnet-ef
```

The first creates `dotnet-tools.json`, a list of CLI tools this repo needs. The second adds `dotnet-ef` (the EF Core migrations CLI) to it at version 10.0.12. Because there is no `--global` flag, it is installed for this repo only, and anyone who clones runs `dotnet tool restore` to get the same version. Laravel: a `require-dev` entry. In Laravel migrations are part of artisan, but in .NET the migrations CLI is a separate tool. It is invoked as `dotnet ef ...`.

**8. Verify: build and test everything**

```bash
dotnet test
```

At the repo root this finds the solution, restores packages, **builds** both projects, then runs every test. Result was 1 passed. Laravel: `php artisan test`, minus the compile step PHP does not have. `dotnet build` does only the compile part and is how you find type errors without running anything.

**9. Verify: the app serves pages** (run from `src/RentalApp.Web`)

```bash
dotnet run --no-build
curl -s -o /dev/null -w "%{http_code}" http://localhost:5155/
```

`dotnet run` starts the app using the first profile in `Properties/launchSettings.json`, which is where port 5155 comes from. `--no-build` skips compiling because step 8 had just built it. The `curl` prints only the status code, which was 200. For daily work use `dotnet watch` instead, which rebuilds and reloads when files change. Laravel: `php artisan serve`.

### Project shape

| This repo | What it is | Laravel equivalent |
|---|---|---|
| `RentalApp.slnx` | **Solution**: a list of projects that build and test together. Running `dotnet build` or `dotnet test` at the root uses it. | No equivalent. Closest is a monorepo root. |
| `src/RentalApp.Web/` | The web app **project**. One project compiles to one DLL. | The whole Laravel app |
| `src/RentalApp.Web/RentalApp.Web.csproj` | Project file: target framework, NuGet packages, compiler settings. | `composer.json` |
| `tests/RentalApp.Tests/` | Tests are a **separate project** that references the web project. | `tests/` folder + `phpunit.xml` |
| `dotnet-tools.json` | Repo-pinned CLI tools (here `dotnet-ef`, the migrations CLI). Restore with `dotnet tool restore`. | `require-dev` in `composer.json` |
| `.editorconfig` | Code style rules read by the compiler, editors, and `dotnet format`. | `pint.json` |
| `bin/`, `obj/` | Build output and restore cache. Git-ignored. | `vendor/` + `bootstrap/cache/` |

### Inside the web project

| File / folder | What it is | Laravel equivalent |
|---|---|---|
| `Program.cs` | App entry point. Registers services in the DI container, then defines the middleware pipeline and routes. | `bootstrap/app.php` + service providers + `routes/web.php` |
| `builder.Services.AddControllersWithViews()` | Registers MVC (controllers + Razor views) in the container. | Framework does this implicitly |
| `app.UseXxx()` lines | Middleware, executed in the order written. | Middleware stack in `bootstrap/app.php` |
| `app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}")` | **Conventional routing**: URL `/Units/Edit/5` calls `UnitsController.Edit(5)`. No route file needed. | Like `Route::resource` for everything, by naming convention |
| `Controllers/HomeController.cs` | Controller. Public methods are **actions**. `return View()` renders `Views/Home/<ActionName>.cshtml`. | `app/Http/Controllers`, `return view('home.index')` |
| `Models/ErrorViewModel.cs` | A **view model**: a typed class passed to a view. | The array you pass to `view()`, but typed |
| `Views/` | Razor templates (`.cshtml`). | `resources/views` (Blade) |
| `Views/Shared/_Layout.cshtml` | Master layout. `@RenderBody()` is where the page goes. | `layouts/app.blade.php` with `@yield('content')` / `{{ $slot }}` |
| `Views/_ViewStart.cshtml` | Runs before every view. Sets the default layout. | `@extends(...)` applied globally |
| `Views/_ViewImports.cshtml` | Namespaces and tag helpers available to all views. | Blade component/alias auto-discovery |
| `Views/Shared/_ValidationScriptsPartial.cshtml` | Partial that pulls in jQuery client-side validation. | A Blade `@include` |
| `wwwroot/` | Public web root for static files. | `public/` |
| `appsettings.json` / `appsettings.Development.json` | Layered config. | `config/*.php` + `.env` |
| `Properties/launchSettings.json` | Dev-only ports and env vars for `dotnet run`. | No equivalent |

### Razor syntax vs Blade

Paths in the last column are relative to `src/RentalApp.Web/Views/`. "Not yet" means the template does not use it and it will first appear in a later phase.

| Razor | Blade | See it in this repo |
|---|---|---|
| `@model SomeViewModel` at top of file | No equivalent (views are untyped) | `Shared/Error.cshtml` line 1 |
| `@Model.Name` | `{{ $name }}` (both HTML-escape) | `Shared/Error.cshtml` line 12 (`@Model.RequestId`) |
| `@if (x) { ... }` | `@if ... @endif` | `Shared/Error.cshtml` line 9 |
| `@foreach (var i in items) { ... }` | `@foreach ... @endforeach` | Not yet. First list page in Phase 4. |
| `@{ ... }` code block | `@php ... @endphp` | `Home/Index.cshtml` line 1, `Home/Privacy.cshtml` line 1, `_ViewStart.cshtml` line 1 |
| `ViewData["Title"] = "x"` then `@ViewData["Title"]` | `@section('title', 'x')` then `@yield('title')` | Set in `Home/Privacy.cshtml` line 2, read in `Shared/_Layout.cshtml` line 6 |
| `@RenderBody()` | `@yield('content')` / `{{ $slot }}` | `Shared/_Layout.cshtml` line 36 |
| `@RenderSection("Scripts")` in the layout | `@stack('scripts')` | `Shared/_Layout.cshtml` line 48 |
| `@section Scripts { ... }` in a page | `@push('scripts')` | Not yet. First form page in Phase 2. |
| `<partial name="_Thing" model="x" />` | `@include('thing', [...])` | Not yet. Phase 2 forms include `_ValidationScriptsPartial`. |
| `<a asp-controller="Home" asp-action="Privacy">` (tag helper) | `<a href="{{ route('home.privacy') }}">` | `Shared/_Layout.cshtml` lines 16, 24, 27, 42 |
| `asp-route-id="5"` on a link | second argument to `route()` | Not yet. Edit links in Phase 4. |
| `asp-append-version="true"` on css/js | `mix()` / Vite asset hashing | `Shared/_Layout.cshtml` lines 9, 10, 47 |
| `~/css/site.css` (`~` means web root) | `asset('css/site.css')` | `Shared/_Layout.cshtml` line 9 |
| `@Html.Raw(x)` | `{!! $x !!}` | Not yet. Avoided unless needed. |

### CLI

| .NET | Laravel |
|---|---|
| `dotnet new mvc -n X -o dir` | `laravel new` |
| `dotnet watch` (run from `src/RentalApp.Web`) | `php artisan serve` + hot reload |
| `dotnet build` | No equivalent (PHP is not compiled) |
| `dotnet test` | `php artisan test` |
| `dotnet add package X` | `composer require x` |
| `dotnet tool restore` | `composer install` for dev tools |

## Phase 1: Domain + EF Core

### Commands run in this phase

All run from the repo root, `~/tw/dotnet/RentalApp`.

**1. Add the ORM and the SQLite driver**

```bash
dotnet add src/RentalApp.Web package Microsoft.EntityFrameworkCore.Sqlite
```

`dotnet add <project> package <name>` is `composer require`. It adds a `<PackageReference>` line to `RentalApp.Web.csproj` and downloads the package. This one package brings EF Core itself plus the SQLite **provider**. A provider is what Laravel calls a database driver. In Laravel all drivers ship with the framework. In EF Core each database is its own NuGet package.

**2. Add design-time support**

```bash
dotnet add src/RentalApp.Web package Microsoft.EntityFrameworkCore.Design
```

The `dotnet ef` CLI loads this from inside the project to generate migrations. In the `.csproj` it is marked `PrivateAssets="all"`, meaning dev-only, like `require-dev`.

**3. Add Identity storage**

```bash
dotnet add src/RentalApp.Web package Microsoft.AspNetCore.Identity.EntityFrameworkCore
```

Provides `IdentityDbContext` and `IdentityUser`, which define the users and roles tables. Laravel: the `users` migration and `User` model that ship with a new app. Login and registration come in Phase 2.

**4. Compile**

```bash
dotnet build
```

Compiles every project in the solution. This is where typos, wrong types, and missing properties are caught. PHP has no equivalent step. The closest is running PHPStan.

**5. Generate a migration**

```bash
dotnet ef migrations add InitialCreate --project src/RentalApp.Web --output-dir Data/Migrations
```

Laravel: `php artisan make:migration`, with one big difference. **You do not write the migration.** EF compares your entity classes and configuration against `AppDbContextModelSnapshot.cs` (its record of the last known schema) and writes the `Up()` and `Down()` methods itself. `InitialCreate` is just the migration's name. `--project` says which project holds the context, needed because we run from the repo root. `--output-dir` is only needed the first time, later migrations go to the same folder. Undo an unapplied migration with `dotnet ef migrations remove`.

It produced three files in `Data/Migrations/`:
- `<timestamp>_InitialCreate.cs` is the migration, with `Up` and `Down`. This is the one to read.
- `<timestamp>_InitialCreate.Designer.cs` is metadata. Never edit.
- `AppDbContextModelSnapshot.cs` is the current full schema as EF understands it. Never edit. Commit it.

**6. Apply migrations**

```bash
dotnet ef database update --project src/RentalApp.Web
```

Laravel: `php artisan migrate`. Creates `src/RentalApp.Web/rentalapp.db` and records applied migrations in the `__EFMigrationsHistory` table, which is Laravel's `migrations` table. Roll back with `dotnet ef database update <PreviousMigrationName>`, or `0` for all. In Phase 3 the app will do this automatically on startup and this command becomes optional.

**7. Look at the result**

```bash
sqlite3 src/RentalApp.Web/rentalapp.db ".tables"
sqlite3 src/RentalApp.Web/rentalapp.db ".schema Units"
```

Plain SQLite CLI, nothing .NET specific. You can also open the `.db` file in TablePlus.

**8. Test**

```bash
dotnet test
```

7 tests passed.

### Concepts

| This repo | What it is | Laravel equivalent |
|---|---|---|
| `Domain/*.cs` | **Entities**: plain classes, no base class, no magic. Every column is a declared, typed property. | `app/Models/*.php` Eloquent models |
| `Domain/ApplicationUser.cs` | Inherits `IdentityUser` to add `FullName` to the users table. | `app/Models/User.php` |
| `Domain/ApplicationStatus.cs` | Enum. Stored as its name via `HasConversion<string>()`. | A backed PHP enum with an enum cast |
| `public int PropertyId` + `public Property Property` in `Domain/Unit.cs` | Foreign key column plus **navigation property**. | `belongsTo()` |
| `public List<Unit> Units` in `Domain/Property.cs` | Collection navigation. | `hasMany()` |
| `public Lease? Lease` in `Domain/RentalApplication.cs` | One-to-one navigation. | `hasOne()` |
| `string?` vs `string` | Nullable vs required. Drives `NULL` / `NOT NULL` in the migration. | `->nullable()` in a migration |
| `required string Name` | The compiler forces you to set it when you `new` the object. | No equivalent |
| `= null!` on navigation properties | "EF fills this in, do not warn me about null." A common idiom, safe here. | No equivalent |
| `DateOnly` | A date without a time. | `'date'` cast |
| `Data/AppDbContext.cs` | The **DbContext**: the database connection, one `DbSet` per table, and a **unit of work**. You change objects, then call `SaveChangesAsync()` once and EF writes all changes in one transaction. | `DB` facade + all models. Closest mental model: there is no `$model->save()`, only one `save everything` call |
| `DbSet<Unit> Units` | Query entry point: `db.Units.Where(...)`. | `Unit::query()` |
| `Data/Configurations/*.cs` (`IEntityTypeConfiguration<T>`) | Column lengths, indexes, unique constraints, delete rules. Called the **fluent API**. | The body of a migration file: `$table->string('name', 100)`, `->unique()`, `->restrictOnDelete()` |
| `OnDelete(DeleteBehavior.Restrict / Cascade)` | Foreign key delete rule. | `->restrictOnDelete()` / `->cascadeOnDelete()` |
| `HasIndex(u => new { u.PropertyId, u.UnitNumber }).IsUnique()` | Composite unique index. | `$table->unique(['property_id', 'unit_number'])` |
| `Data/MoneyToCentsConverter.cs` + `HasConversion(...)` in `UnitConfiguration` | **Value converter**: decimal dollars in C#, integer cents in the `MonthlyRentCents` column. Also works inside `Where` and `OrderBy`. | Attribute cast, or accessor + mutator |
| `Data/Migrations/` | Generated migrations. | `database/migrations/` |
| `ConnectionStrings:Default` in `appsettings.json` | Connection string. | `DB_*` keys in `.env` |
| `builder.Services.AddDbContext<AppDbContext>(...)` in `Program.cs` | Registers the context in the DI container as **scoped**, one instance per HTTP request. Controllers and services receive it through their constructor. | A binding in a service provider. In Laravel the DB connection is global via facades, here it is always injected. |

### The mindset shift: model first, migration second

In Laravel you write the migration, and the model discovers its columns at runtime. In EF Core it is the reverse: **the C# classes are the source of truth** and migrations are generated from them. The workflow for any schema change is:

1. Edit an entity in `Domain/` or a rule in `Data/Configurations/`.
2. `dotnet ef migrations add DescribeTheChange --project src/RentalApp.Web`
3. Read the generated file to confirm it does what you meant.
4. `dotnet ef database update --project src/RentalApp.Web`

### Testing terms

| xUnit | PHPUnit / Pest |
|---|---|
| `[Fact]` | A test method / `it(...)` |
| `[Theory]` + `[InlineData(...)]` | `@dataProvider` / `->with([...])` |
| `Assert.Equal(expected, actual)` | `assertEquals` |
| `new SqliteConnection("Data Source=:memory:")` + `EnsureCreatedAsync()` | `RefreshDatabase` with `:memory:` sqlite |

See `tests/RentalApp.Tests/Data/MoneyToCentsConverterTests.cs`.

## Phase 2: SQL Server, Identity, sign-up and log-in

The brief requires SQL Server, so SQLite was replaced. Migrations are generated for one specific database provider, so the Phase 1 migration was deleted and regenerated. Tests still use in-memory SQLite, because they build the schema with `EnsureCreated` instead of migrations.

### Commands run in this phase

**1. Start SQL Server in a container** (Apple `container` CLI, Docker-compatible flags)

```bash
container system start
container volume create rentalapp-sqldata
container run -d --name rentalapp-sql --platform linux/amd64 --rosetta -u root -c 4 -m 4G \
  -e ACCEPT_EULA=Y -e MSSQL_PID=Developer -e 'MSSQL_SA_PASSWORD=RentalApp!Local1' \
  -p 127.0.0.1:1434:1433 -v rentalapp-sqldata:/var/opt/mssql \
  mcr.microsoft.com/mssql/server:2022-latest
```

SQL Server has no ARM build, so it runs under Rosetta. `-u root` is needed because the image's default `mssql` user cannot write to a fresh volume. Host port 1434 avoids clashing with another project's SQL Server on 1433. Docker equivalent: `docker run` with the same flags. Laravel Sail does the same with `sail up`. Day to day: `container start rentalapp-sql` / `container stop rentalapp-sql`.

**2. Swap the provider packages**

```bash
dotnet add src/RentalApp.Web package Microsoft.EntityFrameworkCore.SqlServer --version 10.0.12
dotnet remove src/RentalApp.Web package Microsoft.EntityFrameworkCore.Sqlite
dotnet add tests/RentalApp.Tests package Microsoft.EntityFrameworkCore.Sqlite --version 10.0.12
dotnet add src/RentalApp.Web package System.Security.Cryptography.Pkcs --version 10.0.13
```

The last line pins a patched version of a package that SqlClient pulls in indirectly. The build warned (`NU1903`) that the indirect version had a known vulnerability. A direct reference wins over an indirect one. Laravel: adding a constraint to `composer.json` to force a fixed version of a sub-dependency.

**3. Keep the local connection string out of git**

```bash
dotnet user-secrets init --project src/RentalApp.Web
dotnet user-secrets set "ConnectionStrings:Default" 'Server=127.0.0.1,1434;Database=RentalApp;User Id=sa;Password=RentalApp!Local1;TrustServerCertificate=True;MultipleActiveResultSets=true' --project src/RentalApp.Web
```

`init` writes a `UserSecretsId` into the `.csproj`. `set` stores the value in `~/.microsoft/usersecrets/<id>/secrets.json`, outside the repo. In Development it overrides `appsettings.json`, which keeps the LocalDB default a Windows reviewer can use as is. Laravel: `.env`, except that the file never sits in the project folder.

**4. Regenerate and apply the migration**

```bash
git rm src/RentalApp.Web/Data/Migrations/*.cs
dotnet ef migrations add InitialCreate --project src/RentalApp.Web --output-dir Data/Migrations
dotnet ef database update --project src/RentalApp.Web
```

From now on the app also runs `MigrateAsync()` on startup (`Data/Seed/DatabaseInitializer.cs`), so `database update` is optional.

### Concepts

| This repo | What it is | Laravel equivalent |
|---|---|---|
| `AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<AppDbContext>()` in `Program.cs` | Users, roles, password hashing, sign-in cookie. Tables come from `IdentityDbContext`. | Breeze/Fortify + `spatie/laravel-permission` |
| `UserManager<ApplicationUser>` | Create users, hash passwords, add roles. | `User::create` + `Hash::make` + `assignRole` |
| `SignInManager<ApplicationUser>` | Check a password and issue the cookie. | `Auth::attempt()` / `Auth::login()` |
| `RoleManager<IdentityRole>` | Create roles. | `Role::create` (spatie) |
| `Controllers/AccountController.cs` | Hand-written MVC sign-up, log-in, log-out. The role is chosen on sign-up. | Breeze auth controllers |
| `Domain/Roles.cs` | Role names as constants. | Role name strings / enum |
| `options.Filters.Add(new AuthorizeFilter())` | Every action needs a signed-in user unless it has `[AllowAnonymous]`. | `auth` middleware on the whole route group |
| `[Authorize(Roles = Roles.PropertyManager)]` | Role gate on a controller or action. | `->middleware('role:manager')` / `Gate` |
| `AutoValidateAntiforgeryTokenAttribute` | CSRF check on every POST. `<form>` tag helpers add the hidden token automatically. | `VerifyCsrfToken` + `@csrf` |
| `User.IsInRole(...)` in a view | Current user's role. | `@role(...)` / `auth()->user()->hasRole()` |
| `TempData["Success"]` + `Views/Shared/_Flash.cshtml` | One-request message that survives a redirect. | `session()->flash()` / `->with('success', ...)` |
| `LocalRedirect(returnUrl)` + `Url.IsLocalUrl` | Redirect back after log-in, refusing off-site URLs. | `redirect()->intended()` |
| `TimeProvider` registered as a singleton | Injectable clock so tests can pin "today". | `Carbon::setTestNow()` |
| `dotnet user-secrets` | Developer secrets outside the repo. | `.env` |

## Phase 3: Business rules, seeding, tests

### Commands run in this phase

```bash
dotnet add src/RentalApp.Web package Bogus
dotnet ef migrations has-pending-model-changes --project src/RentalApp.Web
dotnet test
```

`has-pending-model-changes` checks whether the entities have drifted from the last migration without generating anything. It is useful before committing. Laravel has no equivalent, because there the migration is the source of truth. The app was then started twice, and the row counts in SQL Server were compared to confirm the seeder is idempotent:

```bash
container exec rentalapp-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P 'RentalApp!Local1' -d RentalApp \
  -Q "select Status, count(*) from RentalApplications group by Status"
```

Laravel: `php artisan tinker` or `DB::table(...)->count()`.

### Concepts

| This repo | What it is | Laravel equivalent |
|---|---|---|
| `Domain/RentalApplication.cs` methods `Submit`, `Claim`, `Approve`, ... | The status machine lives on the entity. `Status { get; private set; }` means only the class itself can change it. EF can still write private setters when loading. | Model methods + `$guarded`, or a state machine package |
| `DomainRuleException` | A business rule was broken. Controllers turn it into a message for the user. | `ValidationException::withMessages()` / `abort(422)` |
| `Services/ApplicationWorkflow.cs` | A use-case class: loads, calls the domain, saves. Registered with `AddScoped` and constructor-injected. | An Action / Service class resolved from the container |
| `Include(a => a.Unit).ThenInclude(u => u.Leases)` | Eager loading. | `with('unit.leases')` |
| `Expression<Func<Unit, bool>> IsAvailableOn(date)` in `Unit.cs` | A reusable filter EF translates to SQL. | A local query scope `scopeAvailable()` |
| `.IsConcurrencyToken()` on `Version` / `LeaseVersion` | **Optimistic concurrency.** EF adds `WHERE Version = <read value>` to the UPDATE. If another request changed it first, zero rows match and EF throws `DbUpdateConcurrencyException`. | No built-in. Done by hand with `where('version', $v)->update()` and checking affected rows |
| `Data/Seed/DataSeeder.cs` with `Faker` | Fake data with a fixed random seed, so every run generates the same names and numbers. | `DatabaseSeeder` + factories using Faker |
| `Data/Seed/DatabaseInitializer.cs` | Migrate, then seed, on every start. Each step checks before inserting. | `migrate --seed` in a deploy script, with `firstOrCreate` |

### Testing terms

| This repo | What it is | PHPUnit / Pest |
|---|---|---|
| `tests/.../Support/FixedClock.cs` | A `TimeProvider` subclass frozen at one instant. | `Carbon::setTestNow()` |
| `tests/.../Support/TestDatabase.cs` | In-memory SQLite. `CreateContext()` returns a second context on the same data, which simulates two concurrent requests. | `RefreshDatabase` |
| `tests/.../Data/DataSeederTests.cs` `new ServiceCollection()...BuildServiceProvider()` | Builds a mini DI container with only what the seeder needs. | `$this->app->make(...)` |
| `Assert.Throws<T>(() => ...)` / `await Assert.ThrowsAsync<T>(...)` | Expect an exception. | `expectException()` / `->throws()` |
| `using static ...Applications;` | Lets tests call `Draft()` instead of `Applications.Draft()`. | `use function` |
