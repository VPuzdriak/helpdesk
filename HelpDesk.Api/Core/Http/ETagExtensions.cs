using Microsoft.Net.Http.Headers;

namespace HelpDesk.Api.Core.Http;

public static class ETagExtensions
{
    public static int ToExpectedVersion(this string? eTag)
    {
        ArgumentNullException.ThrowIfNull(eTag);

        var value = EntityTagHeaderValue.Parse(eTag).Tag.Value!;
        return int.Parse(value[1..^2]);
    }
}