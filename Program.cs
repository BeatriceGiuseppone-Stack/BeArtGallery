using System.Text.Json.Serialization;
using BeArtGallery.Data;
using BeArtGallery.Dtos;
using BeArtGallery.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// DATABASE E IDENTITÀ
// =====================================================

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(
    options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Le chiamate alle API non devono essere reindirizzate alla
// pagina di login (HTML): rispondono con 401 / 403.
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

// =====================================================
// SERVIZI APPLICATIVI (logica di dominio)
// =====================================================

// TimeProvider rende "l'ora corrente" una dipendenza:
// nei test può essere sostituito da un orologio fisso.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// =====================================================
// MVC + API
// =====================================================

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Le enum nelle API sono esposte come testo
        // (es. "Confirmed" invece di 1).
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Errori di validazione delle API nello stesso formato
        // { success, message } usato dalle altre risposte.
        options.InvalidModelStateResponseFactory = context =>
        {
            var firstError =
                context.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                ?? "Dati della richiesta non validi.";

            return new BadRequestObjectResult(
                new ApiMessageResponse(false, firstError));
        };
    });

// Documento OpenAPI generato automaticamente dai controller API
// (comprese le descrizioni scritte nei commenti XML ///).
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "BeArtGallery API";
        document.Info.Version = "v1";
        document.Info.Description =
            "API REST della piattaforma BeArtGallery: catalogo delle opere, " +
            "attività prenotabili, prenotazioni dei visitatori e indicatori della dashboard.";
        return Task.CompletedTask;
    });
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout =
        TimeSpan.FromMinutes(30);

    options.Cookie.HttpOnly =
        true;

    options.Cookie.IsEssential =
        true;

    options.Cookie.SameSite =
        SameSiteMode.Lax;
});

var app = builder.Build();

// =====================================================
// PIPELINE HTTP
// =====================================================

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();

    // Documentazione interattiva delle API:
    //   /openapi/v1.json  -> documento OpenAPI
    //   /swagger          -> interfaccia Swagger UI
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
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

// =====================================================
// INIZIALIZZAZIONE DATABASE
// =====================================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    // Applica le migrazioni mancanti: chi scarica il progetto
    // non deve eseguire a mano "Update-Database".
    var db = services.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    await IdentitySeeder.SeedAsync(
        services,
        builder.Configuration);

    // Dati dimostrativi (solo se abilitati in configurazione).
    if (builder.Configuration.GetValue<bool>("DemoData:Enabled"))
    {
        await DemoDataSeeder.SeedAsync(
            services,
            builder.Configuration);
    }
}

app.Run();
