# LittleHelpers Web

Frontend for [LittleHelpers](https://github.com/ferenyl/littlehelpers) – a family chore management app. Served by nginx with a built-in reverse proxy to the API.

## Quick start

```bash
docker run -d \
  -p 80:80 \
  -e API_URL="http://api" \
  ferenyl/littlehelpers.web:latest
```

The `API_URL` is injected into the nginx config at container startup via `envsubst`. All requests to `/api/*` are proxied to the API service.

## Environment variables

| Variable | Required | Default | Description |
|---|---|---|---|
| `API_URL` | ✅ | – | Base URL of the API service, e.g. `http://api` |

## Browser push notifications

Web push notifications are configured through the API, not the web container. To enable them:

1. Configure the API container's `FirebaseNotifications__*` service account fields.
2. Configure the API container's `FirebaseNotifications__Web*` fields and VAPID key.
3. Serve the web app over **HTTPS** in production.

The web client fetches the public Firebase config from `/api/notifications/web-config`, registers `firebase-messaging-sw.js`, and subscribes the logged-in browser after permission is granted.

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

This means `/api/auth/login` in the browser → `http://api/auth/login` on the server side. The API is never exposed directly to the internet.

## Languages

The UI is available in **English** (default) and **Swedish**. The language is selected automatically based on the browser's locale setting (`navigator.language`).

## Docker Compose

For a full stack setup (API + web + PostgreSQL), see the [compose files](https://github.com/ferenyl/littlehelpers/tree/main/docs/compose) in the repository.

```yaml
services:
  web:
    image: ferenyl/littlehelpers.web:latest
    ports:
      - "80"
    environment:
      API_URL: "http://api"
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
