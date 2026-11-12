# BeArtGallery

Prototipo di piattaforma digitale per una **galleria d'arte** (impresa culturale simulata), realizzato come Project Work del CdS *Informatica per le Aziende Digitali (L-31)* — traccia PW 22: *Digitalizzazione dei processi aziendali in un'impresa culturale*.

La piattaforma digitalizza cinque processi della galleria:

| Processo | Prima (strumenti frammentati) | Con BeArtGallery |
|---|---|---|
| Catalogo delle opere | Fogli di calcolo, schede cartacee | Catalogo digitale con artisti, opere, immagini, audioguide e QR code in sala |
| Prenotazione di visite ed eventi | Email, telefono | Prenotazione online con controllo dei posti e biglietto digitale (QR) |
| Accoglienza dei visitatori | Lista cartacea all'ingresso | Check-in tramite scansione del QR della prenotazione |
| Relazione con il pubblico | Email sparse | Richieste di informazioni sulle opere, preferiti, feedback dopo la visita |
| Monitoraggio | Conteggi manuali | Dashboard con indicatori (presenze, occupazione, interesse, soddisfazione) |

---

## Architettura

```
┌──────────────────────┐   HTTP (HTML)    ┌────────────────────────────────┐   EF Core / SQL   ┌──────────────────┐
│  Browser             │ ───────────────► │  ASP.NET Core 10 (C#)           │ ────────────────► │  SQL Server       │
│  Razor + CSS + JS    │                  │  • Controller MVC (pagine)      │                   │  (LocalDB)        │
│  fetch() / Chart.js  │ ◄─────────────── │  • Controller API REST (JSON)   │ ◄──────────────── │  database         │
└──────────────────────┘   HTTP (JSON)    │  • Servizi di dominio           │                   │  relazionale      │
                                          │  • ASP.NET Identity (ruoli)     │                   └──────────────────┘
                                          └────────────────────────────────┘
```

