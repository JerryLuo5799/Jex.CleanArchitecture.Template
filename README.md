# Jex.CleanArchitecture.Template

Jex is a lightweight, opinionated Clean Architecture template for .NET 10. Designed to eliminate boilerplate and accelerate backend development with modern C# practices.

## 🏗️ Architecture Overview

```
src/
├── Jex.Domain/           # Entities, enums — zero infrastructure dependencies
├── Jex.Application/      # CQRS (MediatR), Facet DTOs, validation, interfaces
├── Jex.Infrastructure/   # FreeSQL (Code First), repositories, DI wiring
└── Jex.WebAPI/           # ASP.NET Core controllers, middleware, startup
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
| Application | [Facet 5.7](https://github.com/Tim-Maes/Facet) | Compile-time DTO generation (source generator) |
| Application | [FluentValidation 11](https://docs.fluentvalidation.net) | Input validation via MediatR pipeline |
| Infrastructure | [FreeSQL 3.5](https://freesql.net) | ORM with Code First auto-migration |
| Infrastructure | SQLite (default) | Swappable via `Database:Provider` config |
| WebAPI | ASP.NET Core 10 | Minimal REST API with OpenAPI |

## ✨ Facet — Compile-Time DTO Mapping

**Facet** is a C# source generator that eliminates DTO boilerplate. Declare what you want; Facet generates the type, constructor, LINQ projection, and reverse mapping — **all at compile time with zero runtime overhead**.

```csharp
// Declare: exclude PasswordHash from the public DTO
[Facet(typeof(User), exclude: [nameof(User.PasswordHash)])]
public partial record UserDto;

// Facet generates ToFacet<TSource, TFacet>() as a compile-time extension
var dto = user.ToFacet<User, UserDto>();
```

Facet replaces AutoMapper / Mapster with no reflection, no configuration, no runtime cost.

## ⚡ FreeSQL — Code First

FreeSQL maps entities using **data annotation attributes** directly on entity properties:

```csharp
// Domain/Entities/User.cs
[Table("users")]
public class User : BaseEntity
{
    [MaxLength(200)]
    [Required]
    public string Email { get; set; } = string.Empty;
    // ...
}
```

`UseAutoSyncStructure(true)` automatically creates/migrates tables on startup — no migration files needed.

## 📡 CQRS with MediatR

Every feature is a self-contained vertical slice:

```
Features/Users/
├── Commands/
│   ├── CreateUser/   CreateUserCommand, Handler, Validator
│   ├── UpdateUser/   UpdateUserCommand, Handler, Validator
│   └── DeleteUser/   DeleteUserCommand, Handler
└── Queries/
    ├── GetUserById/  GetUserByIdQuery, Handler, UserDto (Facet)
    └── GetUsers/     GetUsersQuery, Handler
```

A `ValidationBehavior<TRequest, TResponse>` MediatR pipeline behavior automatically runs FluentValidation before every handler.

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

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/users` | List users (paginated) |
| `GET` | `/api/users/{id}` | Get user by Id |
| `POST` | `/api/users` | Create user |
| `PUT` | `/api/users/{id}` | Update user |
| `DELETE` | `/api/users/{id}` | Delete user |

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
│   │   ├── BaseEntity.cs        # Id, CreatedAt, UpdatedAt
│   │   └── User.cs              # Example entity
│   └── Enums/
│       └── UserStatus.cs
│
├── Jex.Application/
│   ├── Common/
│   │   ├── Behaviors/
│   │   │   └── ValidationBehavior.cs   # MediatR pipeline validation
│   │   ├── Exceptions/
│   │   │   ├── NotFoundException.cs
│   │   │   └── ValidationException.cs
│   │   └── Interfaces/
│   │       ├── IRepository.cs
│   │       └── IUserRepository.cs
│   ├── Features/
│   │   └── Users/
│   │       ├── Commands/               # CreateUser, UpdateUser, DeleteUser
│   │       └── Queries/                # GetUserById (with UserDto via Facet), GetUsers
│   └── DependencyInjection.cs
│
├── Jex.Infrastructure/
│   ├── Persistence/
│   │   └── Repositories/
│   │       ├── Repository.cs           # Generic FreeSQL repository
│   │       └── UserRepository.cs
│   └── DependencyInjection.cs
│
└── Jex.WebAPI/
    ├── Controllers/
    │   └── UsersController.cs
    ├── Program.cs                      # DI wiring + exception middleware
    └── appsettings.json
```
