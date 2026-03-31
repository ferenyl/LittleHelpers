---
applyTo: "**/*.cs,**/*.csproj"
---

# .NET C# – Instruktioner

## Grundregler

- Målramverk: **.NET 10**
- Aktivera `<Nullable>enable</Nullable>` och `<ImplicitUsings>enable</ImplicitUsings>` i alla projekt.
- Använd `record`-typer för oföränderliga datamodeller (DTOs, värde-objekt).
- Föredra `primary constructors` för enkel dependency injection.
- Inga varningstysta kommentarer (`#pragma warning disable`) utan godkänd motivering.

## API-projekt (LittleHelpers.ApiService)

- Använd **Controllers**, inte minimal API.
- dokumentera tydligt både för api och för framtida utvecklare.
- Skydda alla endpoints med JWT-autentisering utom `/auth/login`.
- Implementera **HATEOAS** via en generisk `LinkWriter<T>`-service som lägger till hypermedia-länkar baserat på användarroll.
- Returnera alltid `ProblemDetails` vid fel (använd `TypedResults` eller `Problem()`).
- Validera indata med DataAnnotations eller FluentValidation.

## Auth

- JWT-token genereras av API:et vid lyckad inloggning.
- Användare lagras i databasen med: `username`, `password` (bcrypt-hash), `user_level` (parent/child).
- Använd `[Authorize(Roles = "Parent")]` / `[Authorize(Roles = "Child")]` för rollskydd.

## Databas

- Använd **Npgsql** (via `Aspire.Npgsql`-integration).
- Separera sysslor-logg i en egen tabell med kolumner: `chore_id`, `chore_name`, `child_id`, `performed_by`, `points`, `timestamp`.
- Använd migrations för schemaändringar.

## Kodstruktur

```
LittleHelpers.ApiService/
  Controllers/       # API-controllers
  Models/            # Domänmodeller och DTOs
  Services/          # Affärslogik, inklusive LinkWriter<T>
  Data/              # DbContext och migrations
```

## Testprojekt (LittleHelpers.Tests)

- Skriv enhetstester för services och integrationstest för controllers.
- Mocka beroenden med Moq eller NSubstitute.
- Namnge tester: `MethodName_Scenario_ExpectedResult`.
