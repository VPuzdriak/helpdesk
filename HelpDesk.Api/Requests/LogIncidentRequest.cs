using HelpDesk.Api.Incidents;

namespace HelpDesk.Api.Requests;

public record LogIncidentRequest(
    Contact Contact,
    string Description,
    Guid LoggedBy
);