- **Front-end**: viste Razor (HTML), CSS personalizzato, JavaScript (prenotazione e annullamento via `fetch` verso le API, grafici con Chart.js, anteprima dell'opera in sala).
- **Back-end**: ASP.NET Core 10 MVC in C# (linguaggio object-oriented).
  - `Controllers/` — pagine MVC.
  - `Controllers/Api/` — API REST in JSON, documentate con OpenAPI.
  - `Services/` — logica di dominio riusabile e testabile (`BookingService`, `DashboardService`).
- **Database**: SQL Server con Entity Framework Core (approccio *code-first*, migrazioni in `Data/Migrations`).
- **Sicurezza**: autenticazione con ASP.NET Identity, 3 ruoli (`Visitore`, `Staff`, `Admin`), protezione CSRF (anti-forgery token) anche sulle chiamate API, upload con controllo di estensione e dimensione.

### Modello dei dati (entità principali)

| Entità | Descrizione |
|---|---|
| `Artist` | Artista |
| `Artwork` | Opera (1 artista → N opere), con dimensioni e stato di disponibilità |
| `ArtworkAudioGuide` | Audioguida di un'opera per lingua |
| `ArtworkView`, `ArtworkQrScan` | Visualizzazioni e scansioni QR (analytics) |
| `ArtworkFavorite` | Opere preferite di un utente |
| `ArtworkInquiry`, `ArtworkInquiryMessage` | Richiesta di informazioni e conversazione con lo staff |
| `GalleryActivity` | Visita o evento con data, durata e posti massimi |
| `Booking` | Prenotazione (stato, QR, annullamento, check-in) |
| `VisitFeedback` | Valutazione 1–5 e commento dopo il check-in (1 per prenotazione) |
| `ApplicationUser` | Utente (ASP.NET Identity) |

---

## Requisiti

- Windows con **Visual Studio 2022** (17.14 o successivo) o Visual Studio 2026, carico di lavoro *Sviluppo ASP.NET e Web*
- **.NET SDK 10**
- **SQL Server LocalDB** (installato con Visual Studio)

## Avvio del progetto

1. Clonare il repository e aprire `BeArtGallery.slnx` con Visual Studio.
2. Avviare il progetto **BeArtGallery** (F5).

Al primo avvio l'applicazione:
- crea il database `BeArtGallery` su LocalDB e applica tutte le migrazioni;
- crea i ruoli `Visitore`, `Staff`, `Admin`;
- in ambiente *Development* inserisce i **dati dimostrativi** (artisti, opere, attività, prenotazioni, feedback).

In alternativa da riga di comando:

```bash
dotnet run --project BeArtGallery.csproj
```

### Account dimostrativi

| Ruolo | Email | Password |
|---|---|---|
| Admin | `admin@beartgallery.test` | `Demo123!` |
| Staff | `staff@beartgallery.test` | `Demo123!` |
| Visitore | `visitatore@beartgallery.test` | `Demo123!` |

I dati demo si attivano/disattivano in `appsettings.Development.json` (`DemoData:Enabled`). Il catalogo demo viene inserito solo se il database non contiene ancora artisti.

---

## API REST

La documentazione interattiva è disponibile, in ambiente Development, su:

- **Swagger UI**: `https://localhost:<porta>/swagger`
- **Documento OpenAPI**: `https://localhost:<porta>/openapi/v1.json`

| Metodo | Endpoint | Accesso | Descrizione |
|---|---|---|---|
| GET | `/api/artworks` | pubblico | Catalogo opere (filtri: `search`, `artistId`, `availability`) |
| GET | `/api/artworks/{id}` | pubblico | Dettaglio opera con audioguide attive |
| GET | `/api/activities` | pubblico | Visite ed eventi futuri con posti disponibili (filtri: `type`, `includePast`) |
| GET | `/api/activities/{id}` | pubblico | Dettaglio attività |
| GET | `/api/bookings/mine` | utente autenticato | Prenotazioni dell'utente |
| POST | `/api/bookings` | utente autenticato + token CSRF | Crea una prenotazione |
| POST | `/api/bookings/{id}/cancel` | utente autenticato + token CSRF | Annulla una prenotazione |
| GET | `/api/dashboard/stats` | Staff / Admin | Indicatori principali (KPI) |

Esempio di richiesta e risposta:

```http
POST /api/bookings
Content-Type: application/json
RequestVerificationToken: <token anti-forgery>

{ "galleryActivityId": 3, "numberOfParticipants": 2 }
```

```json
{ "success": true, "message": "Prenotazione effettuata con successo.", "bookingId": 12 }
```

Codici di risposta: `200` successo, `400` dati non validi, `401` non autenticato, `403` ruolo non autorizzato, `404` risorsa inesistente, `409` posti esauriti / prenotazione già presente / conflitto concorrente.

---

## Test

Il progetto `BeArtGallery.Tests` (xUnit) verifica le regole di business delle prenotazioni su un database **SQLite in memoria**:

- nessuna prenotazione oltre i posti disponibili (e le prenotazioni annullate liberano posti);
- nessuna doppia prenotazione dello stesso utente per la stessa attività;
- nessuna prenotazione o annullamento per attività già iniziate o annullate;
- numero di partecipanti tra 1 e 10;
- un utente non può annullare la prenotazione di un altro;
- check-in solo per prenotazioni confermate e una sola volta per QR.

Esecuzione: in Visual Studio *Test → Esegui tutti i test*, oppure

```bash
dotnet test
```

---

## Scelte tecniche rilevanti

- **Concorrenza sulle prenotazioni**: verifica dei posti e inserimento avvengono in un'unica transazione con isolamento `SERIALIZABLE`. Se due visitatori prenotano contemporaneamente gli ultimi posti, il database fa fallire una delle due transazioni (risposta `409`) invece di consentire l'overbooking.
- **Aggregazioni nel database**: i KPI della dashboard sono calcolati con query `COUNT`/`SUM`/`AVG`/`GROUP BY` eseguite da SQL Server; in memoria arrivano solo i risultati.
- **Date e orari**: gli orari delle attività sono in ora locale della galleria; i timestamp degli eventi (prenotazione, annullamento, check-in, feedback) sono salvati in UTC e convertiti in ora locale solo in visualizzazione. L'ora corrente è iniettata tramite `TimeProvider`, così i test usano un orologio fisso.
- **DTO per le API**: le API restituiscono oggetti dedicati (`Dtos/ApiDtos.cs`) e non le entità del database, per non esporre dati interni o relazioni cicliche.

## Limiti del prototipo e sviluppi futuri

- Nessun pagamento online dei biglietti → integrazione con un sistema di pagamento.
- Notifiche solo all'interno del sito → email o push per conferme e promemoria.
- Analytics interni semplici → integrazione con strumenti di analytics e reportistica avanzata.
- Database locale (LocalDB) → distribuzione su servizi cloud (es. Azure App Service + Azure SQL).
- Funzionalità di intelligenza artificiale: suggerimento di opere in base ai preferiti, analisi del sentiment dei commenti di feedback.
- Sicurezza: autenticazione a due fattori obbligatoria per Staff/Admin, rate limiting sulle API.
