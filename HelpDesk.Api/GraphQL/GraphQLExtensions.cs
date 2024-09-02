using HotChocolate.Execution.Configuration;

namespace HelpDesk.Api.GraphQL;

internal static class GraphQLExtensions
{
    public static IRequestExecutorBuilder AddGraphQLConventions(this IRequestExecutorBuilder builder)
    {
        builder.AddPagingArguments();
        return builder;
    }
}