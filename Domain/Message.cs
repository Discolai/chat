namespace Domain;

[GenerateSerializer]
[Alias("Domain.Message")]
public class Message
{
    [Id(0)]
    public Guid Id { get; set; }
    [Id(1)]
    public required MessageRole Role { get; init; }
    [Id(2)]
    public required string Content { get; set; }
}
