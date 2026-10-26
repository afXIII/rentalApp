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

## Phase 4: Properties, units, and unit types through modals

No new packages or CLI commands. The endpoints were smoke-tested with `curl` against the running app. That meant logging in with a cookie jar, reading the antiforgery token from the form partial, and POSTing with it, checking for 204 on success and 422 on validation errors.

### The modal protocol

`wwwroot/js/site.js` handles every modal in the app. A button carries `data-modal-url` (the GET that returns the form partial) and either `data-refresh-target` + `data-refresh-url` (the region to re-fetch on success) or `data-refresh="page"`.

| Step | Server (`Controllers/AppController.cs`) | Laravel equivalent |
|---|---|---|
| Open | `return PartialView("_UnitForm", model);` | `return view('units._form', ...)` returned to an AJAX call |
| Invalid | `InvalidModal(...)`: status **422** + the same partial, now with errors | A 422 from `$request->validate()`, but carrying HTML instead of JSON |
| Saved | `ModalSuccess()`: **204 No Content** | `return response()->noContent();` |
| Refresh | `Units/Table` returns the `_UnitTable` partial again | A route that returns just a Blade partial (Livewire / htmx style) |

### Concepts

| This repo | What it is | Laravel equivalent |
|---|---|---|
| `Controllers/AppController.cs` | A base controller with shared helpers. | `app/Http/Controllers/Controller.php` |
| `PartialView("_Name", model)` | Renders a view without the layout. A leading `_` marks a partial by convention. | `view('partials.name')` with no `@extends` |
| `<partial name="_UnitTable" model="Model.Units" />` | Includes a partial with its own model. | `@include('units.table', ['units' => ...])` |
| `Views/Shared/` | Partials found from any controller's views. | Any shared Blade folder |
| `asp-for="UnitNumber"` | Generates `id`, `name`, `value` and client-validation attributes from the view model property. | `name="unit_number" value="{{ old('unit_number', $unit->unit_number) }}"` plus `@error` |
| `<span asp-validation-for="X">` / `<div asp-validation-summary="ModelOnly">` | One field's error / errors not tied to a field. | `@error('x')` / `$errors->first()` |
| `<select asp-for="UnitTypeId" asp-items="Model.UnitTypeOptions">` | A dropdown from a list of `SelectListItem`. | `@foreach ($types as $t) <option ...>` |
| `[Required]`, `[StringLength]`, `[Range]` on view model properties | Validation rules, checked during model binding. Also emitted as `data-val-*` attributes for jQuery client-side validation. | `$request->validate([...])` rules |
| `ModelState.IsValid` / `ModelState.AddModelError("Field", "msg")` | Result of validation / add a server-side error (for example a database check). | `$validator->fails()` / `$validator->errors()->add()` |
| `[ValidateNever]` | Excludes a property from validation (dropdown options are output, not input). | Not listing it in the rules |
| `[HttpPost, ActionName(nameof(Delete))] DeleteConfirmed(int id)` | Two C# methods cannot share a signature, so the POST gets another name and is routed as `Delete`. | Two routes to two methods |
| `Url.Action("Edit", "Units", new { id })` | Builds a URL from routing. | `route('units.edit', $id)` |
| `.Select(u => new UnitRowViewModel { ... })` | **Projection**: EF selects only those columns in one SQL query, including counts and sub-queries. No N+1. | `->select()` + `withCount()` |
| `db.Units.FindAsync(id)` | Load by primary key (checks already-tracked entities first). | `Unit::find($id)` |
| `AsNoTracking()` | Read-only query. EF does not watch the objects for changes, which is cheaper. | No equivalent (Eloquent never tracks) |
| `UnitType.IsSelectableFor(currentTypeId)` checked in `UnitsController.ValidateAsync` | The inactive-type rule is enforced on POST, not only by hiding options. | A custom validation `Rule` |
| `app.UseRequestLocalization("en-US")` | Fixes the culture for `ToString("C")`, dates, and decimal parsing. | `App::setLocale()` + `setlocale()` |

## Phase 5: Browse units, the application page, residences

### Commands run in this phase

No new packages. After a reboot the container service has to be started again before the app can reach SQL Server:

```bash
container system start
container start rentalapp-sql
container logs rentalapp-sql | tail     # wait for "Recovery is complete"
```

Laravel Sail: `sail up -d`. The applicant flow was then smoke-tested with `curl`: apply, continue with invalid data, continue with valid data, add a residence through the modal endpoint, submit, then try to edit again (rejected), and withdraw.

### How the application page works

One URL (`/Applications/Details/{id}?step=...`), one view model (`Models/Applications/ApplicationPageViewModel.cs`), one form, one POST action (`ApplicationsController.Details`).

