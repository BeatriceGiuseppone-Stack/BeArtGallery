# BeArtGallery

Prototipo di piattaforma digitale per una **galleria d'arte** (impresa culturale simulata), realizzato come Project Work del CdS *Informatica per le Aziende Digitali (L-31)* — traccia PW 22: *Digitalizzazione dei processi aziendali in un'impresa culturale*.

La piattaforma digitalizza cinque processi della galleria:

| Processo | Prima (strumenti frammentati) | Con BeArtGallery |
|---|---|---|
| Catalogo delle opere | Fogli di calcolo, schede cartacee | Catalogo digitale con artisti, opere, immagini, audioguide e QR code in sala |
| Prenotazione degli eventi | Email, telefono | Prenotazione online con controllo dei posti e biglietto digitale (QR) |
| Accoglienza dei visitatori | Lista cartacea all'ingresso | Check-in tramite scansione del QR della prenotazione |
| Relazione con il pubblico | Email sparse | Richieste di informazioni sulle opere, preferiti, feedback dopo la partecipazione a un evento |
| Monitoraggio | Conteggi manuali | Dashboard con indicatori (presenze, occupazione, interesse, soddisfazione) |

---

## Architettura

```
┌──────────────────────┐   HTTP (HTML)    ┌────────────────────────────────┐   EF Core / SQL   ┌──────────────────┐
│  Browser             │ ───────────────► │  ASP.NET Core 10 (C#)          │ ────────────────► │  SQL Server      │
│  Razor + CSS + JS    │                  │  • Controller MVC (pagine)     │                   │  (LocalDB)       │
│  fetch() / Chart.js  │ ◄─────────────── │  • Controller API REST (JSON)  │ ◄──────────────── │  database        │
└──────────────────────┘   HTTP (JSON)    │  • Servizi di dominio          │                   │  relazionale     │
                                          │  • ASP.NET Identity (ruoli)    │                   └──────────────────┘
                                          └────────────────────────────────┘
```

