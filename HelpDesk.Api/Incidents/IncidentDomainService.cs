namespace HelpDesk.Api.Incidents;

public record LogIncident(
    Guid IncidentId,
    Guid CustomerId,
    Contact Contact,
    string Description,
    Guid LoggedBy
);

public record CategoriseIncident(
    Guid IncidentId,
    IncidentCategory Category,
    Guid CategorisedBy
);

public record PrioritiseIncident(
    Guid IncidentId,
    IncidentPriority Priority,
    Guid PrioritisedBy
);

public record AssignAgent(
    Guid IncidentId,
    Guid AgentId,
    Guid AssignedBy
);

public record RecordAgentResponseToIncident(
    Guid IncidentId,
    IncidentResponse.FromAgent Response
);

public record RecordCustomerResponseToIncident(
    Guid IncidentId,
    IncidentResponse.FromCustomer Response
);

public record ResolveIncident(
    Guid IncidentId,
    ResolutionType ResolutionType,
    string ResolutionDescription,
    Guid ResolvedBy
);

public record AcknowledgeResolution(
    Guid IncidentId,
    Guid AcknowledgedBy
);

public record CloseIncident(
    Guid IncidentId,
    Guid ClosedBy
);

internal static class IncidentDomainService
{
    public static IncidentLogged Handle(LogIncident command)
    {
        var (incidentId, customerId, contact, description, loggedBy) = command;

        return new IncidentLogged(
            incidentId,
            customerId,
            contact,
            description,
            loggedBy,
            DateTimeOffset.UtcNow
        );
    }

    public static IncidentCategorised Handle(Incident current, CategoriseIncident command)
    {
        if (current.Status == IncidentStatus.Closed)
        {
            throw new InvalidOperationException("Incident is already closed.");
        }

        var (incidentId, category, categorisedBy) = command;

        return new IncidentCategorised(
            incidentId,
            category,
            categorisedBy,
            DateTimeOffset.UtcNow
        );
    }

    public static IncidentPrioritised Handle(Incident current, PrioritiseIncident command)
    {
        if (current.Status == IncidentStatus.Closed)
        {
            throw new InvalidOperationException("Incident is already closed.");
        }

        var (incidentId, priority, prioritisedBy) = command;

        return new IncidentPrioritised(
            incidentId,
            priority,
            prioritisedBy,
            DateTimeOffset.UtcNow
        );
    }

    public static AgentAssigned Handle(Incident current, AssignAgent command)
    {
        if (current.Status == IncidentStatus.Closed)
        {
            throw new InvalidOperationException("Incident is already closed.");
        }

        var (incidentId, agentId, assignedBy) = command;

        return new AgentAssigned(
            incidentId,
            agentId,
            assignedBy,
            DateTimeOffset.UtcNow
        );
    }

    public static AgentRespondedToIncident Handle(Incident current, RecordAgentResponseToIncident command)
    {
        if (current.Status == IncidentStatus.Closed)
        {
            throw new InvalidOperationException("Incident is already closed.");
        }

        var (incidentId, response) = command;

        return new AgentRespondedToIncident(
            incidentId,
            response,
            DateTimeOffset.UtcNow
        );
    }

    public static CustomerRespondedToIncident Handle(Incident current, RecordCustomerResponseToIncident command)
    {
        if (current.Status == IncidentStatus.Closed)
        {
            throw new InvalidOperationException("Incident is already closed.");
        }

        var (incidentId, response) = command;

        return new CustomerRespondedToIncident(
            incidentId,
            response,
            DateTimeOffset.UtcNow
        );
    }

    public static IncidentResolved Handle(Incident current, ResolveIncident command)
    {
        if (current.Status is IncidentStatus.Resolved or IncidentStatus.Closed)
        {
            throw new InvalidOperationException("Cannot resolve already resolved or closed incident.");
        }

        if (current.HasOutstandingResponseToCustomer)
        {
            throw new InvalidOperationException("Cannot resolve incident with outstanding response to customer.");
        }

        var (incidentId, resolutionType, resolutionDescription, resolvedBy) = command;

        return new IncidentResolved(
            incidentId,
            resolutionType,
            resolutionDescription,
            resolvedBy,
            DateTimeOffset.UtcNow
        );
    }

    public static ResolutionAcknowledgedByCustomer Handle(Incident current, AcknowledgeResolution command)
    {
        if (current.Status != IncidentStatus.Resolved)
        {
            throw new InvalidOperationException("Only resolved incidents can be acknowledged.");
        }

        var (incidentId, acknowledgedBy) = command;

        return new ResolutionAcknowledgedByCustomer(
            incidentId,
            acknowledgedBy,
            DateTimeOffset.UtcNow
        );
    }

    public static IncidentClosed Handle(Incident current, CloseIncident command)
    {
        if (current.Status != IncidentStatus.ResolutionAcknowledgedByCustomer)
        {
            throw new InvalidOperationException("Only incidents with acknowledged resolution can be closed.");
        }

        var (incidentId, closedBy) = command;

        return new IncidentClosed(
            incidentId,
            closedBy,
            DateTimeOffset.UtcNow
        );
    }
}