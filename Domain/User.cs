namespace Domain;

[GenerateSerializer]
[Alias("Domain.User")]
public class User
{
    [Id(0)]
    public required Guid Id { get; init; }
    [Id(1)]
    public required string Username { get; init; }
    [Id(2)]
    public List<ConversationInfo> Conversations { get; init; } = [];
}
