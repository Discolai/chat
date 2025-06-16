namespace Application;

public class JwtOptions
{
    public required string MetadataAddress { get; set; }
    public required string Authority { get; set; }
    public required string Audience { get; set; }
}
