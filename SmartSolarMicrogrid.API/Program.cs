using SmartSolarMicrogrid.API.Configuration;
using SmartSolarMicrogrid.API.Data;
using SmartSolarMicrogrid.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ── Services ────────────────────────────────────────────────────────
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title   = "Smart Solar Microgrid API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description  = "Enter your JWT token directly (without 'Bearer ' prefix)"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});



var app = builder.Build();

// ── Database seeding ─────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
    await DatabaseSeeder.SeedAsync(dbContext);
    var indexInitializer = scope.ServiceProvider.GetRequiredService<IndexInitializer>();
    await indexInitializer.RunAsync();
}

// ── Middleware pipeline ───────────────────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Solar Microgrid API v1");
    c.RoutePrefix = "swagger";
});

// app.UseHttpsRedirection();
app.UseCors("AllowWebApp");
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();

app.Run();
