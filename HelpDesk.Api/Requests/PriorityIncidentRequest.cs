using HelpDesk.Api.Incidents;

namespace HelpDesk.Api.Requests;

public record PriorityIncidentRequest(IncidentPriority Priority);