- **Front-end**: viste Razor (HTML), CSS personalizzato, Bootstrap 5, JavaScript (prenotazione e annullamento via `fetch` verso le API, grafici con Chart.js, anteprima dell'opera nella propria stanza).
- **Back-end**: ASP.NET Core 10 MVC in C# (linguaggio orientato agli oggetti).
  - `Controllers/` — pagine MVC.
  - `Controllers/Api/` — API REST in JSON, documentate con OpenAPI.
  - `Services/` — logica di dominio riusabile e testabile (`BookingService`, `DashboardService`).
- **Database**: SQL Server con Entity Framework Core (approccio *code-first*, migrazioni in `Data/Migrations`).
- **Sicurezza**: reindirizzamento automatico da HTTP a HTTPS, autenticazione con ASP.NET Identity e 3 ruoli (Visitatore, Staff, Admin), autorizzazioni verificate sul server, protezione CSRF (anti-forgery token) anche sulle chiamate API, upload con controllo di estensione e dimensione.

> Nel codice il ruolo Visitatore è definito come `RoleNames.Visitore` (valore `"Visitore"`); nell'interfaccia viene sempre mostrato come "Visitatore".

### Modello dei dati (entità principali)

| Entità | Descrizione |
|---|---|
| `Artist` | Artista |
| `Artwork` | Opera (1 artista → N opere), con dimensioni e stato di disponibilità |
| `ArtworkAudioGuide` | Audioguida di un'opera per lingua |
| `ArtworkView`, `ArtworkQrScan` | Visualizzazioni e scansioni QR (analytics) |
| `ArtworkFavorite` | Opere preferite di un utente |
| `ArtworkInquiry`, `ArtworkInquiryMessage` | Richiesta di informazioni e conversazione con lo staff |
| `GalleryActivity` | Evento con data, durata, luogo, posti massimi e stato |
| `Booking` | Prenotazione (stato, QR, annullamento, check-in) |
| `VisitFeedback` | Valutazione 1–5 e commento dopo il check-in (1 per prenotazione) |
| `ApplicationUser` | Utente (ASP.NET Identity) |

---

## Requisiti

- Windows con **Visual Studio 2026**, carico di lavoro *Sviluppo ASP.NET e Web*
- **.NET SDK 10**
- **SQL Server LocalDB** (installato con Visual Studio)

## Avvio del progetto

1. Clonare il repository (oppure scaricarlo da *Code → Download ZIP*) e aprire `BeArtGallery.slnx` con Visual Studio.
2. Avviare il progetto **BeArtGallery** (F5).

Al primo avvio l'applicazione:
- crea il database `BeArtGallery` su LocalDB e applica tutte le migrazioni;
- crea i ruoli Visitatore, Staff e Admin;
- in ambiente *Development* inserisce i **dati dimostrativi** (artisti, opere, eventi, prenotazioni, feedback) e gli account di prova.

In alternativa da riga di comando:

```bash
dotnet run --project BeArtGallery.csproj
```

### Account dimostrativi

| Ruolo | Email | Password |
|---|---|---|
| Admin | `admin@beartgallery.test` | `Demo123!` |
| Staff | `staff@beartgallery.test` | `Demo123!` |
| Visitatore | `visitatore@beartgallery.test` | `Demo123!` |

Gli account sono solo di prova e funzionano esclusivamente in locale. I dati demo si attivano/disattivano in `appsettings.Development.json` (`DemoData:Enabled`); il catalogo demo viene inserito solo se il database non contiene ancora artisti.

---

## API REST

La documentazione interattiva è disponibile, in ambiente Development, su:

- **Swagger UI**: `https://localhost:<porta>/swagger`
- **Documento OpenAPI**: `https://localhost:<porta>/openapi/v1.json`

Il documento OpenAPI è generato dalla struttura degli endpoint e arricchito dai commenti XML presenti nel codice.

| Metodo | Endpoint | Accesso | Descrizione |
|---|---|---|---|
| GET | `/api/artworks` | pubblico | Catalogo opere (filtri: `search`, `artistId`, `availability`) |
| GET | `/api/artworks/{id}` | pubblico | Dettaglio opera con audioguide attive |
| GET | `/api/activities` | pubblico | Eventi futuri con posti disponibili (filtri: `type`, `includePast`) |
| GET | `/api/activities/{id}` | pubblico | Dettaglio di un evento con i posti ancora liberi |
| GET | `/api/bookings/mine` | utente autenticato | Prenotazioni dell'utente |
| POST | `/api/bookings` | utente autenticato + token CSRF | Crea una prenotazione |
| POST | `/api/bookings/{id}/cancel` | utente autenticato + token CSRF | Annulla una prenotazione (solo prima dell'inizio dell'evento) |
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

Il progetto `BeArtGallery.Tests` (xUnit) usa un database **SQLite in memoria**, creato e distrutto per ogni test, e un orologio fisso. In totale i test sono **33**.

`BookingServiceTests` (17 test) verifica le regole delle prenotazioni:

- nessuna prenotazione oltre i posti disponibili (e le prenotazioni annullate liberano posti);
- nessuna doppia prenotazione dello stesso utente per lo stesso evento;
- nessuna prenotazione o annullamento per eventi già iniziati o annullati;
- numero di partecipanti tra 1 e 10;
- un utente non può annullare la prenotazione di un altro;
- check-in solo per prenotazioni confermate e una sola volta per QR.

`DashboardServiceTests` (9 test) verifica gli indicatori della dashboard e i feedback:

- il tasso di presenza considera solo gli eventi conclusi (inizio + durata già trascorsi);
- l'occupazione esclude eventi annullati, eventi passati e prenotazioni annullate;
- con il database vuoto nessun indicatore divide per zero;
- media dei feedback, insight automatici e riepilogo mensile delle prenotazioni;
- il database rifiuta un secondo feedback sulla stessa prenotazione.

`PaginationTests` (6 test) verifica la paginazione dell'elenco artisti (9 per pagina): ordine alfabetico, ultima pagina parziale e numeri di pagina fuori intervallo riportati entro i limiti.

`ArtworkDeletionTests` (1 test) verifica che, eliminando un'opera, le richieste di informazioni collegate restino nel database: perdono solo il collegamento all'opera (`ON DELETE SET NULL`) e conservano titolo e artista salvati al momento dell'invio.

Esecuzione: in Visual Studio *Test → Esegui tutti i test* (finestra *Esplora test*), oppure

```bash
dotnet test
```

---

## Scelte tecniche rilevanti

- **Concorrenza sulle prenotazioni**: verifica dei posti e inserimento avvengono in un'unica transazione con isolamento `SERIALIZABLE`. Se due visitatori prenotano contemporaneamente gli ultimi posti, il database fa fallire una delle due transazioni (risposta `409`) invece di consentire l'overbooking.
- **Aggregazioni nel database**: i conteggi e le somme della dashboard sono calcolati con query `COUNT`/`SUM`/`AVG`/`GROUP BY` eseguite da SQL Server; il servizio riceve solo i risultati e ne ricava le percentuali.
- **Tasso di presenza**: confronta i partecipanti con check-in con quelli attesi, considerando solo gli eventi già conclusi; se non ci sono eventi conclusi restituisce 0, per evitare una divisione per zero.
- **Conservazione delle richieste**: eliminando un'opera, le richieste di informazioni collegate non vengono cancellate (`ON DELETE SET NULL`); l'eliminazione è però bloccata finché esiste una conversazione ancora aperta.
- **Date e orari**: gli orari degli eventi sono in ora locale della galleria; i timestamp (prenotazione, annullamento, check-in, feedback) sono salvati in UTC e convertiti in ora locale solo in visualizzazione. Nei servizi di prenotazione e dashboard l'ora corrente è fornita tramite `TimeProvider`, così i test usano un orologio fisso.
- **DTO per le API**: le API restituiscono oggetti dedicati (`Dtos/ApiDtos.cs`) e non le entità del database, per non esporre dati interni o relazioni cicliche.
- **Misure delle opere**: larghezza e altezza in centimetri sono di tipo `decimal`, per evitare le approssimazioni dei numeri in virgola mobile.

## Limiti del prototipo e sviluppi futuri

- Nessun pagamento online dei biglietti → integrazione con un sistema di pagamento.
- Notifiche solo all'interno del sito → email o push per conferme e promemoria.
- Analytics interni semplici → integrazione con strumenti di analytics e reportistica avanzata.
- Database locale (LocalDB) → distribuzione su servizi cloud con database gestito e backup automatici.
- Funzionalità di intelligenza artificiale: suggerimento di opere in base ai preferiti, analisi del sentiment dei commenti di feedback.
- Sicurezza: autenticazione a due fattori obbligatoria per Staff/Admin, limiti al numero di richieste sulle API, test automatici delle autorizzazioni.