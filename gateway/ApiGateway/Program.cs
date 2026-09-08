var builder = WebApplication.CreateBuilder(args);

// YARP reverse proxy: routes/clusters are defined in appsettings.json.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseCors();
app.MapGet("/", () => "Rice Store API Gateway is running.");
app.MapReverseProxy();

app.Run();
