namespace Domain;

[GenerateSerializer]
[Alias("Domain.ConversationAlreadyInitializedException")]
public class ConversationAlreadyInitializedException : Exception
{
    public ConversationAlreadyInitializedException() : base("The conversation has already been initialized")
    {
    }
}