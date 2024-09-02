using HelpDesk.Api.Core.Marten;
using HelpDesk.Api.Incidents;
using HelpDesk.Api.Requests;
using Marten;
using Marten.Events.Daemon.Coordination;
using Marten.Pagination;
using Marten.Schema.Identity;
using Microsoft.AspNetCore.Mvc;
using static HelpDesk.Api.Incidents.IncidentDomainService;

namespace HelpDesk.Api.Endpoints;

internal static class EndpointsExtensions
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("projections/{projectionName}/reset",
            async ([FromServices] IProjectionCoordinator projectionCoordinator, [FromRoute] string projectionName,
                CancellationToken ct) =>
            {
                var daemon = projectionCoordinator.DaemonForMainDatabase();
                await daemon.RebuildProjectionAsync(projectionName, ct);
            }
        );

        builder.MapPost("customers/{customerId:guid}/incidents",
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

                return Results.Created($"incidents/{incidentId}", incidentId);
            }).WithTags("Customer");

        builder.MapPut("agents/{agentId:guid}/incidents/{incidentId:guid}/category",
            async (
                IDocumentSession documentSession,
                Guid incidentId,
                Guid agentId,
                [FromHeader(Name = "X-Version")] int version,
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

        builder.MapPut("agents/{agentId:guid}/incidents/{incidentId:guid}/priority",
            async (
                IDocumentSession documentSession,
                Guid incidentId,
                Guid agentId,
                [FromHeader(Name = "X-Version")] int version,
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

        builder.MapPut("agents/{agentId:guid}/incidents/{incidentId:guid}/assign",
            async (
                IDocumentSession documentSession,
                Guid incidentId,
                Guid agentId,
                [FromHeader(Name = "X-Version")] int version,
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

        builder.MapGet("customers/{customerId:guid}/incidents",
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

        builder.MapGet("incidents/{incidentId:guid}",
                (
                    IQuerySession querySession,
                    Guid incidentId,
                    CancellationToken ct
                ) => querySession.Query<IncidentShortInfo>().FirstOrDefaultAsync(i => i.Id == incidentId, ct))
            .WithTags("Customer");

        return builder;
    }
}