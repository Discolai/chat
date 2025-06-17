namespace Domain;

public record ConfiguredAIModel(AIModelProvider Provider, string Name, string Description, string? ApiKey, string? Endpoint)
{
    public AIModel ToAIModel() => new(Provider, Name, Description);
}
