using Marten;

namespace HelpDesk.Api.Core.Marten;

public static class DocumentSessionExtensions
{
    public static async Task Add<T>(this IDocumentSession documentSession, Guid id, object @event, CancellationToken ct)
        where T : class
    {
        documentSession.Events.StartStream<T>(id, @event);
        await documentSession.SaveChangesAsync(ct);
    }

    public static Task GetAndUpdate<T>(
        this IDocumentSession documentSession,
        Guid id,
        int version,
        Func<T, object> handle, CancellationToken ct)
        where T : class =>
        documentSession.Events.WriteToAggregate<T>(
            id,
            version,
            stream => stream.AppendOne(handle(stream.Aggregate)),
            ct);
}