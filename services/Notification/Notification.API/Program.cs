using System.Reflection;
using BuildingBlocks.Messaging;
using Notification.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Email sender (Brevo SMTP relay).
builder.Services.AddScoped<IEmailSender, BrevoEmailSender>();

builder.Services.AddEventBus(builder.Configuration, Assembly.GetExecutingAssembly());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Health endpoint so the gateway/ops can check the service is alive.
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "notification" }));

app.Run();
