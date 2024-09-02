using HelpDesk.Api.Incidents;
using Marten;

namespace HelpDesk.Api.GraphQL.Repositories;

public sealed class IncidentRepository(IQuerySession querySession)
{
    public IQueryable<IncidentShortInfo> GetByCustomerId(Guid customerId) =>
        querySession.Query<IncidentShortInfo>().Where(x => x.CustomerId == customerId);
}