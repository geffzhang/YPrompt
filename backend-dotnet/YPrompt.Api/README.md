# YPrompt Backend - ASP.NET Core 10

This is the C# ASP.NET Core 10 implementation of the YPrompt backend API, migrated from the original Python Sanic implementation.

## Features

- **Dual Authentication**: Linux.do OAuth 2.0 + Local username/password authentication
- **Dual Database Support**: SQLite (default) + MySQL
- **JWT Authentication**: Token-based authentication with 7-day expiration
- **RESTful API**: Full API compatibility with the original Python backend
- **Swagger/OpenAPI**: API documentation available at `/docs`

## Project Structure

```
YPrompt.Api/
├── Controllers/           # API Controllers
│   ├── AuthController.cs        # Authentication endpoints
│   ├── PromptsController.cs     # Prompt CRUD operations
│   ├── VersionsController.cs    # Version management
│   ├── TagsController.cs        # Tag management
│   └── PromptRulesController.cs # User prompt rules
├── Data/                  # Database Context
│   └── YPromptDbContext.cs
├── Models/
│   ├── Entities/          # Entity classes (EF Core)
│   │   ├── User.cs
│   │   ├── Prompt.cs
│   │   ├── PromptVersion.cs
│   │   ├── PromptTag.cs
│   │   ├── PromptShare.cs
│   │   ├── UserPromptRules.cs
│   │   └── UserSession.cs
│   └── DTOs/              # Data Transfer Objects
│       ├── ApiResponse.cs
│       ├── AuthDTOs.cs
│       ├── PromptDTOs.cs
│       ├── VersionDTOs.cs
│       ├── TagDTOs.cs
│       └── PromptRulesDTOs.cs
├── Services/              # Business Logic
│   ├── AuthService.cs
│   ├── PromptService.cs
│   ├── VersionService.cs
│   ├── TagService.cs
│   ├── PromptRulesService.cs
│   └── DatabaseInitializer.cs
├── Utils/                 # Utility Classes
│   ├── JwtUtil.cs
│   ├── PasswordUtil.cs
│   └── LinuxDoOAuth.cs
├── Program.cs             # Application entry point
├── appsettings.json       # Configuration
└── YPrompt.Api.csproj     # Project file
```

## Quick Start

### Prerequisites

- .NET 10 SDK

### Run the API

```bash
cd backend-dotnet/YPrompt.Api
dotnet run
```

The API will start at `http://localhost:8888` by default.

### Configuration

Configuration can be set via `appsettings.json` or environment variables:

| Setting | Environment Variable | Default | Description |
|---------|---------------------|---------|-------------|
| `Jwt:SecretKey` | `SECRET_KEY` | - | JWT signing key (required) |
| `Database:Type` | `DB_TYPE` | `sqlite` | Database type: `sqlite` or `mysql` |
| `ConnectionStrings:Sqlite` | `SQLITE_DB_PATH` | `../data/yprompt.db` | SQLite database path |
| `LinuxDo:ClientId` | `LINUX_DO_CLIENT_ID` | - | Linux.do OAuth client ID |
| `LinuxDo:ClientSecret` | `LINUX_DO_CLIENT_SECRET` | - | Linux.do OAuth client secret |
| `LinuxDo:RedirectUri` | `LINUX_DO_REDIRECT_URI` | - | OAuth redirect URI |
| `DefaultAdmin:Username` | `ADMIN_USERNAME` | `admin` | Default admin username |
| `DefaultAdmin:Password` | `ADMIN_PASSWORD` | `admin123` | Default admin password |

## API Endpoints

### Authentication (`/api/auth`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/linux-do/login` | Linux.do OAuth login |
| POST | `/api/auth/local/login` | Local username/password login |
| POST | `/api/auth/local/register` | Register new local user |
| POST | `/api/auth/refresh` | Refresh JWT token |
| GET | `/api/auth/userinfo` | Get current user info |
| POST | `/api/auth/logout` | User logout |
| GET | `/api/auth/config` | Get auth configuration |

### Prompts (`/api/prompts`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/prompts` | Save/create prompt |
| GET | `/api/prompts` | List prompts |
| GET | `/api/prompts/{id}` | Get prompt detail |
| PUT | `/api/prompts/{id}` | Update prompt |
| DELETE | `/api/prompts/{id}` | Delete prompt |
| POST | `/api/prompts/{id}/favorite` | Toggle favorite |
| POST | `/api/prompts/{id}/use` | Record usage |

### Versions (`/api/versions`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/versions/{promptId}` | Create version |
| GET | `/api/versions/{promptId}/versions` | List versions |
| GET | `/api/versions/{promptId}/versions/{versionId}` | Get version detail |
| GET | `/api/versions/{promptId}/versions/compare` | Compare versions |
| POST | `/api/versions/{promptId}/versions/{versionId}/rollback` | Rollback to version |
| PUT | `/api/versions/{promptId}/versions/{versionId}/tag` | Update version tag |
| DELETE | `/api/versions/{promptId}/versions/{versionId}` | Delete version |

### Tags (`/api/tags`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/tags` | List user tags |
| POST | `/api/tags` | Create tag |
| DELETE | `/api/tags/{id}` | Delete tag |
| GET | `/api/tags/popular` | Get popular tags |

### Prompt Rules (`/api/prompt-rules`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/prompt-rules` | Get user rules |
| POST | `/api/prompt-rules` | Save user rules |
| DELETE | `/api/prompt-rules` | Reset to defaults |

## API Response Format

All responses follow this format:

```json
{
  "code": 200,
  "message": "Success",
  "data": { ... }
}
```

Error responses:

```json
{
  "code": 400,
  "message": "Error description"
}
```

## Development

### Build

```bash
dotnet build
```

### Run with hot reload

```bash
dotnet watch run
```

### Environment Variables

For production, set these environment variables:

```bash
export SECRET_KEY="your-secure-secret-key-at-least-32-characters"
export DB_TYPE="sqlite"
export ADMIN_USERNAME="admin"
export ADMIN_PASSWORD="your-secure-password"
```

## License

MIT License
