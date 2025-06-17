namespace Domain;
public class AIModelConfiguration
{
    public required ConfiguredAIModel[] Models { get; set; }

    public required string DefaultModel { get; set; }
}
