using Asp.Versioning;
using FluentValidation;
using JL.Commerce.Tecnology.Service.Application.Behaviors;
using JL.Commerce.Tecnology.Service.Application.Commands.CreateEntity;
using JL.Commerce.Tecnology.Service.Application.Mappings;
using JL.Commerce.Tecnology.Service.Application.Ports;
using JL.Commerce.Tecnology.Service.Infrastructure.Data.Context;
using JL.Commerce.Tecnology.Service.Infrastructure.Data.Repositories;
using JL.Commerce.Tecnology.Service.Infrastructure.Integration.Messaging.Consumers;
using JL.Commerce.Tecnology.Service.Infrastructure.Integration.PaymentGateway;
using JL.Commerce.Tecnology.Service.Presentation.Endpoints;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── OpenAPI 3.1 (native, no Swashbuckle generation) ─────────────────────────
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((doc, _, _) =>
    {
        doc.Info = new()
        {
            Title       = "JL.Commerce.Tecnology.Service API",
            Version     = "v1",
            Description = "Hexagonal Architecture · DDD · CQRS · .NET 10"
        };
        return Task.CompletedTask;
    });
});

// ── API Versioning ────────────────────────────────────────────────────────────
builder.Services.AddApiVersioning(opt =>
{
    opt.DefaultApiVersion                   = new ApiVersion(1, 0);
    opt.AssumeDefaultVersionWhenUnspecified = true;
    opt.ReportApiVersions                   = true;
});

// ── MediatR + Pipeline Behaviors ─────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateEntityCommand).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// ── FluentValidation ─────────────────────────────────────────────────────────
builder.Services.AddValidatorsFromAssembly(typeof(CreateEntityCommand).Assembly);

// ── AutoMapper ────────────────────────────────────────────────────────────────
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(EntityMappingProfile).Assembly));

// ── EF Core + PostgreSQL ──────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

// ── Repository ────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IEntityRepository, EntityRepository>();
builder.Services.AddScoped<ICatalogProductRepository, CatalogProductRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IPaymentGateway, MockPaymentGateway>();

// ── In-Memory Cache ───────────────────────────────────────────────────────────
builder.Services.AddMemoryCache();

// ── MassTransit (In-Memory transport) ────────────────────────────────────────
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<EntityCreatedConsumer>();
    x.AddConsumer<CatalogProductCreatedConsumer>();
    x.AddConsumer<UserCreatedConsumer>();
    x.AddConsumer<OrderCreatedConsumer>();
    x.UsingInMemory((ctx, cfg) => cfg.ConfigureEndpoints(ctx));
});

// ── Authentication (JWT Bearer) ───────────────────────────────────────────────
builder.Services.AddAuthentication()
    .AddJwtBearer(opt =>
    {
        opt.Authority = builder.Configuration["Jwt:Authority"];
        opt.Audience  = builder.Configuration["Jwt:Audience"];
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// ── Global exception handler ──────────────────────────────────────────────────
app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
{
    ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
    ctx.Response.ContentType = "application/json";
    var correlationId = ctx.TraceIdentifier;
    await ctx.Response.WriteAsJsonAsync(new
    {
        correlationId,
        message = "An unexpected error occurred. Please try again later."
    });
}));

// ── OpenAPI JSON endpoint ─────────────────────────────────────────────────────
app.MapOpenApi();                         // → GET /openapi/v1.json

// ── ReDoc UI ──────────────────────────────────────────────────────────────────
app.MapGet("/docs", () => Results.Content("""
    <!DOCTYPE html>
    <html>
    <head>
        <title>JL.Commerce.Tecnology.Service API Docs</title>
        <meta charset="utf-8"/>
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <link href="https://fonts.googleapis.com/css?family=Montserrat:300,400,700|Roboto:300,400,700" rel="stylesheet">
        <style>body { margin: 0; padding: 0; }</style>
    </head>
    <body>
        <redoc spec-url='/openapi/v1.json'></redoc>
        <script src="https://cdn.redoc.ly/redoc/latest/bundles/redoc.standalone.js"></script>
    </body>
    </html>
    """, "text/html"))
    .ExcludeFromDescription();

app.UseAuthentication();
app.UseAuthorization();

// ── Route registration ────────────────────────────────────────────────────────
app.MapEntityEndpoints();
app.MapCatalogProductEndpoints();
app.MapUserEndpoints();
app.MapOrderEndpoints();

app.Run();
