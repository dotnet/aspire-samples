using CQRS.Mediatr.Lite;

namespace CrossPlatform.Web.Api.Data.Queries;

public class GetEnrichedGeographiesQuery : Query<IEnumerable<Geography>>
{
    public override string DisplayName => nameof(GetEnrichedGeographiesQuery);
    public override string Id { get; } = Guid.NewGuid().ToString();

    public override bool Validate(out string? validationErrorMessage)
    {
        validationErrorMessage = null;
        return true;
    }
}

