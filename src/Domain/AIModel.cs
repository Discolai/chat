namespace Domain;

[GenerateSerializer]
[Alias("Domain.AIModel")]
public record AIModel(AIModelProvider Provider, string Name, string Description);