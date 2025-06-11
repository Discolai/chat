namespace Domain;

[GenerateSerializer]
[Alias("Domain.ConversationInfo")]
public class ConversationInfo
{
    [Id(0)]
    public required Guid Id { get; init; }
    [Id(1)]
    public required AIModel Model { get; init; }
    [Id(2)]
    public required string Title { get; init; }
}
