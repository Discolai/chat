namespace Domain;

[GenerateSerializer]
public enum MessageState
{
    Pending,
    Completed,
    Failed
}
