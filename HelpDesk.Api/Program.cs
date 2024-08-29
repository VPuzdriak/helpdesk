using System.Text.Json.Serialization;
using HelpDesk.Api.Core.Marten;
using HelpDesk.Api.Incidents;
using HelpDesk.Api.Requests;
using HelpDesk.Api.Core.Http;
using Marten;
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
        options.AutoCreateSchemaObjects = AutoCreate.CreateOnly;
    }
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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

app.MapPut("agents/{agentId:guid}/incidents/{incidentId:guid}/category",
    async (
        IDocumentSession documentSession,
        Guid incidentId,
        Guid agentId,
        [FromHeader(Name = "If-Match")] string eTag,
        CategoryIncidentRequest body,
        CancellationToken ct) =>
    {
        var category = body.Category;

        await documentSession.GetAndUpdate<Incident>(
            incidentId,
            eTag.ToExpectedVersion(),
            current => Handle(current, new CategoriseIncident(current.Id, category, agentId)),
            ct);
    }).WithTags("Agent");

app.MapPut("agents/{agentId:guid}/incidents/{incidentId:guid}/priority",
    async (
        IDocumentSession documentSession,
        Guid incidentId,
        Guid agentId,
        [FromHeader(Name = "If-Match")] string eTag,
        PriorityIncidentRequest body,
        CancellationToken ct) =>
    {
        var priority = body.Priority;

        await documentSession.GetAndUpdate<Incident>(
            incidentId,
            eTag.ToExpectedVersion(),
            current => Handle(current, new PrioritiseIncident(current.Id, priority, agentId)),
            ct);
    }).WithTags("Agent");

app.MapPut("agents/{agentId:guid}/incidents/{incidentId:guid}/assign", async (
    IDocumentSession documentSession,
    Guid incidentId,
    Guid agentId,
    [FromHeader(Name = "If-Match")] string eTag,
    AssignIncidentRequest body,
    CancellationToken ct
) =>
{
    await documentSession.GetAndUpdate<Incident>(
        incidentId,
        eTag.ToExpectedVersion(),
        current => Handle(current, new AssignAgent(current.Id, agentId, body.AssignedBy)),
        ct);
}).WithTags("Agent");

app.Run();