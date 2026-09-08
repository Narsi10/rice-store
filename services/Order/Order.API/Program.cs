using System.Reflection;
using BuildingBlocks.Auth;
using BuildingBlocks.Messaging;
using Microsoft.EntityFrameworkCore;
using Order.API.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("OrderDb")));

// Registers MassTransit + RabbitMQ and auto-discovers the saga consumers.
builder.Services.AddEventBus(builder.Configuration, Assembly.GetExecutingAssembly());

// JWT auth so admin endpoints can be protected with [Authorize(Roles="Admin")].
builder.Services.AddJwtAuth(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();
