using HelpDesk.Api.Incidents;

namespace HelpDesk.Api.GraphQL.Queries.Types;

public class IncidentShortInfoType : ObjectType<IncidentShortInfo>
{
    protected override void Configure(IObjectTypeDescriptor<IncidentShortInfo> descriptor)
    {
        descriptor.BindFieldsExplicitly();

        descriptor.Field(p => p.Id);
        descriptor.Field(p => p.CustomerId);
        descriptor.Field(p => p.Status);
        descriptor.Field(p => p.NotesCount);
        descriptor.Field(p => p.Category);
        descriptor.Field(p => p.Priority);
    }
}