# Playbook

A personal knowledge-base app for storing reusable how-to "playbooks" (activities) across any domain — technical workflows, fitness routines, hobbies, life admin. Each activity has rich-text description and notes, do's and don'ts, tags, categories, attachments, and favorite/archive/view tracking.

---

## Tech Stack

### Backend
| Concern | Technology |
|---|---|
| Runtime | .NET 10 |
| API | GraphQL via [HotChocolate](https://chillicream.com/docs/hotchocolate) 14.3.1 |
| Database | MongoDB 7 (Atlas in prod, local container in dev) |
| Auth | JWT (HS256, 15-min access) + HttpOnly refresh cookie (7-day, rotated) |
| File storage | Azure Blob Storage (Azurite locally) |
| Logging | Serilog → Application Insights |
| Validation | FluentValidation |

### Frontend
| Concern | Technology |
|---|---|
| Framework | Angular 19 (standalone components, signals) |
| Styling | Tailwind CSS 3 (dark mode via `class`) |
| GraphQL client | Apollo Angular 13 |
| Rich text | TipTap 3 / ProseMirror (JSON storage) |
| Icons | Lucide Angular |
| Fonts | Inter, JetBrains Mono |

---

## Project Structure

```
think-ahead/
├── Playbook.slnx                      # solution file
├── global.json                        # pins .NET SDK 10.0.x
├── docker-compose.dev.yml             # local Mongo + Azurite
├── apps/
│   └── playbook-web/                  # Angular 19 SPA
│       └── src/app/
│           ├── core/                  # auth, GraphQL, theme
│           ├── shared/                # editor, pipes, UI
│           ├── features/              # dashboard, activities, categories, tags, auth
│           └── layouts/               # shell (sidebar + main)
└── src/
    ├── Playbook.Domain/               # entities, value objects — no external deps
    ├── Playbook.Application/          # use cases, ports, validators
    ├── Playbook.Infrastructure/       # Mongo, Blob, JWT, password hashing
    └── Playbook.Api/                  # HotChocolate, REST /uploads, DI root
tests/
    ├── Playbook.Domain.Tests/
    ├── Playbook.Application.Tests/
    └── Playbook.Api.IntegrationTests/ # Testcontainers (real Mongo)
```

Dependency flow: `Api` → `Infrastructure` → `Application` → `Domain`. Domain references nothing outside .NET BCL.

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (`global.json` pins 10.0.x)
- [Node.js 20+](https://nodejs.org/) and npm
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for local MongoDB + Azurite)
- [Angular CLI](https://angular.dev/tools/cli): `npm install -g @angular/cli`

---

## Local Development Setup

### 1. Start infrastructure

```bash
docker compose -f docker-compose.dev.yml up -d
```

This starts:
- **MongoDB 7** on `localhost:27017`
- **Azurite** (Azure Blob emulator) on ports `10000–10002`

### 2. Configure API secrets

The API uses [.NET User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) — never committed to source control.

```bash
cd src/Playbook.Api
dotnet user-secrets set "Jwt:SigningKey" "your-dev-signing-key-min-32-chars"
dotnet user-secrets set "Mongo:ConnectionString" "mongodb://localhost:27017"
dotnet user-secrets set "Mongo:Database" "playbook-dev"
dotnet user-secrets set "Blob:ConnectionString" "UseDevelopmentStorage=true"
```

### 3. Run the API

```bash
dotnet run --project src/Playbook.Api
```

API is available at:
- GraphQL playground: `http://localhost:5136/graphql`
- Health (live): `http://localhost:5136/health/live`
- Health (ready): `http://localhost:5136/health/ready`
- File upload: `POST http://localhost:5136/api/uploads`

### 4. Run the frontend

```bash
cd apps/playbook-web
npm install
npm start
```

The Angular app starts at `http://localhost:4200`. The `proxy.conf.json` forwards `/graphql` and `/api` to the .NET API at port 5136, so no CORS issues in dev.

---

## Key Features

- **Activities** — title, rich-text description and notes (TipTap/ProseMirror JSON), do's/don'ts lists, tags, category, attachments
- **Categories** — color-coded (Indigo, Emerald, Amber, Sky, Violet, Rose, Stone); filter activities by category
- **Tags** — free-form strings on activities; tag cloud in sidebar; click to filter
- **Favorites & Archive** — toggle per activity; dedicated sidebar links
- **View tracking** — monotonic view counter + `lastViewedAt` per activity; "Recently viewed" on dashboard
- **File attachments** — upload via `POST /api/uploads` (multipart, ≤ 25 MB); metadata stored in activity document; download via short-lived SAS URL
- **Dark mode** — toggled by `ThemeService`, persisted in `localStorage`; `<html class="dark">` strategy
- **Cursor pagination** — Relay-style `(updatedAt, _id)` cursor on activity list with "Load more"
- **Search** — full-text search via MongoDB text index (title weighted 10×, description, notes); debounced in both sidebar and list page

---

## Architecture Notes

### Authentication
- Signup/login return a short-lived JWT access token (15 min, HS256) and set a `HttpOnly; Secure; SameSite=Strict` refresh cookie scoped to `/api/auth/refresh`
- Refresh tokens are stored as SHA-256 hashes; rotated on every use
- Presenting a revoked-but-unexpired refresh token revokes the entire token chain (theft signal)
- Angular `AuthService` stores the access token in a signal; the auth interceptor handles 401 → refresh → retry, queuing concurrent requests on one in-flight refresh

### GraphQL
- All queries and mutations are `@authorize` protected except `signup`, `login`, `refreshToken`
- Every repository call is scoped by `userId` extracted from the JWT `sub` claim
- Explicit `ActivityFilter` input type; no schema-wide `[UseFiltering]`
- `[UsePaging]` cursor pagination on `activities` query
- Category batch loading via `BuildCategoryMapAsync()` to avoid N+1 per-activity lookups

### Rich Text
- TipTap stores content as ProseMirror JSON (`editor.getJSON()`)
- Backend stores it as `BsonDocument` — no HTML, no sanitization needed
- Frontend serializes to `JSON.stringify()` before sending to the GraphQL `String` field and `JSON.parse()` when reading back into TipTap

### MongoDB
Collections: `users`, `activities`, `categories`.  
Key indexes on `activities`:
- `{ userId, isArchived, updatedAt }` — default list
- `{ userId, categoryId, isArchived, updatedAt }` — category filter
- `{ userId, tags, isArchived }` — multikey tag filter
- `{ userId, isFavorite, updatedAt }` — partial (favorites only)
- Text index on `title`, `descriptionDoc`, `notesDoc`

---

## Running Tests

```bash
# Unit tests
dotnet test tests/Playbook.Domain.Tests
dotnet test tests/Playbook.Application.Tests

# Integration tests (requires Docker — spins up a real Mongo container via Testcontainers)
dotnet test tests/Playbook.Api.IntegrationTests
```

---

## Production Deployment (Azure)

| Resource | Service |
|---|---|
| .NET API | Azure App Service Linux (B1+) |
| Angular SPA | Azure Static Web Apps (Free tier) |
| Database | MongoDB Atlas (M0 dev / M10 prod) |
| File storage | Azure Blob Storage (private container `pb-files`) |
| Secrets | Azure Key Vault |
| Monitoring | Application Insights |

- All secrets are stored in Key Vault and surfaced to App Service via Key Vault references — no secrets in `appsettings.json`
- Blob files are private; download URLs are user-delegation SAS tokens with 15-minute TTL generated per request
- `appsettings.Development.json` is gitignored; use .NET User Secrets locally and Key Vault references in production

---

## Environment Variables / Configuration Keys

| Key | Description |
|---|---|
| `Jwt:SigningKey` | HS256 signing secret (min 32 chars) |
| `Jwt:Issuer` | Token issuer claim (default: `playbook-api`) |
| `Jwt:Audience` | Token audience claim (default: `playbook-web`) |
| `Mongo:ConnectionString` | MongoDB connection string |
| `Mongo:Database` | Database name |
| `Blob:ConnectionString` | Azure Blob / Azurite connection string |
| `Blob:Container` | Blob container name (default: `pb-files`) |

Never commit actual values for these. Use `dotnet user-secrets` locally and Key Vault in production.
