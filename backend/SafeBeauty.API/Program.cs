using SafeBeauty.API.Data;
using Microsoft.EntityFrameworkCore;
using SafeBeauty.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

// builder.Services.AddDbContext<SafeBeautyDbContext>(options =>
//     options.UseSqlite("Data Source=safebeauty.db"));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<SafeBeautyDbContext>(options => options.UseSqlite(connectionString));
}
else
{
    builder.Services.AddDbContext<SafeBeautyDbContext>(options => options.UseSqlServer(connectionString));
}
builder.Services.AddScoped<DataSeeder>();
builder.Services.AddScoped<IngredientAnalysisService>();
builder.Services.AddHttpClient();

// heandshake with frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SafeBeautyDbContext>();
    await db.Database.MigrateAsync();
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

app.Run();


