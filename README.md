# LittleHelpers

LittleHelpers is a family chore management app. Parents create chores, assign them to children, and award points based on difficulty. Children mark chores as completed through the frontend. Each child can be given a monthly allowance goal – once enough points are earned, the app calculates how much of the allowance has been earned that month.

**Key features:**
- Parent role: manage users, create/edit chores, view all children and their progress
- Child role: view and complete assigned chores
- Monthly allowance tracking with point goals and payout calculation
- Per-child score history with a monthly chart
- Parent bonus/deduction chores that affect a child's points directly
- Full audit log of every completed chore including who performed it

---

## Tech stack

| Layer | Technology |
|---|---|
| Orchestration | [.NET Aspire](https://aspire.dev) |
| Backend | ASP.NET Core (.NET 10) – Controllers, HATEOAS |
| Frontend | Angular (standalone components, signals, Bootstrap 5 dark mode) |
| Database | PostgreSQL 17 |
| Auth | JWT (Bearer tokens) |
| ORM | Entity Framework Core + Npgsql |
| Passwords | BCrypt |
| Observability | OpenTelemetry (traces, logs, metrics via Aspire Dashboard) |

### Project layout

```
LittleHelpers.sln
├── LittleHelpers.AppHost/        # Aspire orchestrator – defines all resources
├── LittleHelpers.ApiService/     # ASP.NET Core API
│   ├── Controllers/
│   ├── Data/                     # DbContext + EF migrations
│   ├── Models/                   # Entities and DTOs
│   └── Services/                 # Business logic, LinkWriter<T> (HATEOAS)
├── LittleHelpers.ServiceDefaults/# Shared Aspire service defaults (telemetry, health)
├── LittleHelpers.Tests/          # Unit and integration tests
└── LittleHelpers.Web/
    # Angular app (src/app/{core,features,shared})
```

---

## Running with Aspire (development)

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Aspire CLI](https://learn.microsoft.com/dotnet/aspire/fundamentals/aspire-cli) (`dotnet tool install -g aspire-cli` or `aspire update`)
- [Docker](https://docs.docker.com/get-docker/) (used by Aspire to run PostgreSQL)
- [Node.js](https://nodejs.org/) 22+

### First-time setup

The API requires a JWT signing key and a seed admin password. These must be set in `appsettings.Development.json` (gitignored). Create the file in `LittleHelpers.ApiService/`:

```json
{
  "Jwt": {
    "Key": "your-secret-key-min-32-characters-long",
    "AccessTokenLifetimeHours": 168,
    "RenewTokenLifetimeHours": 336
  },
  "SeedAdminPassword": "YourAdminPassword123!",
  "MonthlyCycle": {
    "BreakpointDay": 1
  }
}
```

> **Tip:** Generate a strong key with `openssl rand -base64 48`

### Start the application

```bash
aspire run
```

Aspire will start PostgreSQL in Docker, run the API, and serve the Angular frontend. The Aspire Dashboard (traces, logs, metrics) opens automatically.

If a previous instance is already running, the CLI will prompt to stop it first.

### Useful Aspire commands

```bash
# Start the application
aspire run

# List resources and their status
aspire resource list

# View logs for a specific resource
aspire logs apiservice

# Stop the application
aspire stop
```

> Changes to `LittleHelpers.AppHost/AppHost.cs` require restarting the application. Changes to the API or frontend hot-reload automatically.

---

## Docker

The project ships two Dockerfiles, both built from the **repository root** (they need access to multiple projects).

### API image (`Dockerfile.api`)

Multi-stage build: `sdk:10.0` for compilation → `aspnet:10.0` runtime (~95 MB). Runs as a non-root user.

```bash
# Build
docker build -f Dockerfile.api -t littlehelpers-api:latest .

# Tag for a registry
docker tag littlehelpers-api:latest ghcr.io/yourorg/littlehelpers-api:1.0.0

# Push
docker push ghcr.io/yourorg/littlehelpers-api:1.0.0

# Run standalone (requires a running PostgreSQL)
docker run -d \
  -p 80:80 \
  -e Jwt__Key="your-secret-key-min-32-characters" \
  -e Jwt__AccessTokenLifetimeHours="168" \
  -e Jwt__RenewTokenLifetimeHours="336" \
  -e SeedAdminPassword="YourAdminPassword123!" \
  -e MonthlyCycle__BreakpointDay="1" \
  -e ConnectionStrings__littlehelpers="Host=localhost;Port=5432;Username=littlehelpers;Password=secret;Database=littlehelpers" \
  littlehelpers-api:latest
```

### Frontend image (`Dockerfile.web`)

Multi-stage build: `node:22-alpine` for `ng build` → `nginx:1.27-alpine` serving static files (~21 MB). The nginx config is a template – `API_URL` is substituted at container startup via `envsubst`, so the proxy target can be changed without rebuilding. The same startup templating also supports runtime override of web app metadata (`name`, `short_name`, `description`, `lang`, and HTML `<title>`).

```bash
# Build
docker build -f Dockerfile.web -t littlehelpers-web:latest .

# Tag for a registry
docker tag littlehelpers-web:latest ghcr.io/yourorg/littlehelpers-web:1.0.0

# Push
docker push ghcr.io/yourorg/littlehelpers-web:1.0.0

# Run standalone (proxies /api/* to the API)
docker run -d \
  -p 80:80 \
  -e API_URL="http://apiservice" \
  -e WEBAPP_NAME="My Family App" \
  -e WEBAPP_SHORT_NAME="FamilyApp" \
  -e WEBAPP_DESCRIPTION="Household chores and rewards" \
  -e WEBAPP_LANG="en" \
  -e WEBAPP_TITLE="My Family App" \
  littlehelpers-web:latest
```

The `API_URL` environment variable must point to the API service. When running both containers together, use the service name from docker-compose (`http://apiservice`).

---

## Docker Compose

The `docs/compose/` directory contains a ready-to-use Compose file and an example environment file.

```
docs/compose/
├── docker-compose.yml   # Full stack: postgres, apiservice, webfrontend
└── .env.example         # Template – copy to .env and fill in values
```

### First-time setup

```bash
cd docs/compose
cp .env.example .env
```

Open `.env` and fill in all `CHANGE_ME` values:

| Variable | Description |
|---|---|
| `POSTGRES_PASSWORD` | PostgreSQL password |
| `JWT_KEY` | JWT signing key, minimum 32 characters |
| `JWT_ISSUER` | JWT issuer claim (default: `littlehelpers`) |
| `JWT_AUDIENCE` | JWT audience claim (default: `littlehelpers`) |
| `JWT_ACCESS_TOKEN_LIFETIME_HOURS` | Access token lifetime in hours (default: `168`) |
| `JWT_RENEW_TOKEN_LIFETIME_HOURS` | Renewed token lifetime in hours (default: `336`) |
| `SEED_ADMIN_PASSWORD` | Password for the initial admin account |
| `MONTHLY_CYCLE_BREAKPOINT_DAY` | Day in month when a new allowance cycle starts (`1-31`, default: `1`; falls back to last day when needed) |
| `WEB_PORT` | Host port the frontend listens on (default: `80`) |
| `WEBAPP_NAME` | PWA `name` in manifest (default: `LittleHelpers`) |
| `WEBAPP_SHORT_NAME` | PWA `short_name` in manifest (default: `LittleHelpers`) |
| `WEBAPP_DESCRIPTION` | PWA description in manifest |
| `WEBAPP_LANG` | Language used in manifest and `<html lang>` (default: `en`) |
| `WEBAPP_TITLE` | Browser tab title (`<title>`) (default: `LittleHelpers`) |
| `APISERVICE_IMAGE` | Override API image (optional, default: `littlehelpers-api:latest`) |
| `WEBFRONTEND_IMAGE` | Override frontend image (optional, default: `littlehelpers-web:latest`) |

> **Never commit `.env` to version control.** It is listed in `.gitignore`.

### Build and start

```bash
# Build images locally and start all services
docker compose up -d --build

# Or pull pre-built images (if APISERVICE_IMAGE / WEBFRONTEND_IMAGE are set)
docker compose up -d
```

The frontend will be available at `http://localhost` (or whatever `WEB_PORT` is set to). The API is not exposed to the host – it is only reachable from within the Docker network via the frontend nginx proxy.

### Common commands

```bash
# View logs
docker compose logs -f

# Logs for a single service
docker compose logs -f apiservice

# Stop all services (data volume is preserved)
docker compose down

# Stop and remove all data (destructive)
docker compose down -v

# Restart a single service after a rebuild
docker compose up -d --build apiservice
```

---

## Authentication

The API seeds a default admin account at startup if it does not already exist:

| Field | Value |
|---|---|
| Username | `admin` |
| Password | Value of `SeedAdminPassword` |
| Role | `Parent` |

Log in at `/login` in the frontend, or call `POST /api/auth/login` directly:

```bash
curl -X POST http://localhost/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"YourAdminPassword123!"}'
```

The response includes a JWT Bearer token. All endpoints except `/api/auth/login` and `/api/health` require the `Authorization: Bearer <token>` header.

To renew an active token, call:

```bash
curl -X POST http://localhost/api/auth/renew \
  -H "Authorization: Bearer <token>"
```

---

## Database migrations

EF Core migrations are applied automatically at startup. To run them manually or create new ones:

```bash
# Apply migrations
dotnet ef database update \
  --project LittleHelpers.ApiService \
  --connection "Host=localhost;Port=5432;Username=littlehelpers;Password=secret;Database=littlehelpers"

# Create a new migration
dotnet ef migrations add <MigrationName> \
  --project LittleHelpers.ApiService \
  --output-dir Data/Migrations
```

The design-time factory (`AppDbContextFactory`) reads the connection string from the `LITTLEHELPERS_CONNSTR` environment variable when running `dotnet ef` commands outside of Aspire.

---

## Running tests

```bash
dotnet test
```

All tests are in `LittleHelpers.Tests/`. The test suite uses an in-memory JWT key and does not require a running database or Docker.

---

## Releases (GitHub tag-driven)

Releases are created automatically by GitHub Actions when you push a version tag that points to a commit on `main`.

### Tag formats

| Tag example | Type | GitHub release | Docker tags |
|---|---|---|---|
| `v0.0.7` | Stable release | Release | `v0.0.7` + `latest` |
| `v0.0.7-pre1` | Pre-release | Pre-release | `v0.0.7-pre1` + `prerelease` |

### What happens automatically

1. CI tests run first (.NET + Angular build/tests).
2. Docker images are built and pushed (`ferenyl/littlehelpers.api` and `ferenyl/littlehelpers.web`).
3. A GitHub release is created from the tag.
4. Release notes are auto-generated from commits since the previous release and include GitHub compare links.

### Create a stable release

```bash
git checkout main
git pull --ff-only
git tag v0.0.7
git push origin v0.0.7
```

### Create a pre-release

```bash
git checkout main
git pull --ff-only
git tag v0.0.7-pre1
git push origin v0.0.7-pre1
```

> If a tag does not point to a commit contained in `main`, release/publish jobs are skipped.

---

## Translations (i18n)

The frontend uses [`@jsverse/transloco`](https://jsverse.github.io/transloco/) for internationalization. Supported languages are **English** (default) and **Swedish**.

### How it works

On startup, the app detects the browser language via `navigator.language`. If the language starts with `sv`, Swedish is used – otherwise English is the fallback.

Translation files are static JSON files served as regular assets:

```
LittleHelpers.Web/public/i18n/
├── en.json   # English (default)
└── sv.json   # Swedish
```

The files are fetched at runtime via `HttpClient`. There is no build step required when adding or editing translations.

### Key structure

Translations are organized by feature:

| Prefix | Covers |
|---|---|
| `common.*` | Shared labels: loading, save, cancel, edit, delete, points |
| `nav.*` | App name, logout button |
| `login.*` | Login form and error messages |
| `users.*` | User list, user form, role labels |
| `chores.*` | Chore list, chore form, difficulty levels |
| `children.*` | Children list view |
| `childDetail.*` | Child detail view, allowance section, chart |
| `months.1–12` | Month names (used in chart/history labels) |

### Adding a new language

1. Create `public/i18n/<code>.json` using `en.json` as a template.
2. Register the language code in `app.config.ts`:

```ts
// in provideTransloco({ ... })
availableLangs: ['en', 'sv', '<code>'],
```

3. Update `detectLanguage()` if you want the new language to be auto-detected:

```ts
function detectLanguage(): string {
  const lang = navigator.language.split('-')[0];
  return ['en', 'sv', '<code>'].includes(lang) ? lang : 'en';
}
```

### Using translations in components

In templates, use the `transloco` pipe:

```html
<p>{{ 'common.save' | transloco }}</p>

<!-- With interpolation parameters -->
<h2>{{ 'childDetail.allowance.title' | transloco: { month: translatedMonth(), year: year() } }}</h2>
```

In TypeScript (e.g. confirm dialogs), inject the service:

```ts
private transloco = inject(TranslocoService);

if (!confirm(this.transloco.translate('chores.confirmDelete'))) return;
```

---

## Security notes

- **JWT key**: stored in `appsettings.Development.json` locally (gitignored). In production, set via the `Jwt__Key` environment variable.
- **Seed password**: stored in `appsettings.Development.json` locally (gitignored). In production, set via `SeedAdminPassword`.
- **Database credentials**: never hardcoded. Always supplied via environment variables or Aspire connection strings.
- The `appsettings.Development.json` file is gitignored. Do not commit it.
