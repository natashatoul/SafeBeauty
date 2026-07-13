using SafeBeauty.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SafeBeauty.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// builder.Services.AddControllers();
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

// if (builder.Environment.IsDevelopment())
// {
//     builder.Services.AddDbContext<SafeBeautyDbContext>(options => options.UseSqlite(connectionString));
// }
// else
// {
//     builder.Services.AddDbContext<SafeBeautyDbContext>(options => options.UseSqlServer(connectionString));
// }
builder.Services.AddScoped<DataSeeder>();
builder.Services.AddScoped<IngredientDeduplicationService>();
builder.Services.AddHttpClient<HuggingFaceService>();
builder.Services.AddHttpClient<AiSummaryService>();
builder.Services.AddScoped<IngredientAnalysisService>();

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
            .AllowAnyMethod();
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

app.MapControllers();

app.MapGet("/", () => "SafeBeauty API is running");

app.Run();


