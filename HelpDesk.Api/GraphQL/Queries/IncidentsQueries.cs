using HelpDesk.Api.GraphQL.Repositories;
using HelpDesk.Api.Incidents;
using HotChocolate.Data;
using HotChocolate.Pagination;
using HotChocolate.Types.Pagination;
using Marten;

namespace HelpDesk.Api.GraphQL.Queries;

[QueryType]
public class IncidentsQueries
{
    [UsePaging]
    [UseSorting]
    [UseFiltering]
    public IQueryable<IncidentShortInfo> GetCustomerIncidents(Guid customerId, IncidentRepository incidentRepository) =>
        incidentRepository.GetByCustomerId(customerId);
}