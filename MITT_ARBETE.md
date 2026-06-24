# Mitt arbete - Inlamning kurs 1

## 1. Beskrivning av applikationen

Jag har byggt en fullstack webbapplikation med:
- Backend: ASP.NET Core (.NET 8) REST API
- Frontend: React + TypeScript
- Databaslager: Entity Framework Core

Applikationen ar en enkel publiceringsplattform dar anvandare kan registrera konto, logga in, skapa inlagg och kommentera inlagg. Den losar behovet av ett mini-forum/community med autentisering och behorighetshantering.

Huvudresursen ar `Post` och den har fler an tre attribut utover ID, till exempel:
- `Title`
- `Content`
- `AuthorId`
- `CreatedAt`
- `UpdatedAt`
- `ViewCount`

Applikationen innehaller aven fler resurser:
- `User`
- `Comment`
- `RefreshToken`
- `AuditLog`

## 2. Implementerad testning

### Backend-testning (xUnit)
I backend finns enhetstester for service-lagret:
- `AuthServiceTests`
- `PostServiceTests`
- `CommentServiceTests`

Testerna verifierar bland annat:
- registrering/inloggning och token-floden
- refresh token-rotation
- paginering och sokning for inlagg
- behorighetskontroller (agare/admin kontra obehorig)
- skapande/borttagning av kommentarer

Aktuell status lokalt:
- 14 backend-tester passerar.

### Frontend-testning (Vitest)
I frontend finns tester for valideringslogik och token-hantering:
- tokenStore (spara/hamta/rensa token)
- validering av anvandarnamn
- validering av losenordsstyrka
- password strength-berakning

Aktuell status lokalt:
- 17 frontend-tester passerar.

## 3. Testtackning och kvalitet

Testningen tacker centrala delar av applikationens affarslogik:
- autentisering och sessionsflode
- post- och kommentarslogik
- grundlaggande klientvalidering

Vad som ar bra:
- tydlig separation mellan service-lager och controller-lager gor logik testbar
- in-memory databas i tester ger snabb och repeterbar testkorning

Vad som saknas for hogre testtackning:
- fler integrationstester pa API-niva (controller + middleware + db tillsammans)
- end-to-end tester mellan frontend och backend
- negativa tester for fler kantfall (felaktiga payloads, timeout, externa beroenden)

## 4. Testbarhet i applikationen

Applikationen ar relativt testbar, men inte 100%.

Det som redan ar testbart:
- stor del av doman- och service-logiken
- frontendens rena valideringsfunktioner

Det som ar svare att testa fullt ut idag:
- vissa delar av infrastrukturfloden (t.ex. extern e-postleverans)
- driftmiljo-specifika saker (container runtime, deploymentmiljo)

For att narma sig full testbarhet skulle jag:
- oka anvandningen av mockning/stubbning for externa beroenden
- lagga till integrationstester for API-endpoints
- lagga till e2e-testning i pipeline (exempelvis Playwright)

## 5. CI/CD-pipeline: steg, gates och logik

Pipeline ligger i GitHub Actions (`.github/workflows/ci.yml`) och innehaller 6 jobb:

1. **Backend**  
   - NuGet-sakerhetsskanning (High/Critical blockerar)  
   - build  
   - enhetstester

2. **Frontend**  
   - npm audit (High/Critical blockerar)  
   - type-check  
   - lint  
   - tester  
   - produktionsbuild

3. **CodeQL SAST**  
   - statisk analys for C# och TypeScript

4. **Docker + Trivy**  
   - bygger backend- och frontend-image  
   - Trivy-skanning (High/Critical blockerar)

5. **Smoke test**  
   - startar verklig backend  
   - testar `/health` och krav pa HTTP 200

6. **Deploy**  
   - kor endast pa `main` vid push  
   - krav att tidigare jobb/gates ar godkanda

Detta ger en tydlig gate-logik: osakra eller trasiga andringar stoppas innan deploy.

## 6. Svarigheter och identifierade luckor

Under arbetet identifierades foljande:
- ett testfel i backend (saknad `using Xunit`) som ar atgardat
- en instabil testforvantning i refresh-token-test som ar justerad till mer robust verifiering
- frontend-beroenden uppdaterades (vite/vitest) sa att `npm audit` nu ar gron
- ImageSharp 4.x kravde betald licens, darfor anvands 3.1.12
- SQLite-sarbarheten GHSA-2m69-gcr7-jv3q atgardades genom explicit pin av `SQLitePCLRaw.lib.e_sqlite3` 3.50.3
- ESLint-konfiguration saknades i frontend och lades till for att lint-steget i CI ska fungera

Det betyder att testflodet fungerar lokalt och att sakerhetsgates i pipeline ar strikta utan tillfalliga undantag.

## 7. Reflektion

I projektet har jag fatt praktisk erfarenhet av hur utveckling, testning och sakerhet kan knytas ihop i en sammanhangen DevSecOps-process. Jag har lart mig att det inte racker att applikationen "fungerar" lokalt - den maste ocksa vara testbar, overvakad och skyddad genom automatiserade gates i CI/CD.

Det jag framfor allt tar med mig:
- hur man bygger en pipeline med flera kvalitets- och sakerhetssteg
- hur testbar kodstruktur underlattar snabbare utveckling
- hur beroendesakerhet och kontinuerlig uppdatering ar avgorande i praktiken

Det jag vill lara mig mer om:
- avancerad e2e-testning och teststrategi for distribuerade system
- secrets management och hardare produktionssakerhet
- mer avancerad deployment- och rollback-strategi i molnmiljo
