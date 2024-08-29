namespace HelpDesk.Api.Requests;

public record AssignIncidentRequest(Guid AgentId, Guid AssignedBy);