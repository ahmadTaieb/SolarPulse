using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Solar.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Controllers, CORS, and Swagger / OpenAPI
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SolarPulse - Solar Telemetry & Energy API",
        Version = "v1",
        Description = "Real-time solar inverter telemetry synchronization, grid availability analytics, and energy monitoring platform."
    });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// 2. Register all Solar infrastructure, options, and domain services
builder.Services.AddSolarServices(builder.Configuration);

var app = builder.Build();

// 3. Automatically apply EF Core database migrations on startup if enabled
await app.ApplyDatabaseMigrationsAsync();

// 4. Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SolarPulse API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();

// Enable serving default document (index.html) and static assets from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();
app.MapControllers();

// Redirect /dashboard to root or serve index.html directly
app.MapGet("/dashboard", () => Results.Redirect("/"));
app.MapFallbackToFile("index.html");

app.Run();
