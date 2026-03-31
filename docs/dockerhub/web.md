# LittleHelpers Web

Frontend for [LittleHelpers](https://github.com/ferenyl/littlehelpers) – a family chore management app. Served by nginx with a built-in reverse proxy to the API.

## Quick start

```bash
docker run -d \
  -p 80:80 \
  -e API_URL="http://api:80" \
  ferenyl/littlehelpers.web:latest
```

The `API_URL` is injected into the nginx config at container startup via `envsubst`. All requests to `/api/*` are proxied to the API service.

## Environment variables

| Variable | Required | Default | Description |
|---|---|---|---|
| `API_URL` | ✅ | – | Base URL of the API service, e.g. `http://api:80` |

## Ports

| Port | Protocol | Description |
|---|---|---|
| `80` | HTTP | Web frontend |

## How the proxy works

The nginx config template uses `API_URL` to set the proxy target:

```
location /api/ {
    proxy_pass ${API_URL}/;
}
```

This means `/api/auth/login` in the browser → `http://api:80/auth/login` on the server side. The API is never exposed directly to the internet.

## Languages

The UI is available in **English** (default) and **Swedish**. The language is selected automatically based on the browser's locale setting (`navigator.language`).

## Docker Compose

For a full stack setup (API + web + PostgreSQL), see the [compose files](https://github.com/ferenyl/littlehelpers/tree/main/docs/compose) in the repository.

```yaml
services:
  web:
    image: ferenyl/littlehelpers.web:latest
    ports:
      - "80:80"
    environment:
      API_URL: "http://api:80"
    depends_on:
      - api
```

## Tech stack

- Angular (standalone components, signals, OnPush)
- Bootstrap 5 dark mode
- Transloco i18n (English + Swedish)
- nginx 1.27 (static files + API reverse proxy)

## Source

[github.com/ferenyl/littlehelpers](https://github.com/ferenyl/littlehelpers)
