using CryptiqChat.Data;
using CryptiqChat.Hubs;
using CryptiqChat.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Base de datos ──────────────────────────────────────────
builder.Services.AddDbContext<CryptiqDbContext>(options => 
    options.UseSqlServer(builder.Configuration.GetConnectionString("CryptiqDB")) 
        .EnableSensitiveDataLogging() 
        .LogTo(Console.WriteLine, LogLevel.Information));

// ── Servicios ──────────────────────────────────────────────
builder.Services.AddScoped<ChatService>();

// ── SignalR ────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── CORS ───────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("CryptiqPolicy", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithOrigins(
                "http://localhost:3000",
                "http://localhost:5098",
                "null"
            );
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("CryptiqPolicy");
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();