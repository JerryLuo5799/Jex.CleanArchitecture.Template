# Jex.CleanArchitecture.Template

Jex is a lightweight, opinionated Clean Architecture template for .NET 10. Designed to eliminate boilerplate and accelerate backend development with modern C# practices.

## 🏗️ Architecture Overview

```
src/
├── Jex.Domain/           # Entities, enums — zero infrastructure dependencies
├── Jex.Application/      # CQRS (MediatR), Sannr validation, DTOs, interfaces
├── Jex.Infrastructure/   # FreeSQL (Code First), Identity, repositories, DI wiring
└── Jex.WebAPI/           # ASP.NET Core controllers, middleware, JWT auth, startup
```

### Dependency Rule

```
WebAPI → Application ← Infrastructure
           ↓
         Domain
```

The Domain layer has **no** NuGet dependencies. Every other layer depends inward only.

## 🚀 Key Technologies

| Layer | Technology | Purpose |
|---|---|---|
| Domain | Pure C# | Entities and enums |
| Application | [MediatR 12](https://github.com/jbogard/MediatR) | CQRS — commands & queries |
| Application | [Sannr 1.4](https://github.com/Tim-Maes/Sannr) | Compile-time input validation (source generator) |
| Infrastructure | [FreeSQL 3.5](https://freesql.net) | ORM with Code First auto-migration |
| Infrastructure | ASP.NET Core Identity | User management and password hashing |
| Infrastructure | SQLite (default) | Swappable via `Database:Provider` config |
| WebAPI | ASP.NET Core 10 | Minimal REST API with OpenAPI |
| WebAPI | JWT Bearer | Stateless authentication |
| WebAPI | [NLog 6](https://nlog-project.org) | Structured request/SQL logging |

## ✅ Sannr — Compile-Time Input Validation

**Sannr** is a C# source generator that provides zero-reflection, compile-time validation. Validators are registered once at startup and executed automatically via a MediatR pipeline behavior before every handler.

```csharp
// Register rules once in DependencyInjection.cs
Sannr.AspNetCore.SannrValidatorRegistry.Register<CreateUserCommand>(cmd =>
{
    var result = new ValidationResult();

    if (string.IsNullOrWhiteSpace(cmd.Email))
        result.Add(nameof(cmd.Email), "Email is required.", Severity.Error);

    return Task.FromResult(result);
});
```

A `ValidationBehavior<TRequest, TResponse>` MediatR pipeline behavior runs all registered Sannr validators before every handler and throws `ValidationException` when any rule fails.

## 🔐 ASP.NET Core Identity & JWT

User management (password hashing, user creation, credential validation) is handled by **ASP.NET Core Identity** backed by FreeSQL custom stores (`FreeSqlUserStore`, `FreeSqlRoleStore`). All credential lifecycle concerns are encapsulated behind the `IIdentityService` interface in the Application layer.

Authentication uses **JWT Bearer** tokens. On successful login the `TokenService` issues a signed HS256 token. All user endpoints require a valid token via `[Authorize]`.

Configure the JWT secret in `appsettings.json` (override in production via environment variables or secrets):

```json
{
  "Jwt": {
    "Secret": "CHANGE-THIS-TO-A-STRONG-SECRET-KEY-AT-LEAST-32-CHARS",
    "Issuer": "Jex",
    "Audience": "Jex",
    "ExpiryMinutes": "60"
  }
}
```

> **⚠️ Important:** The application will refuse to start if the default placeholder secret is used unchanged.

## ⚡ FreeSQL — Code First

FreeSQL maps entities using **data annotation attributes** directly on entity properties:

```csharp
// Domain/Entities/User.cs
[Table("users")]
public class User : IdentityUser<long>, IAuditableEntity
{
    [MaxLength(100)]
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    [Required]
    public string LastName { get; set; } = string.Empty;

    public UserStatus Status { get; set; } = UserStatus.Active;
    // ...
}
```

`UseAutoSyncStructure(true)` automatically creates/migrates tables on startup — no migration files needed.

## 📡 CQRS with MediatR

Every feature is a self-contained vertical slice:

```
Features/
├── Auth/
│   └── Commands/
│       └── Login/    LoginCommand, Handler, Validator
└── Users/
    ├── Commands/
    │   ├── CreateUser/   CreateUserCommand, Handler, Validator
    │   ├── UpdateUser/   UpdateUserCommand, Handler, Validator
    │   └── DeleteUser/   DeleteUserCommand, Handler
    └── Queries/
        ├── GetUserById/  GetUserByIdQuery, Handler, UserDto
        └── GetUsers/     GetUsersQuery, Handler
```

A `ValidationBehavior<TRequest, TResponse>` MediatR pipeline behavior automatically runs Sannr validators before every handler.

## 🔧 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Install the Template

Install the template from NuGet:

```bash
dotnet new install Jex.CleanArchitecture.Template
```

### Create a New Project

```bash
dotnet new jex -n YourProjectName
```

This will scaffold a complete Clean Architecture solution under `YourProjectName/`.

### Run

```bash
cd YourProjectName/src/YourProjectName.WebAPI
dotnet run
```

The app will:
1. Create `jex.db` (SQLite, Code First — tables auto-created)
2. Expose the REST API at `http://localhost:5208`
3. Serve OpenAPI at `http://localhost:5208/openapi/v1.json`

### Uninstall the Template

```bash
dotnet new uninstall Jex.CleanArchitecture.Template
```

### API Endpoints

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/api/auth/login` | ❌ | Authenticate and receive JWT token |
| `GET` | `/api/users` | ✅ JWT | List users (paginated) |
| `GET` | `/api/users/{id}` | ✅ JWT | Get user by Id |
| `POST` | `/api/users` | ✅ JWT | Create user |
| `PUT` | `/api/users/{id}` | ✅ JWT | Update user |
| `DELETE` | `/api/users/{id}` | ✅ JWT | Delete user |

### Switch Database Provider

Change `appsettings.json` to use a different FreeSQL provider:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=jex;User=root;Password=pass;"
  },
  "Database": {
    "Provider": "MySql"
  }
}
```

Add the corresponding FreeSQL provider package (e.g., `FreeSql.Provider.MySql`).

## 📁 Project Structure

```
src/
├── Jex.Domain/
│   ├── Entities/
│   │   ├── ApplicationRole.cs   # ASP.NET Core Identity role entity
│   │   ├── BaseEntity.cs        # Id, CreatedAt, UpdatedAt (for non-Identity entities)
│   │   ├── IAuditableEntity.cs  # Audit field contract
│   │   ├── User.cs              # Identity user + domain fields
│   │   └── UserRole.cs          # User–role join entity
│   └── Enums/
│       └── UserStatus.cs
│
├── Jex.Application/
│   ├── Common/
│   │   ├── Behaviors/
│   │   │   └── ValidationBehavior.cs   # MediatR pipeline — runs Sannr validators
│   │   ├── Exceptions/
│   │   │   ├── NotFoundException.cs
│   │   │   └── ValidationException.cs
│   │   ├── IIdentityService.cs         # Abstraction over Identity operations
│   │   ├── IRepository.cs              # Generic repository interface
│   │   └── IUserRepository.cs
│   ├── Features/
│   │   ├── Auth/
│   │   │   └── Commands/
│   │   │       └── Login/              # LoginCommand, Handler, Validator
│   │   └── Users/
│   │       ├── Commands/               # CreateUser, UpdateUser, DeleteUser
│   │       └── Queries/                # GetUserById (with UserDto), GetUsers
│   └── DependencyInjection.cs
│
├── Jex.Infrastructure/
│   ├── Identity/
│   │   ├── FreeSqlRoleStore.cs         # IQueryableRoleStore backed by FreeSQL
│   │   ├── FreeSqlUserStore.cs         # IQueryableUserStore backed by FreeSQL
│   │   ├── IdentityService.cs          # IIdentityService implementation
│   │   └── TokenService.cs             # JWT token generation
│   ├── Persistence/
│   │   └── Repositories/
│   │       ├── Repository.cs           # Generic FreeSQL repository
│   │       └── UserRepository.cs
│   └── DependencyInjection.cs
│
└── Jex.WebAPI/
    ├── Controllers/
    │   ├── AuthController.cs           # POST /api/auth/login
    │   └── UsersController.cs          # CRUD /api/users (JWT-protected)
    ├── Middleware/
    │   └── ApiLoggingMiddleware.cs     # Structured request/response logging via NLog
    ├── Models/
    │   └── ApiResponse.cs              # Unified response envelope
    ├── Program.cs                      # DI wiring, JWT, exception handler
    ├── appsettings.json
    └── nlog.config
```