| Piece | File | Laravel equivalent |
|---|---|---|
| Page | `Views/Applications/Details.cshtml` | One Blade page with a `$step` variable |
| Section 1 (partial view) | `Views/Applications/_ApplicantInfoSection.cshtml` | `@include('applications.sections.info')` |
| Section 2 (view component) | `ViewComponents/ResidenceHistoryViewComponent.cs` + `Views/Shared/Components/ResidenceHistory/Default.cshtml` | A Blade class component with its own query (`<x-residence-history />`) |
| Summary | `Views/Applications/_SummarySection.cshtml`. Renders the same partial and component, read-only. | An `@include` of both, with an `editable => false` flag |
| Editable or read-only | `Model.IsEditing(section)`, computed on the server from owner + Draft/Returned + current step | A `@can('update', $application)` check inside the partial |
| Which button was clicked | `<button name="command" value="continue">`, and the action takes a `string command` parameter | `$request->input('command')` |

### Concepts

| This repo | What it is | Laravel equivalent |
|---|---|---|
| `ViewComponent` class + `InvokeAsync(...)` | A reusable piece of UI with its own logic and data loading. Looked up in `Views/Shared/Components/<Name>/Default.cshtml`. | Blade class component (`app/View/Components`) |
| `<vc:residence-history application-id="Model.Id" editable="false">` | Tag-helper syntax to invoke a view component. PascalCase becomes kebab-case. Needs `@addTagHelper *, RentalApp.Web` in `_ViewImports.cshtml`. | `<x-residence-history :application-id="$id" />` |
| `return ViewComponent("ResidenceHistory", new { applicationId, editable })` in `ResidencesController.List` | A controller action returns just the component's HTML, which is how the list refreshes after the modal. | Returning `view('components.residence-history')` from a route |
| `[Bind(Prefix = "ApplicantInfo")] ApplicantInfoSection applicantInfo` | Binds the form fields `ApplicantInfo.FullName`, ... into a small object. The prefix matches what `asp-for="ApplicantInfo.FullName"` generated. | `$request->input('applicant_info')` with dot-notation rules (`applicant_info.full_name`) |
| `RedirectToAction(nameof(Details), new { id, step })` after saving | **Post/Redirect/Get**, so a browser refresh does not re-post. | `return redirect()->route(...)` after a POST |
| `return View(nameof(Details), model)` on invalid | Re-render the same page with `ModelState` errors and the posted values. | `back()->withErrors()->withInput()`, except no redirect is needed |
| `ModelState.Clear()` | Drop binding errors for fields the current step did not post. | Validating only the rules for the current step |
| `IValidatableObject.Validate(...)` on `ResidenceFormViewModel` | Cross-field rules (move-out after move-in, not in the future). | `'move_out' => 'after_or_equal:move_in'` |
| `validationContext.GetService(typeof(TimeProvider))` | Resolve a service inside validation. | `app(Clock::class)` inside a custom Rule |
| `<input asp-for="MoveInDate" type="date" asp-format="{0:yyyy-MM-dd}">` | A `DateOnly` field. The browser date picker needs ISO format. | `value="{{ $date->format('Y-m-d') }}"` |
| `formnovalidate` + `class="cancel"` on Back | Skips jQuery client-side validation for that button. | `formnovalidate` |
| `disabled="@(!Model.CanSubmit)"` | Razor drops the attribute when the value is `false`. | `@disabled(...)` |
| `Forbid()` / `NotFound()` | 403 / 404. Other people's applications are 404 so their ids are not confirmed. | `abort(403)` / `abort(404)` |
| `options.Events.OnRedirectToAccessDenied` in `Program.cs` | For AJAX and `/api` requests return 401/403 instead of redirecting to an HTML page. | `$request->expectsJson()` in the auth exception handler |
| `ApplicationStatusExtensions.Terminal.Contains(a.Status)` | An array `Contains` that EF translates to `IN (...)`. | `whereIn('status', [...])` / `whereNotIn` |
| `Services/ApplicationPageBuilder.cs` | Builds the page with one projection. `ManagerNotes` is never selected for this page. | A query + API Resource that omits a field |

## Phase 6: Review queue, review modal, history, manager notes

### Commands run in this phase

No new packages. The flow was smoke-tested with two manager sessions and one applicant session (three cookie jars): claim, a second manager trying to claim and complete (refused), notes, Return without a comment (422), Return, applicant fix and resubmit, claim, Approve. Then the applicant's page was checked to confirm the notes text never appears in it.

### Concepts

