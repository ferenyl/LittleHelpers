---
applyTo: "**"
---

# LittleHelpers – Projektöversikt

LittleHelpers är en familjeapp för att hantera sysslor som barn ska utföra. Föräldrar skapar sysslor, tilldelar dem till barn och sätter poäng baserat på svårighetsgrad. Barn markerar sysslor som utförda via frontend.

## Projektchecklista

**Alla funktioner och ändringar ska följa [`projectchecklist.md`](../../projectchecklist.md).** Läs igenom checklistan innan du påbörjar arbete för att förstå vad som ska byggas, prioriteringsordning och vilka krav som gäller.

## Teknikstack

| Lager        | Teknik                                        |
| ------------ | --------------------------------------------- |
| Orkestrering | .NET Aspire                                   |
| Backend      | ASP.NET Core (.NET 10), Controllers + HATEOAS |
| Frontend     | Angular                                       |
| Databas      | PostgreSQL                                    |
| Auth         | JWT                                           |

## Roller

- **Parent** – kan skapa/redigera användare, sysslor och se alla barn
- **Child** – kan markera tilldelade sysslor som utförda

## Viktiga principer

- API:et använder controllers, inte minimal API
- Auth skyddas via JWT på alla endpoints utom login
- HATEOAS implementeras via en generisk `LinkWriter`-service
- Frontend har routeguard och omdirigerar till login vid saknad auth
- Menyval exponeras via ett separat API-endpoint baserat på usernivå
- Sysslor loggas i separat tabell med: chore id, chore name, child id, utförare, poäng
- jcodemunch-mcp är tillgänglig använd den gör att kolla filer och kod istället för de inbyggda så långt det är möjligt. Den mcp servern är till för att söka i kod och inte få med onödiga tokens
