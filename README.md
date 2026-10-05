# BlogPlatform


BlogPlatform is an ASP.NET Core MVC application for publishing and discovering blog posts. It includes account management, categories and tags, reader interactions, and writer profile pages.


## Features


- Browse and search published posts by category or tag.
- Create, edit, publish, and manage posts as a writer.
- Add a category, select popular tags, or enter custom tags for each post.
- Like posts, comment and reply, and bookmark posts.
- View public writer profiles and update profile details and pictures.
- Receive notifications for likes and comments, with per-user notification preferences.
- Sign in and register using ASP.NET Core Identity with Reader, Writer, and Admin roles.


## Technology


- ASP.NET Core MVC and Razor Pages
- .NET 10
- ASP.NET Core Identity
- Entity Framework Core
- SQL Server (LocalDB in the default development configuration)
- Bootstrap and jQuery


## Requirements


- .NET 10 SDK
- SQL Server or SQL Server LocalDB
- Windows is required to use the default `(localdb)\mssqllocaldb` connection string. For other environments, configure a reachable SQL Server instance.


## Run locally


From the `BlogPlatform` project directory:


```powershell
dotnet restore
dotnet run --launch-profile https
```


The HTTPS profile listens at `https://localhost:7260` and also exposes `http://localhost:5079`. The HTTP profile can be started with:


```powershell
dotnet run --launch-profile http
```


The app creates the database when it starts and seeds the default roles, categories, and tags. The default connection string points to a LocalDB database named `BlogPlatformDb`.


## Configuration


The default connection string is in `appsettings.json`. Override it for your environment rather than committing environment-specific credentials.


For local development, the connection string can be stored with .NET User Secrets:


```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\mssqllocaldb;Database=BlogPlatformDb;Trusted_Connection=True;MultipleActiveResultSets=true"
```


Or set the configuration environment variable before starting the app:


```powershell
$env:ConnectionStrings__DefaultConnection = "YOUR_SQL_SERVER_CONNECTION_STRING"
dotnet run
```


Uploaded post and profile images are stored under `wwwroot/uploads`. ASP.NET Core Data Protection keys are persisted in the project’s `Keys` directory so authentication and antiforgery cookies remain valid across restarts.


## Project layout


```text
BlogPlatform/
├── Areas/Identity/     # Identity UI pages
├── Controllers/        # MVC controllers
├── Data/               # EF Core context and startup seeding
├── Models/             # Domain and Identity models
├── Services/           # File storage and notification services
├── ViewModels/         # Form and page view models
├── Views/              # Razor MVC views
└── wwwroot/            # Static assets and uploaded images
```