| This repo | What it is | Laravel equivalent |
|---|---|---|
| `Controllers/ReviewController.cs` `Claim` / `Release` | Plain POST forms that redirect back with a flash message. | `Route::post('/applications/{a}/claim')` + `back()->with(...)` |
| `Complete` (GET partial + POST) | The review modal. Outcome radio + comment. The success path reloads the page (`data-refresh="page"`). | A modal form posting to a controller |
| `ReviewFormViewModel : IValidatableObject` | Comment required only for Return and Deny. The domain enforces it again. | `'comment' => 'required_if:outcome,return,deny'` |
| `ViewComponents/StatusHistoryViewComponent.cs` | Loads and renders who / when / comment. Returns `Content("")` unless the viewer is a property manager. | A Blade component wrapped in `@role('manager')` |
| `ViewComponents/ManagerNotesViewComponent.cs` | The only place notes are read for display. It has the same role check. | Same |
| `UserClaimsPrincipal` inside a view component | The current user, as `User` is in a controller. | `auth()->user()` |
| `ExecuteUpdateAsync(s => s.SetProperty(a => a.ManagerNotes, ...))` | A single `UPDATE ... SET` without loading the entity. It also skips the concurrency token, so saving notes never conflicts with a review. | `Application::whereKey($id)->update([...])` |
| `<partial name="_StatusBadge" model="row.ToStatus" />` | A tiny partial reused in the header, the history, and the list. | `@include('partials.status-badge')` / `<x-status-badge>` |

## Phase 7: Application list, JSON grid, OpenAPI

### Commands run in this phase

The `Microsoft.AspNetCore.OpenApi` and `Swashbuckle.AspNetCore.SwaggerUI` packages were added in Phase 2 and wired up here:

```bash
dotnet add src/RentalApp.Web package Microsoft.AspNetCore.OpenApi --version 10.0.12
dotnet add src/RentalApp.Web package Swashbuckle.AspNetCore.SwaggerUI
```

Checking the endpoint and the document by hand:

```bash
curl -b cookies.txt "http://localhost:5155/api/applications?status=Submitted&sort=property&direction=asc&page=1&pageSize=10"
curl http://localhost:5155/openapi/v1.json     # the OpenAPI document (Development only)
open http://localhost:5155/swagger             # Swagger UI over that document
node --check src/RentalApp.Web/wwwroot/js/grid.js   # syntax check, no bundler in this project
```

Laravel: `php artisan route:list` + Scribe / L5-Swagger for the docs.

### Concepts

| This repo | What it is | Laravel equivalent |
|---|---|---|
| `Controllers/Api/ApplicationsApiController.cs` with `[ApiController]` + `[Route("api/applications")]` | **Attribute routing** (the route is on the class, not in the conventional pattern). `[ApiController]` turns on automatic 400s for bad input and puts the action in the OpenAPI document. | `routes/api.php` + an API controller |
| `[FromQuery] ApplicationListRequest request` | Binds `?status=&propertyId=&sort=&page=` into one object. | `$request->query()` / a FormRequest |
| `ActionResult<PagedResult<ApplicationListRow>>` + `Ok(...)` | A typed JSON response, camelCase by default. | `return response()->json(...)` / an API Resource collection |
| `PagedResult<T>(Rows, Total, Page, PageSize)` | One page plus the filtered total. | `->paginate()` result (`data`, `total`, ...) |
| `Services/ApplicationListService.cs` `Skip/Take` + `CountAsync` | Paging and counting in SQL (`OFFSET ... FETCH`). | `->paginate($perPage)` |
| `SortColumns` dictionary | A whitelist from `?sort=` to a column, so the query string can never sort by something arbitrary. | `in_array($sort, $allowed)` before `orderBy($sort)` |
| `.AddJsonOptions(... JsonStringEnumConverter ...)` + `ConfigureHttpJsonOptions(...)` | Write enums as names. The first is for MVC responses; the second is read by the OpenAPI generator. | An enum cast that serializes `->value` |
| `builder.Services.AddOpenApi()` + `app.MapOpenApi()` | Generates `/openapi/v1.json` from the `[ApiController]` actions. | L5-Swagger / Scribe |
| `[EndpointSummary]`, `[EndpointDescription]`, `[Description]` on request properties, `[ProducesResponseType<T>(200)]` | What the document says about the endpoint, parameters, and responses. Declaring any response type turns off the inferred 200, so it is declared too. | Scribe docblock annotations |
| `app.UseSwaggerUI(...)` | A browsable UI for the document at `/swagger`. | `/api/documentation` (L5-Swagger) |
| `ViewComponents/DataGridViewComponent.cs` + `Models/Shared/GridDefinition.cs` | A reusable grid. The page passes columns, filters, and the data URL; the component renders the shell. | A Blade component taking a config array |
| `wwwroot/js/grid.js` | Fetches each page, renders rows with `textContent` (never `innerHTML`, so user data cannot inject HTML), and keeps filters / sort / page in the URL. | Vanilla JS / Alpine on a JSON endpoint |
| `@section Scripts { <script src="~/js/grid.js"> }` | Adds a script only to this page. | `@push('scripts')` |
| `Data/UtcDateTimeConverter.cs` + `ConfigureConventions` in `AppDbContext` | Every `DateTime` read from SQL Server is marked UTC, so JSON gets a `Z` and `ToLocalTime()` works. Applies model-wide in one line. | `'timezone' => 'UTC'` + datetime casts |
