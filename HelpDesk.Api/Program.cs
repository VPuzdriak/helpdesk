using System.Text.Json.Serialization;
using HelpDesk.Api.Core.Marten;
using HelpDesk.Api.Incidents;
using HelpDesk.Api.Requests;
using Marten;
using Marten.Events.Daemon.Coordination;
using Marten.Events.Daemon.Resiliency;
using Marten.Events.Projections;
using Marten.Pagination;
using Marten.Schema.Identity;
using Microsoft.AspNetCore.Mvc;
using Weasel.Core;
using static HelpDesk.Api.Incidents.IncidentDomainService;
using static Microsoft.AspNetCore.Http.Results;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(o =>
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddMarten(options =>
    {
        options.Connection(builder.Configuration.GetConnectionString("Events")!);
        options.UseSystemTextJsonForSerialization();

        if (builder.Environment.IsDevelopment())
        {
            options.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;
        }

        options.Projections.Add<IncidentShortInfoProjection>(ProjectionLifecycle.Inline);
    })
    .UseLightweightSessions()
    .AddAsyncDaemon(DaemonMode.Solo);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("projections/{projectionName}/reset",
        async (IProjectionCoordinator projectionCoordinator, string projectionName) =>
        {
            var daemon = projectionCoordinator.DaemonForMainDatabase();
            await daemon.RebuildProjectionAsync(projectionName, CancellationToken.None);
        })
    .WithTags("Projection");

app.MapPost("customers/{customerId:guid}/incidents",
    async (
        IDocumentSession documentSession,
        Guid customerId,
        LogIncidentRequest body,
        CancellationToken ct) =>
    {
        var (contact, description, loggedBy) = body;
        var incidentId = CombGuidIdGeneration.NewGuid();

        await documentSession.Add<Incident>(
            incidentId,
            Handle(new LogIncident(incidentId, customerId, contact, description, loggedBy)),
            ct);

        return Created($"incidents/{incidentId}", incidentId);
    }).WithTags("Customer");

app.MapPut("agents/{agentId:guid}/incidents/{incidentId:guid}/{version:int}/category",
    async (
        IDocumentSession documentSession,
        Guid incidentId,
        Guid agentId,
        int version,
        CategoryIncidentRequest body,
        CancellationToken ct) =>
    {
        var category = body.Category;

        await documentSession.GetAndUpdate<Incident>(
            incidentId,
            version,
            current => Handle(current, new CategoriseIncident(current.Id, category, agentId)),
            ct);
    }).WithTags("Agent");

app.MapPut("agents/{agentId:guid}/incidents/{incidentId:guid}/{version:int}/priority",
    async (
        IDocumentSession documentSession,
        Guid incidentId,
        Guid agentId,
        int version,
        PriorityIncidentRequest body,
        CancellationToken ct) =>
    {
        var priority = body.Priority;

        await documentSession.GetAndUpdate<Incident>(
            incidentId,
            version,
            current => Handle(current, new PrioritiseIncident(current.Id, priority, agentId)),
            ct);
    }).WithTags("Agent");

app.MapPut("agents/{agentId:guid}/incidents/{incidentId:guid}/{version:int}/assign",
    async (
        IDocumentSession documentSession,
        Guid incidentId,
        Guid agentId,
        int version,
        AssignIncidentRequest body,
        CancellationToken ct
    ) =>
    {
        await documentSession.GetAndUpdate<Incident>(
            incidentId,
            version,
            current => Handle(current, new AssignAgent(current.Id, agentId, body.AssignedBy)),
            ct);
    }).WithTags("Agent");

app.MapGet("customers/{customerId:guid}/incidents",
        (
            IQuerySession querySession,
            Guid customerId,
            [FromQuery] int? pageNumber,
            [FromQuery] int? pageSize,
            CancellationToken ct
        ) => querySession.Query<IncidentShortInfo>()
            .Where(i => i.CustomerId == customerId)
            .ToPagedListAsync(pageNumber ?? 1, pageSize ?? 10, ct))
    .WithTags("Customer");

app.MapGet("incidents/{incidentId:guid}",
        (
            IDocumentSession documentSession,
            Guid incidentId,
            CancellationToken ct
        ) => documentSession.Query<IncidentShortInfo>().FirstOrDefaultAsync(i => i.Id == incidentId, ct))
    .WithTags("Customer");

app.Run();