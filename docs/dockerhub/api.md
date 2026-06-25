# LittleHelpers API

Backend API for [LittleHelpers](https://github.com/ferenyl/littlehelpers) – a family chore management app where parents create chores, assign them to children, and track monthly allowance progress.

## Quick start

```bash
docker run -d \
  -p -p 80:80 \
  -e Jwt__Key="your-secret-key-min-32-characters-long" \
  -e Jwt__Issuer="littlehelpers" \
  -e Jwt__Audience="littlehelpers" \
  -e Jwt__AccessTokenLifetimeHours="168" \
  -e Jwt__RenewTokenLifetimeHours="336" \
  -e SeedAdminPassword="YourAdminPassword123!" \
  -e MonthlyCycle__BreakpointDay="1" \
  -e FirebaseNotifications__Active="false" \
  -e FirebaseNotifications__ProjectId="" \
  -e FirebaseNotifications__PrivateKeyId="" \
  -e FirebaseNotifications__PrivateKey="" \
  -e FirebaseNotifications__ClientEmail="" \
  -e FirebaseNotifications__ClientId="" \
  -e FirebaseNotifications__WebAppUrl="" \
  -e FirebaseNotifications__WebApiKey="" \
  -e FirebaseNotifications__WebAuthDomain="" \
  -e FirebaseNotifications__WebStorageBucket="" \
  -e FirebaseNotifications__WebMessagingSenderId="" \
  -e FirebaseNotifications__WebAppId="" \
  -e FirebaseNotifications__WebVapidKey="" \
  -e ConnectionStrings__littlehelpers="Host=db;Port=5432;Username=littlehelpers;Password=secret;Database=littlehelpers" \
  ferenyl/littlehelpers.api:latest
```

A running PostgreSQL instance is required. See [docker-compose](#docker-compose) below for a full stack example.

## Environment variables

| Variable | Required | Description |
|---|---|---|
| `ConnectionStrings__littlehelpers` | ✅ | PostgreSQL connection string |
| `Jwt__Key` | ✅ | JWT signing key, minimum 32 characters |
| `Jwt__Issuer` | ✅ | JWT issuer claim (e.g. `littlehelpers`) |
| `Jwt__Audience` | ✅ | JWT audience claim (e.g. `littlehelpers`) |
| `Jwt__AccessTokenLifetimeHours` | ❌ | Access token lifetime in hours (default: `168`) |
| `Jwt__RenewTokenLifetimeHours` | ❌ | Renewed token lifetime in hours (default: `336`) |
| `SeedAdminPassword` | ✅ | Password for the auto-created `admin` account |
| `MonthlyCycle__BreakpointDay` | ❌ | Day in month when a new allowance cycle starts (`1-31`, default: `1`; falls back to last day when needed) |
| `FirebaseNotifications__Active` | ❌ | Enable/disable Firebase notifications (`true`/`false`, default: `false`) |
| `FirebaseNotifications__ProjectId` | ❌ | Firebase project id (required if notifications are active) |
| `FirebaseNotifications__PrivateKeyId` | ❌ | Firebase service account private key id |
| `FirebaseNotifications__PrivateKey` | ❌ | Firebase service account private key, use `\n` for new lines |
| `FirebaseNotifications__ClientEmail` | ❌ | Firebase service account client email |
| `FirebaseNotifications__ClientId` | ❌ | Firebase service account client id |
| `FirebaseNotifications__WebAppUrl` | ❌ | Public web app URL used as notification click target, e.g. `https://littlehelpers.example.com` |
| `FirebaseNotifications__WebApiKey` | ❌ | Firebase Web app API key (required for browser push notifications) |
| `FirebaseNotifications__WebAuthDomain` | ❌ | Firebase Web app auth domain |
| `FirebaseNotifications__WebStorageBucket` | ❌ | Firebase Web app storage bucket |
| `FirebaseNotifications__WebMessagingSenderId` | ❌ | Firebase Web app messaging sender id |
| `FirebaseNotifications__WebAppId` | ❌ | Firebase Web app app id |
| `FirebaseNotifications__WebVapidKey` | ❌ | Firebase Web Push certificate public key (VAPID) |

> Generate a strong JWT key: `openssl rand -base64 48`

## Browser push notifications

To send push notifications to the web client, configure both the Firebase service account fields and the Firebase Web app fields above, then serve the web frontend over HTTPS. The API exposes the public web config at `/notifications/web-config` and subscribes logged-in browsers to the correct Firebase topic.

## Default admin account

On first startup the API seeds an admin account:

| Field | Value |
|---|---|
| Username | `admin` |
| Password | Value of `SeedAdminPassword` |
| Role | `Parent` |

## Ports

| Port | Protocol | Description |
|---|---|---|
| `80` | HTTP | API |

## Database migrations

EF Core migrations are applied automatically at startup. No manual steps required.

## Docker Compose

For a full stack setup (API + web frontend + PostgreSQL), see the [compose files](https://github.com/ferenyl/littlehelpers/tree/main/docs/compose) in the repository.

```yaml
services:
  db:
    image: postgres:17-alpine
    environment:
      POSTGRES_DB: littlehelpers
      POSTGRES_USER: littlehelpers
      POSTGRES_PASSWORD: secret

  api:
    image: ferenyl/littlehelpers.api:latest
    ports:
      - "-p 80"
    environment:
      ConnectionStrings__littlehelpers: "Host=db;Port=5432;Username=littlehelpers;Password=secret;Database=littlehelpers"
      Jwt__Key: "your-secret-key-min-32-characters-long"
      Jwt__Issuer: "littlehelpers"
      Jwt__Audience: "littlehelpers"
      Jwt__AccessTokenLifetimeHours: "168"
      Jwt__RenewTokenLifetimeHours: "336"
      SeedAdminPassword: "YourAdminPassword123!"
      MonthlyCycle__BreakpointDay: "1"
      FirebaseNotifications__Active: "false"
      FirebaseNotifications__ProjectId: ""
      FirebaseNotifications__PrivateKeyId: ""
      FirebaseNotifications__PrivateKey: ""
      FirebaseNotifications__ClientEmail: ""
      FirebaseNotifications__ClientId: ""
      FirebaseNotifications__WebAppUrl: ""
      FirebaseNotifications__WebApiKey: ""
      FirebaseNotifications__WebAuthDomain: ""
      FirebaseNotifications__WebStorageBucket: ""
      FirebaseNotifications__WebMessagingSenderId: ""
      FirebaseNotifications__WebAppId: ""
      FirebaseNotifications__WebVapidKey: ""
    depends_on:
      - db
```

## Tech stack

- ASP.NET Core (.NET 10)
- Entity Framework Core + PostgreSQL (Npgsql)
- JWT Bearer authentication
- HATEOAS via `LinkWriter<T>`
- OpenTelemetry

## Source

[github.com/ferenyl/littlehelpers](https://github.com/ferenyl/littlehelpers)
