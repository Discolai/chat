namespace Domain;

[GenerateSerializer]
[Alias("Domain.Message")]
public class Message
{
    [Id(0)]
    public required MessageType Type { get; init; }
    [Id(1)]
    public required string Content { get; init; }
}
