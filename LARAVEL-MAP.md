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
