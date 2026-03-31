---
applyTo: "**"
---

# Aspire – Instruktioner

## Generella regler

- Kör alltid `aspire run` och kontrollera resursstatus innan du börjar göra ändringar.
- Ändringar i `AppHost/Program.cs` (apphost) kräver omstart av applikationen.
- Gör förändringar inkrementellt och validera med `aspire run` efter varje steg.
- Undvik persistenta containrar tidigt i utvecklingen för att undvika state-problem vid omstart.
- Aspire workload är obsolet – installera eller använd det aldrig.

## Lägg till integrationer

1. Använd verktyget **list integrations** för att hämta aktuell version av tillgänglig integration.
2. Välj version som matchar `Aspire.AppHost.Sdk`-versionen i `AppHost/AppHost.csproj`.
3. Använd sedan **get integration docs** för att läsa dokumentationen innan du implementerar.

## Felsökning

Aspire samlar in rik telemetri. Använd dessa verktyg i ordning innan du ändrar kod:

1. **list structured logs** – strukturerade loggar per resurs
2. **list console logs** – konsoloutput från resurser och kommandon
3. **list traces** – distribuerade traces
4. **list trace structured logs** – loggar kopplade till ett specifikt trace

## Officiell dokumentation

- https://aspire.dev
- https://learn.microsoft.com/dotnet/aspire
- https://nuget.org (för integrationspaket)
