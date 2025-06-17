namespace Domain;

[GenerateSerializer]
[Alias("Domain.ConversationInfo")]
public record ConversationInfo(Guid Id, AIModel Model, string Title);
