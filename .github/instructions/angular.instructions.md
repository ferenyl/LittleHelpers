---
applyTo: "**/ClientApp/**,**/*.ts,**/*.html,**/*.scss"
---

# Angular – Instruktioner

## Grundregler

- Använd **standalone components** (ingen NgModule om det inte är absolut nödvändigt).
- Använd **signals** (`signal`, `computed`, `effect`) för reaktivt state.
- Använd **modern control flow**: `@if`, `@for`, `@switch` – inte `*ngIf` / `*ngFor`.
- Använd **typed reactive forms** (`FormGroup<T>`).
- Aktivera `OnPush` change detection på alla komponenter.
- Föredra `inject()` framför constructor-injection.

## Auth

- Implementera en **Route Guard** (`CanActivateFn`) som omdirigerar till `/login` om JWT saknas.
- Spara JWT i `localStorage` (eller `sessionStorage` vid kortare sessions).
- Lägg till en HTTP-interceptor som bifogar `Authorization: Bearer <token>` på alla anrop.
- Hämta tillgängliga menypunkter från API-endpoint baserat på användarroll.

## Vyer och roller

| Vy           | Usernivå        | Rutt              |
|--------------|-----------------|-------------------|
| Login        | –               | `/login`          |
| Add user     | parent          | `/users/new`      |
| Edit user    | parent          | `/users/:id/edit` |
| List items   | parent          | `/items`          |
| Add item     | parent          | `/items/new`      |
| Edit item    | parent          | `/items/:id/edit` |
| Childs       | parent          | `/children`       |
| Child detail | parent & child  | `/children/:id`   |

Child detail-vyn ska innehålla:
- Klickbara knappar för att markera sysslor som utförda.
- Linjediagram med aktuell månads poäng (med möjlighet att byta månad).
- Expanderbar historiklista som visar de fem senaste sysslorna som standard.

## Tjänster och HTTP

- Använd `HttpClient` med `inject(HttpClient)`.
- Skapa separata services per domän: `AuthService`, `ChoreService`, `UserService`.
- Hantera HATEOAS-länkar från API-svaret för att avgöra vilka åtgärder som är tillgängliga.

## Kodstruktur

```
src/
  app/
    core/          # Auth-guard, interceptors, AuthService
    features/      # En mapp per vy/feature
    shared/        # Delade komponenter, pipes, directives
```

## Stil och konventioner

- Använd **SCSS** för stilar.
- Komponentfiler: `feature-name.component.ts` / `.html` / `.scss`.
- Exportera inte komponenter om de inte används utanför sin feature-mapp.
