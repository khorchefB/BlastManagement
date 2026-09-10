using System.Text.Json.Serialization;
using BlastManagement.Api.Api.Endpoints;
using BlastManagement.Api.Api.ExceptionHandling;
using BlastManagement.Api.Application.Abstractions;
using BlastManagement.Api.Application.Commands.AddHole;
using BlastManagement.Api.Application.Commands.ChargeHole;
using BlastManagement.Api.Application.Commands.CreateBlast;
using BlastManagement.Api.Application.Commands.FireBlast;
using BlastManagement.Api.Application.Commands.MarkHoleReady;
using BlastManagement.Api.Application.Configuration;
using BlastManagement.Api.Application.Queries.GetBlast;
using BlastManagement.Api.Application.Queries.GetBlastHistory;
using BlastManagement.Api.Infrastructure.EventStore;
using BlastManagement.Api.Infrastructure.Projections;
using BlastManagement.Api.Infrastructure.Repositories;
using BlastManagement.Api.Infrastructure.Time;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

var requireReadyToFire = bool.TryParse(
    builder.Configuration["BlastRules:RequireReadyToFire"],
    out var configuredValue) && configuredValue;

builder.Services.AddSingleton(new BlastRules(requireReadyToFire));
builder.Services.AddSingleton<IClock, SystemClock>();

// Event-store design:
// - One append-only stream per Blast aggregate; Hole events live in that same
//   stream so FireBlast can enforce its cross-hole invariant atomically.
// - expectedVersion implements optimistic concurrency (409 on a stale write).
// - Appended events are synchronously sent to an in-memory projection. Reads
//   are therefore cheap and immediately consistent in this process. The trade-
//   off is that a production projection needs durable checkpoints, retries and
//   replay support; the event stream remains the source of truth.
builder.Services.AddSingleton<BlastReadModelProjection>();
builder.Services.AddSingleton<IEventProjection>(provider =>
    provider.GetRequiredService<BlastReadModelProjection>());
builder.Services.AddSingleton<IBlastReadStore>(provider =>
    provider.GetRequiredService<BlastReadModelProjection>());
builder.Services.AddSingleton<IEventStoreInMemory, EventStoreInMemory>();
builder.Services.AddSingleton<IBlastRepository, EventSourcedBlastRepository>();

builder.Services.AddTransient<CreateBlastCommandHandler>();
builder.Services.AddTransient<AddHoleCommandHandler>();
builder.Services.AddTransient<ChargeHoleCommandHandler>();
builder.Services.AddTransient<MarkHoleReadyCommandHandler>();
builder.Services.AddTransient<FireBlastCommandHandler>();
builder.Services.AddTransient<GetBlastQueryHandler>();
builder.Services.AddTransient<GetBlastHistoryQueryHandler>();

var app = builder.Build();

app.UseExceptionHandler();

app.MapGet("/", () => Results.Ok(new
{
    service = "Blast Management API",
    eventStore = "in-memory",
    endpoints = "/blasts"
}));

app.MapBlastEndpoints();

app.Run();

public partial class Program
{
}
