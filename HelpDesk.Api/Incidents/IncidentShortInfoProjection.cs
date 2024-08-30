using Marten.Events.Aggregation;

namespace HelpDesk.Api.Incidents;

public record IncidentShortInfo(
    Guid Id,
    Guid CustomerId,
    IncidentStatus Status,
    int NotesCount,
    IncidentCategory? Category = null,
    IncidentPriority? Priority = null
);

public class IncidentShortInfoProjection : SingleStreamProjection<IncidentShortInfo>
{
    public IncidentShortInfoProjection()
    {
        ProjectionName = "incident_short_info";
    }

    public static IncidentShortInfo Create(IncidentLogged logged) =>
        new(logged.IncidentId, logged.CustomerId, IncidentStatus.Pending, 0);

    public IncidentShortInfo Apply(IncidentCategorised categorised, IncidentShortInfo current) =>
        current with { Category = categorised.Category };

    public IncidentShortInfo Apply(IncidentPrioritised prioritised, IncidentShortInfo current) =>
        current with { Priority = prioritised.Priority };

    public IncidentShortInfo Apply(AgentRespondedToIncident _, IncidentShortInfo current) =>
        current with { NotesCount = current.NotesCount + 1 };

    public IncidentShortInfo Apply(CustomerRespondedToIncident _, IncidentShortInfo current) =>
        current with { NotesCount = current.NotesCount + 1 };
}