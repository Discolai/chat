namespace Application.Models;

public static class ModelsEndpoints
{
    public static IEndpointRouteBuilder MapModelsEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/models").RequireAuthorization();

        group.MapGet("/", GetModels.Handle);

        return builder;
    }
}
