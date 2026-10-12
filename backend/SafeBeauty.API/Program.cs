using SafeBeauty.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SafeBeauty.API.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SafeBeauty.API.Authentication;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddDbContext<SafeBeautyDbContext>(options =>
    options.UseSqlite(connectionString));

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key configuration is missing.");

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<SafeBeautyDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Keep JWT claim names unchanged so controllers can read the standard
    // "sub" claim directly instead of relying on legacy claim mapping.
    options.MapInboundClaims = false;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Issuer"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey))
    };

    // Browsers receive the JWT as an HttpOnly cookie. Keeping the bearer
    // handler means [Authorize] continues to validate the token normally,
    // but application JavaScript never needs access to the token itself.
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (string.IsNullOrEmpty(context.Token)
                && context.Request.Cookies.TryGetValue(AuthConstants.CookieName, out var token))
            {
                context.Token = token;
            }

            return Task.CompletedTask;
        }
    };
});


builder.Services.AddScoped<DataSeeder>();
builder.Services.AddScoped<IngredientDeduplicationService>();
builder.Services.AddHttpClient<HuggingFaceService>();
builder.Services.AddHttpClient<AiSummaryService>();

builder.Services.AddHttpClient<PubChemService>(client =>
{
    client.BaseAddress = new Uri(
        "https://pubchem.ncbi.nlm.nih.gov/rest/pug/"
    );

    client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddScoped<IngredientAnalysisService>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<EmailService>();


// CORS: allow the deployed frontend origin to call this API from the browser
// CORS- Cross-Origin Resource Sharing - protection for browser
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "https://natashatoul.github.io",
                "http://localhost:3000",
                "http://localhost:5173",
                "http://localhost:5174")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SafeBeautyDbContext>();
    var migrator = db.GetService<IMigrator>();
    const string normalizationMigration =
        "20260709085218_AddNormalizedIngredientName";

    var appliedMigrations = (await db.Database.GetAppliedMigrationsAsync())
        .ToHashSet(StringComparer.Ordinal);

    // On existing databases, stop after the column migration so historical
    // duplicates can be merged before the unique index migration is applied.
    if (!appliedMigrations.Contains(normalizationMigration))
    {
        await migrator.MigrateAsync(normalizationMigration);
    }


    var deduplicationService = scope.ServiceProvider
        .GetRequiredService<IngredientDeduplicationService>();
    await deduplicationService.RunAsync();

    // Apply the unique-index migration and any later migrations only after the
    // duplicate cleanup transaction has committed.
    await migrator.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

// HttpOnly authentication cookies are sent automatically by the browser.
// Requiring a custom header on state-changing authenticated requests prevents
// another site from submitting those requests as ordinary HTML forms.
app.Use(async (context, next) =>
{
    var isStateChangingRequest = HttpMethods.IsPost(context.Request.Method)
        || HttpMethods.IsPut(context.Request.Method)
        || HttpMethods.IsPatch(context.Request.Method)
        || HttpMethods.IsDelete(context.Request.Method);

    if (isStateChangingRequest
        && context.Request.Cookies.ContainsKey(AuthConstants.CookieName)
        && context.Request.Headers[AuthConstants.CsrfHeaderName] != "1")
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("Missing CSRF protection header.");
        return;
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => "SafeBeauty API is running");

app.Run();


