namespace Domain;

[GenerateSerializer]
[Alias("Domain.AIModel")]
public record AIModel(string Provider, string Name, string Version);