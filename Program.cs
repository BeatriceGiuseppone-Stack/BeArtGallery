using System.Text.Json.Serialization;
using BeArtGallery.Data;
using BeArtGallery.Dtos;
using BeArtGallery.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// Crea l'applicazione web e legge la configurazione (appsettings.json)
var builder = WebApplication.CreateBuilder(args);

// Database e utenti

// Stringa di connessione al database SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Gestione degli utenti con Identity e dei ruoli
builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false).AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddErrorDescriber<BeArtGallery.Authorization.ItalianIdentityErrorDescriber>();

// Per le API non si fa il redirect alla pagina di login:
// si risponde con 401 (non autenticato) o 403 (non autorizzato)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

// Servizi con la logica dell'applicazione

// Orologio dell'applicazione: nei test si usa un orologio fisso
builder.Services.AddSingleton(TimeProvider.System);
// Servizi per prenotazioni e dashboard
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Controller MVC e API

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Nelle API gli stati vengono scritti come testo (es. "Confirmed")
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Errori di validazione delle API nello stesso formato delle altre risposte
        options.InvalidModelStateResponseFactory = context =>
        {
            var firstError = context.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m)) ?? "Dati della richiesta non validi.";

            return new BadRequestObjectResult(new ApiMessageResponse(false, firstError));
        };
    });

// Documentazione OpenAPI delle API, creata in automatico dai controller
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "BeArtGallery API";
        document.Info.Version = "v1";
        document.Info.Description = "API REST della piattaforma BeArtGallery: catalogo delle opere, " +
            "attività prenotabili, prenotazioni dei visitatori e indicatori della dashboard.";
        return Task.CompletedTask;
    });
});

// Sessione: serve per ricordare alcune informazioni tra una pagina e l'altra
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);

    options.Cookie.HttpOnly = true;

    options.Cookie.IsEssential = true;

    options.Cookie.SameSite = SameSiteMode.Lax;
});

var app = builder.Build();

// Gestione delle richieste HTTP

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();

    // In sviluppo la documentazione delle API è visibile su /swagger
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "BeArtGallery API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "BeArtGallery API";
    });
}
else
{
    // In produzione si mostra una pagina di errore generica
    app.UseExceptionHandler("/Home/Error");
    // HSTS: il browser userà sempre HTTPS per questo sito
    app.UseHsts();
}

// Reindirizza da HTTP a HTTPS
app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();

// Prima si controlla chi è l'utente, poi cosa può fare
app.UseAuthentication();
app.UseAuthorization();
// Rotte dei controller API (es. /api/artworks)
app.MapControllers();

app.MapStaticAssets();

// Rotta predefinita delle pagine: /Controller/Azione/Id
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();

// Pagine di login e registrazione (Identity)
app.MapRazorPages().WithStaticAssets();

// Preparazione del database all'avvio

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    // Applica le migration mancanti, così il database è sempre aggiornato
    var db = services.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    // Crea i ruoli e l'utente amministratore
    await IdentitySeeder.SeedAsync(services, builder.Configuration);

    // Inserisce i dati di esempio, solo se attivati in configurazione
    if (builder.Configuration.GetValue<bool>("DemoData:Enabled"))
    {
        await DemoDataSeeder.SeedAsync(services, builder.Configuration);
    }
}

app.Run();